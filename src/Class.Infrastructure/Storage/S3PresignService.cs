using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Class.Application.Abstractions;
using Class.Application.Dto;
using Class.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Class.Infrastructure.Storage;

public sealed class S3PresignService : IStoragePresignService, IDisposable
{
    private const string Region = "us-east-1";
    private static readonly TimeSpan ObjectCheckTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<S3PresignService> _logger;
    private readonly IMinioClient _minioClient;
    private readonly MinioEndpoint _minioEndpoint;
    private readonly S3Options _options;
    private readonly AmazonS3Client _publicClient;

    public S3PresignService(S3Options options, ILogger<S3PresignService>? logger = null)
    {
        _options = options;
        _logger = logger ?? NullLogger<S3PresignService>.Instance;
        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        _publicClient = new AmazonS3Client(credentials, CreateClientConfig(options.PublicEndpoint, options.UseSsl));
        _minioEndpoint = ParseMinioEndpoint(options.InternalEndpoint);
        _minioClient = new MinioClient()
            .WithEndpoint(_minioEndpoint.Endpoint)
            .WithCredentials(options.AccessKey, options.SecretKey)
            .WithSSL(_minioEndpoint.Secure)
            .WithTimeout((int)ObjectCheckTimeout.TotalMilliseconds)
            .Build();

        _logger.LogDebug(
            "Configured MinIO object checks for bucket {Bucket} at {Endpoint} secure={Secure}.",
            options.Bucket,
            _minioEndpoint.Endpoint,
            _minioEndpoint.Secure);
    }

    public Task<PresignedUrlDto> CreatePutUrlAsync(string objectKey, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.PresignedUrlExpiresMinutes);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Protocol = _options.UseSsl ? Protocol.HTTPS : Protocol.HTTP,
            Expires = expiresAt.UtcDateTime
        };

        var url = UseConfiguredEndpoint(_publicClient.GetPreSignedURL(request), _options.PublicEndpoint);
        return Task.FromResult(new PresignedUrlDto(url, expiresAt));
    }

    public Task<PresignedUrlDto> CreateGetUrlAsync(string objectKey, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.PresignedUrlExpiresMinutes);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Protocol = _options.UseSsl ? Protocol.HTTPS : Protocol.HTTP,
            Expires = expiresAt.UtcDateTime
        };

        var url = UseConfiguredEndpoint(_publicClient.GetPreSignedURL(request), _options.PublicEndpoint);
        return Task.FromResult(new PresignedUrlDto(url, expiresAt));
    }

    public async Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ObjectCheckTimeout);

            var request = new StatObjectArgs()
                .WithBucket(_options.Bucket)
                .WithObject(objectKey);

            await _minioClient.StatObjectAsync(request, timeout.Token);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Timed out while checking S3 object {Bucket}/{ObjectKey} at {Endpoint}.",
                _options.Bucket,
                objectKey,
                _minioEndpoint.Endpoint);
            throw new StorageObjectCheckException("Timed out while checking uploaded object in storage.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to check S3 object {Bucket}/{ObjectKey} at {Endpoint}.",
                _options.Bucket,
                objectKey,
                _minioEndpoint.Endpoint);
            throw new StorageObjectCheckException("Failed to check uploaded object in storage.", ex);
        }
    }

    public void Dispose()
    {
        _publicClient.Dispose();
        _minioClient.Dispose();
    }

    public static AmazonS3Config CreateClientConfig(string endpoint, bool useSsl)
    {
        var serviceUrl = endpoint.TrimEnd('/');

        return new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,
            UseHttp = !useSsl,
            AuthenticationRegion = Region,
            Timeout = ObjectCheckTimeout,
#pragma warning disable CS0618
            ReadWriteTimeout = ObjectCheckTimeout,
#pragma warning restore CS0618
            MaxErrorRetry = 1
        };
    }

    public static MinioEndpoint ParseMinioEndpoint(string endpoint)
    {
        var uri = new Uri(endpoint.TrimEnd('/'));
        var host = uri.Host.Contains(':') && !uri.Host.StartsWith("[", StringComparison.Ordinal)
            ? $"[{uri.Host}]"
            : uri.Host;
        var parsedEndpoint = uri.IsDefaultPort ? host : $"{host}:{uri.Port}";

        return new MinioEndpoint(parsedEndpoint, uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string UseConfiguredEndpoint(string presignedUrl, string endpoint)
    {
        var endpointUri = new Uri(endpoint);
        var urlBuilder = new UriBuilder(presignedUrl)
        {
            Scheme = endpointUri.Scheme,
            Host = endpointUri.Host,
            Port = endpointUri.IsDefaultPort ? -1 : endpointUri.Port
        };

        if (endpointUri.AbsolutePath != "/")
        {
            urlBuilder.Path = $"{endpointUri.AbsolutePath.TrimEnd('/')}/{urlBuilder.Path.TrimStart('/')}";
        }

        return urlBuilder.Uri.ToString();
    }
}

public sealed class StorageObjectCheckException : Exception
{
    public StorageObjectCheckException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public readonly record struct MinioEndpoint(string Endpoint, bool Secure);

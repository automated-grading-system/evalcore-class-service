using Microsoft.Extensions.Configuration;

namespace Class.Infrastructure.Configuration;

public sealed class S3Options
{
    public string InternalEndpoint { get; init; } = "http://localhost:9000";

    public string PublicEndpoint { get; init; } = "http://localhost:9000";

    public string AccessKey { get; init; } = "ags";

    public string SecretKey { get; init; } = "ags_password";

    public bool UseSsl { get; init; }

    public string Bucket { get; init; } = "lab-assets";

    public int PresignedUrlExpiresMinutes { get; init; } = 15;

    public static S3Options FromConfiguration(IConfiguration configuration)
    {
        return new S3Options
        {
            InternalEndpoint = ReadEndpoint(configuration, "S3_INTERNAL_ENDPOINT", "S3:InternalEndpoint", "http://localhost:9000"),
            PublicEndpoint = ReadEndpoint(configuration, "S3_PUBLIC_ENDPOINT", "S3:PublicEndpoint", "http://localhost:9000"),
            AccessKey = Read(configuration, "S3_ACCESS_KEY", "S3:AccessKey", "ags"),
            SecretKey = Read(configuration, "S3_SECRET_KEY", "S3:SecretKey", "ags_password"),
            UseSsl = bool.TryParse(Read(configuration, "S3_USE_SSL", "S3:UseSsl", "false"), out var useSsl) && useSsl,
            Bucket = Read(configuration, "LAB_ASSETS_BUCKET", "S3:Bucket", "lab-assets"),
            PresignedUrlExpiresMinutes = int.TryParse(
                Read(configuration, "PRESIGNED_URL_EXPIRES_MINUTES", "S3:PresignedUrlExpiresMinutes", "15"),
                out var minutes)
                ? Math.Clamp(minutes, 1, 1440)
                : 15
        };
    }

    private static string Read(IConfiguration configuration, string environmentKey, string configKey, string defaultValue)
    {
        var value = configuration[environmentKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        value = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return defaultValue;
    }

    private static string ReadEndpoint(IConfiguration configuration, string environmentKey, string configKey, string defaultValue)
    {
        var endpoint = Read(configuration, environmentKey, configKey, defaultValue).TrimEnd('/');
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException($"{environmentKey} must be an absolute HTTP or HTTPS endpoint.");
        }

        return endpoint;
    }
}

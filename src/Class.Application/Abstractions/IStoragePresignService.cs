using Class.Application.Dto;

namespace Class.Application.Abstractions;

public interface IStoragePresignService
{
    Task<PresignedUrlDto> CreatePutUrlAsync(string objectKey, CancellationToken cancellationToken);

    Task<PresignedUrlDto> CreateGetUrlAsync(string objectKey, CancellationToken cancellationToken);

    Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken);
}

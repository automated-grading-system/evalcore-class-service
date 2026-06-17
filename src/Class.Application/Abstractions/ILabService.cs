using Class.Application.Common;
using Class.Application.Dto;
using Class.Application.Requests;

namespace Class.Application.Abstractions;

public interface ILabService
{
    Task<ServiceResult<CreateLabResponse>> CreateLabAsync(Guid classId, CreateLabRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<LabDto>>> ListLabsAsync(Guid classId, PaginationQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<LabDto>> GetLabAsync(Guid labId, CancellationToken cancellationToken);

    Task<ServiceResult<LabDto>> UpdateLabAsync(Guid labId, UpdateLabRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<DeletedResponse>> DeleteLabAsync(Guid labId, CancellationToken cancellationToken);

    Task<ServiceResult<LabDto>> CompleteAssetsAsync(Guid labId, CompleteLabAssetsRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<PresignedUrlDto>> GetRequirementUrlAsync(Guid labId, CancellationToken cancellationToken);

    Task<ServiceResult<PresignedUrlDto>> GetCollectionUrlAsync(Guid labId, CancellationToken cancellationToken);
}

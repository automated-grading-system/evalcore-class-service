using Class.Application.Common;
using Class.Application.Dto;
using Class.Application.Requests;

namespace Class.Application.Abstractions;

public interface IClassService
{
    Task<ServiceResult<ClassDto>> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<ClassDto>>> ListClassesAsync(PaginationQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<ClassDto>>> SearchClassesAsync(string? name, PaginationQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<ClassDto>> GetClassAsync(Guid classId, CancellationToken cancellationToken);

    Task<ServiceResult<ClassDto>> UpdateClassAsync(Guid classId, UpdateClassRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<DeletedResponse>> DeleteClassAsync(Guid classId, CancellationToken cancellationToken);

    Task<ServiceResult<ClassStudentDto>> JoinClassAsync(Guid classId, CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<ClassDto>>> ListMyClassesAsync(PaginationQuery query, CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<ClassMemberDto>>> ListMembersAsync(Guid classId, PaginationQuery query, CancellationToken cancellationToken);
}

using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Application.Dto;
using Class.Application.Mapping;
using Class.Application.Requests;
using Class.Domain.Constants;
using Class.Domain.Entities;

namespace Class.Application.Services;

public sealed class ClassService : IClassService
{
    private readonly IClassRepository _classRepository;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ClassService(
        IClassRepository classRepository,
        IClock clock,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _classRepository = classRepository;
        _clock = clock;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<ClassDto>> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<ClassDto>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Lecturer))
        {
            return ServiceResult<ClassDto>.Forbidden(ErrorCodes.Forbidden, "Only lecturers can create classes.");
        }

        var validation = ValidateClassName<ClassDto>(request.Name);
        if (validation is not null)
        {
            return validation;
        }

        var now = _clock.UtcNow;
        var classroomClass = new ClassroomClass
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            CreatedBy = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _classRepository.AddAsync(classroomClass, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClassDto>.Created(classroomClass.ToDto());
    }

    public async Task<ServiceResult<PagedResponse<ClassDto>>> ListClassesAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<PagedResponse<ClassDto>>();
        if (auth is not null)
        {
            return auth;
        }

        var page = query.ToPageRequest();
        (IReadOnlyList<ClassroomClass> Items, int TotalItems) result;

        if (IsRole(RoleNames.Admin))
        {
            result = await _classRepository.ListAllAsync(page, cancellationToken);
        }
        else if (IsRole(RoleNames.Lecturer))
        {
            result = await _classRepository.ListByLecturerAsync(_currentUser.UserId, page, cancellationToken);
        }
        else
        {
            return ServiceResult<PagedResponse<ClassDto>>.Forbidden(ErrorCodes.Forbidden, "Only lecturers and admins can list managed classes.");
        }

        return ServiceResult<PagedResponse<ClassDto>>.Ok(ToPagedClassResponse(result.Items, page, result.TotalItems));
    }

    public async Task<ServiceResult<PagedResponse<ClassDto>>> SearchClassesAsync(string? name, PaginationQuery query, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<PagedResponse<ClassDto>>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Student) && !IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
        {
            return ServiceResult<PagedResponse<ClassDto>>.Forbidden(ErrorCodes.Forbidden, "Role is not allowed to search classes.");
        }

        var page = query.ToPageRequest();
        var result = await _classRepository.SearchByNameAsync(name, page, cancellationToken);
        return ServiceResult<PagedResponse<ClassDto>>.Ok(ToPagedClassResponse(result.Items, page, result.TotalItems));
    }

    public async Task<ServiceResult<ClassDto>> GetClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<ClassDto>();
        if (auth is not null)
        {
            return auth;
        }

        var classroomClass = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classroomClass is null)
        {
            return ServiceResult<ClassDto>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        var access = await EnsureCanViewClassAsync<ClassDto>(classroomClass, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        return ServiceResult<ClassDto>.Ok(classroomClass.ToDto());
    }

    public async Task<ServiceResult<ClassDto>> UpdateClassAsync(Guid classId, UpdateClassRequest request, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<ClassDto>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
        {
            return ServiceResult<ClassDto>.Forbidden(ErrorCodes.Forbidden, "Only lecturers and admins can update classes.");
        }

        var validation = ValidateClassName<ClassDto>(request.Name);
        if (validation is not null)
        {
            return validation;
        }

        var classroomClass = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classroomClass is null)
        {
            return ServiceResult<ClassDto>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        if (!CanManageClass(classroomClass))
        {
            return ServiceResult<ClassDto>.Forbidden(ErrorCodes.ClassAccessDenied, "You do not have access to manage this class.");
        }

        classroomClass.Name = request.Name.Trim();
        classroomClass.Description = NormalizeOptional(request.Description);
        classroomClass.UpdatedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<ClassDto>.Ok(classroomClass.ToDto());
    }

    public async Task<ServiceResult<DeletedResponse>> DeleteClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<DeletedResponse>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
        {
            return ServiceResult<DeletedResponse>.Forbidden(ErrorCodes.Forbidden, "Only lecturers and admins can delete classes.");
        }

        var classroomClass = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classroomClass is null)
        {
            return ServiceResult<DeletedResponse>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        if (!CanManageClass(classroomClass))
        {
            return ServiceResult<DeletedResponse>.Forbidden(ErrorCodes.ClassAccessDenied, "You do not have access to manage this class.");
        }

        _classRepository.Remove(classroomClass);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<DeletedResponse>.Ok(new DeletedResponse(true));
    }

    public async Task<ServiceResult<ClassStudentDto>> JoinClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<ClassStudentDto>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Student))
        {
            return ServiceResult<ClassStudentDto>.Forbidden(ErrorCodes.Forbidden, "Only students can join classes.");
        }

        if (!await _classRepository.ExistsAsync(classId, cancellationToken))
        {
            return ServiceResult<ClassStudentDto>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        if (await _classRepository.IsStudentInClassAsync(classId, _currentUser.UserId, cancellationToken))
        {
            return ServiceResult<ClassStudentDto>.Conflict(ErrorCodes.AlreadyJoinedClass, "Student has already joined this class.");
        }

        var classStudent = new ClassStudent
        {
            Id = Guid.NewGuid(),
            ClassId = classId,
            StudentId = _currentUser.UserId,
            JoinedAt = _clock.UtcNow
        };

        await _classRepository.AddStudentAsync(classStudent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<ClassStudentDto>.Created(classStudent.ToDto());
    }

    public async Task<ServiceResult<PagedResponse<ClassDto>>> ListMyClassesAsync(PaginationQuery query, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<PagedResponse<ClassDto>>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Student))
        {
            return ServiceResult<PagedResponse<ClassDto>>.Forbidden(ErrorCodes.Forbidden, "Only students can list joined classes.");
        }

        var page = query.ToPageRequest();
        var result = await _classRepository.ListJoinedClassesAsync(_currentUser.UserId, page, cancellationToken);
        return ServiceResult<PagedResponse<ClassDto>>.Ok(ToPagedClassResponse(result.Items, page, result.TotalItems));
    }

    public async Task<ServiceResult<PagedResponse<ClassMemberDto>>> ListMembersAsync(
        Guid classId,
        PaginationQuery query,
        CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<PagedResponse<ClassMemberDto>>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
        {
            return ServiceResult<PagedResponse<ClassMemberDto>>.Forbidden(ErrorCodes.Forbidden, "Only lecturers and admins can view class members.");
        }

        var classroomClass = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classroomClass is null)
        {
            return ServiceResult<PagedResponse<ClassMemberDto>>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        if (!CanManageClass(classroomClass))
        {
            return ServiceResult<PagedResponse<ClassMemberDto>>.Forbidden(ErrorCodes.ClassAccessDenied, "You do not have access to this class.");
        }

        var page = query.ToPageRequest();
        var result = await _classRepository.ListMembersAsync(classId, page, cancellationToken);
        var dto = result.Items.Select(student => student.ToMemberDto()).ToList();
        return ServiceResult<PagedResponse<ClassMemberDto>>.Ok(new PagedResponse<ClassMemberDto>(dto, page.Page, page.PageSize, result.TotalItems));
    }

    private bool CanManageClass(ClassroomClass classroomClass)
    {
        return IsRole(RoleNames.Admin) || (IsRole(RoleNames.Lecturer) && classroomClass.CreatedBy == _currentUser.UserId);
    }

    private async Task<ServiceResult<T>?> EnsureCanViewClassAsync<T>(ClassroomClass classroomClass, CancellationToken cancellationToken)
    {
        if (CanManageClass(classroomClass))
        {
            return null;
        }

        if (IsRole(RoleNames.Student))
        {
            var joined = await _classRepository.IsStudentInClassAsync(classroomClass.Id, _currentUser.UserId, cancellationToken);
            return joined
                ? null
                : ServiceResult<T>.Forbidden(ErrorCodes.StudentNotInClass, "Student has not joined this class.");
        }

        return ServiceResult<T>.Forbidden(ErrorCodes.ClassAccessDenied, "You do not have access to this class.");
    }

    private static PagedResponse<ClassDto> ToPagedClassResponse(IReadOnlyList<ClassroomClass> classes, PageRequest page, int totalItems)
    {
        var dto = classes.Select(classroomClass => classroomClass.ToDto()).ToList();
        return new PagedResponse<ClassDto>(dto, page.Page, page.PageSize, totalItems);
    }

    private ServiceResult<T>? EnsureAuthenticated<T>()
    {
        return _currentUser.IsAuthenticated ? null : ServiceResult<T>.Unauthorized();
    }

    private bool IsRole(string role)
    {
        return string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceResult<T>? ValidateClassName<T>(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult<T>.Validation(
                ErrorCodes.ValidationError,
                "Validation failed.",
                new Dictionary<string, string[]> { ["name"] = ["Name is required."] });
        }

        if (name.Trim().Length > 150)
        {
            return ServiceResult<T>.Validation(
                ErrorCodes.ValidationError,
                "Validation failed.",
                new Dictionary<string, string[]> { ["name"] = ["Name must be 150 characters or fewer."] });
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

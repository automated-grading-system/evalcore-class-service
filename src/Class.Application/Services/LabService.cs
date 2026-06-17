using System.Text.Json;
using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Application.Dto;
using Class.Application.Mapping;
using Class.Application.Requests;
using Class.Domain.Constants;
using Class.Domain.Entities;

namespace Class.Application.Services;

public sealed class LabService : ILabService
{
    private static readonly JsonSerializerOptions EventJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IClassRepository _classRepository;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ILabRepository _labRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly IStoragePresignService _storagePresignService;
    private readonly IUnitOfWork _unitOfWork;

    public LabService(
        IClassRepository classRepository,
        IClock clock,
        ICurrentUser currentUser,
        ILabRepository labRepository,
        IOutboxEventRepository outboxEventRepository,
        IStoragePresignService storagePresignService,
        IUnitOfWork unitOfWork)
    {
        _classRepository = classRepository;
        _clock = clock;
        _currentUser = currentUser;
        _labRepository = labRepository;
        _outboxEventRepository = outboxEventRepository;
        _storagePresignService = storagePresignService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<CreateLabResponse>> CreateLabAsync(
        Guid classId,
        CreateLabRequest request,
        CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<CreateLabResponse>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
        {
            return ServiceResult<CreateLabResponse>.Forbidden(ErrorCodes.Forbidden, "Only lecturers and admins can create labs.");
        }

        var classroomClass = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classroomClass is null)
        {
            return ServiceResult<CreateLabResponse>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        if (!CanManageClass(classroomClass))
        {
            return ServiceResult<CreateLabResponse>.Forbidden(ErrorCodes.ClassAccessDenied, "You do not have access to manage this class.");
        }

        var validation = ValidateCreateLab<CreateLabResponse>(request);
        if (validation is not null)
        {
            return validation;
        }

        var labId = Guid.NewGuid();
        var requirementKey = $"requirements/{labId}/{ToSafeFileName(request.RequirementFileName)}";
        var collectionKey = $"postman-collections/{labId}/{ToSafeFileName(request.CollectionFileName)}";

        PresignedUrlDto requirementUpload;
        PresignedUrlDto collectionUpload;

        try
        {
            requirementUpload = await _storagePresignService.CreatePutUrlAsync(requirementKey, cancellationToken);
            collectionUpload = await _storagePresignService.CreatePutUrlAsync(collectionKey, cancellationToken);
        }
        catch (Exception ex)
        {
            return ServiceResult<CreateLabResponse>.InternalError(ErrorCodes.S3PresignFailed, "Failed to generate lab asset upload URLs.", ex.Message);
        }

        var now = _clock.UtcNow;
        var lab = new Lab
        {
            Id = labId,
            ClassId = classId,
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            RequirementObjectKey = requirementKey,
            CollectionObjectKey = collectionKey,
            Status = LabStatuses.PendingAssets,
            Deadline = request.Deadline.ToUniversalTime(),
            CreatedBy = _currentUser.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _labRepository.AddAsync(lab, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var upload = new LabUploadDto(
            requirementUpload.Url,
            collectionUpload.Url,
            requirementUpload.ExpiresAt <= collectionUpload.ExpiresAt ? requirementUpload.ExpiresAt : collectionUpload.ExpiresAt);

        return ServiceResult<CreateLabResponse>.Created(new CreateLabResponse(lab.ToDto(), upload));
    }

    public async Task<ServiceResult<PagedResponse<LabDto>>> ListLabsAsync(
        Guid classId,
        PaginationQuery query,
        CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<PagedResponse<LabDto>>();
        if (auth is not null)
        {
            return auth;
        }

        var classroomClass = await _classRepository.GetByIdAsync(classId, cancellationToken);
        if (classroomClass is null)
        {
            return ServiceResult<PagedResponse<LabDto>>.NotFound(ErrorCodes.ClassNotFound, "Class was not found.");
        }

        var access = await EnsureCanViewClassAsync<PagedResponse<LabDto>>(classroomClass, cancellationToken);
        if (access is not null)
        {
            return access;
        }

        var page = query.ToPageRequest();
        var activeOnly = IsRole(RoleNames.Student);
        var result = await _labRepository.ListByClassAsync(classId, activeOnly, page, cancellationToken);
        var dto = result.Items.Select(lab => lab.ToDto()).ToList();

        return ServiceResult<PagedResponse<LabDto>>.Ok(new PagedResponse<LabDto>(dto, page.Page, page.PageSize, result.TotalItems));
    }

    public async Task<ServiceResult<LabDto>> GetLabAsync(Guid labId, CancellationToken cancellationToken)
    {
        var labResult = await GetAccessibleLabAsync<LabDto>(labId, requireManageAccess: false, requireActiveForStudent: true, cancellationToken);
        if (labResult.ErrorResult is not null)
        {
            return labResult.ErrorResult;
        }

        return ServiceResult<LabDto>.Ok(labResult.Lab!.ToDto());
    }

    public async Task<ServiceResult<LabDto>> UpdateLabAsync(Guid labId, UpdateLabRequest request, CancellationToken cancellationToken)
    {
        var labResult = await GetAccessibleLabAsync<LabDto>(labId, requireManageAccess: true, requireActiveForStudent: false, cancellationToken);
        if (labResult.ErrorResult is not null)
        {
            return labResult.ErrorResult;
        }

        var validation = ValidateUpdateLab<LabDto>(request);
        if (validation is not null)
        {
            return validation;
        }

        var lab = labResult.Lab!;
        var classroomClass = labResult.Class!;
        var changedFields = new List<string>();
        var title = request.Title.Trim();
        var description = NormalizeOptional(request.Description);
        var deadline = request.Deadline.ToUniversalTime();

        if (!string.Equals(lab.Title, title, StringComparison.Ordinal))
        {
            changedFields.Add("title");
        }

        if (!string.Equals(lab.Description, description, StringComparison.Ordinal))
        {
            changedFields.Add("description");
        }

        if (lab.Deadline != deadline)
        {
            changedFields.Add("deadline");
        }

        lab.Title = title;
        lab.Description = description;
        lab.Deadline = deadline;
        lab.UpdatedAt = _clock.UtcNow;

        await _outboxEventRepository.AddAsync(CreateLabUpdatedEvent(lab, classroomClass.Name, changedFields), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<LabDto>.Ok(lab.ToDto());
    }

    public async Task<ServiceResult<DeletedResponse>> DeleteLabAsync(Guid labId, CancellationToken cancellationToken)
    {
        var labResult = await GetAccessibleLabAsync<DeletedResponse>(labId, requireManageAccess: true, requireActiveForStudent: false, cancellationToken);
        if (labResult.ErrorResult is not null)
        {
            return labResult.ErrorResult;
        }

        _labRepository.Remove(labResult.Lab!);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<DeletedResponse>.Ok(new DeletedResponse(true));
    }

    public async Task<ServiceResult<LabDto>> CompleteAssetsAsync(
        Guid labId,
        CompleteLabAssetsRequest request,
        CancellationToken cancellationToken)
    {
        var labResult = await GetAccessibleLabAsync<LabDto>(labId, requireManageAccess: true, requireActiveForStudent: false, cancellationToken);
        if (labResult.ErrorResult is not null)
        {
            return labResult.ErrorResult;
        }

        var lab = labResult.Lab!;
        var classroomClass = labResult.Class!;

        if (lab.Status != LabStatuses.PendingAssets)
        {
            return ServiceResult<LabDto>.Conflict(ErrorCodes.LabAssetsAlreadyCompleted, "Lab assets have already been completed.");
        }

        if (!request.RequirementUploaded || !request.CollectionUploaded)
        {
            return ServiceResult<LabDto>.Validation(
                ErrorCodes.LabAssetsNotCompleted,
                "Both requirement and collection assets must be uploaded before completing the lab.");
        }

        var requirementExists = await CheckObjectExistsAsync<LabDto>(lab.RequirementObjectKey, cancellationToken);
        if (requirementExists.ErrorResult is not null)
        {
            return requirementExists.ErrorResult;
        }

        if (!requirementExists.Exists)
        {
            return ServiceResult<LabDto>.NotFound(ErrorCodes.S3ObjectNotFound, "Requirement PDF object was not found in object storage.");
        }

        var collectionExists = await CheckObjectExistsAsync<LabDto>(lab.CollectionObjectKey, cancellationToken);
        if (collectionExists.ErrorResult is not null)
        {
            return collectionExists.ErrorResult;
        }

        if (!collectionExists.Exists)
        {
            return ServiceResult<LabDto>.NotFound(ErrorCodes.S3ObjectNotFound, "Postman Collection object was not found in object storage.");
        }

        var now = _clock.UtcNow;
        lab.Status = LabStatuses.Active;
        lab.UpdatedAt = now;
        lab.AssetsCompletedAt = now;

        await _outboxEventRepository.AddAsync(CreateLabCreatedEvent(lab, classroomClass.Name), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<LabDto>.Ok(lab.ToDto());
    }

    public async Task<ServiceResult<PresignedUrlDto>> GetRequirementUrlAsync(Guid labId, CancellationToken cancellationToken)
    {
        var labResult = await GetAccessibleLabAsync<PresignedUrlDto>(labId, requireManageAccess: false, requireActiveForStudent: true, cancellationToken);
        if (labResult.ErrorResult is not null)
        {
            return labResult.ErrorResult;
        }

        try
        {
            return ServiceResult<PresignedUrlDto>.Ok(await _storagePresignService.CreateGetUrlAsync(labResult.Lab!.RequirementObjectKey, cancellationToken));
        }
        catch (Exception ex)
        {
            return ServiceResult<PresignedUrlDto>.InternalError(ErrorCodes.S3PresignFailed, "Failed to generate requirement PDF URL.", ex.Message);
        }
    }

    public async Task<ServiceResult<PresignedUrlDto>> GetCollectionUrlAsync(Guid labId, CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<PresignedUrlDto>();
        if (auth is not null)
        {
            return auth;
        }

        if (!IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
        {
            return ServiceResult<PresignedUrlDto>.Forbidden(ErrorCodes.LabAccessDenied, "Students cannot access Postman Collection assets.");
        }

        var labResult = await GetAccessibleLabAsync<PresignedUrlDto>(labId, requireManageAccess: true, requireActiveForStudent: false, cancellationToken);
        if (labResult.ErrorResult is not null)
        {
            return labResult.ErrorResult;
        }

        try
        {
            return ServiceResult<PresignedUrlDto>.Ok(await _storagePresignService.CreateGetUrlAsync(labResult.Lab!.CollectionObjectKey, cancellationToken));
        }
        catch (Exception ex)
        {
            return ServiceResult<PresignedUrlDto>.InternalError(ErrorCodes.S3PresignFailed, "Failed to generate Postman Collection URL.", ex.Message);
        }
    }

    private async Task<LabAccessResult<T>> GetAccessibleLabAsync<T>(
        Guid labId,
        bool requireManageAccess,
        bool requireActiveForStudent,
        CancellationToken cancellationToken)
    {
        var auth = EnsureAuthenticated<T>();
        if (auth is not null)
        {
            return new LabAccessResult<T>(null, null, auth);
        }

        var lab = await _labRepository.GetByIdAsync(labId, cancellationToken);
        if (lab is null)
        {
            return new LabAccessResult<T>(null, null, ServiceResult<T>.NotFound(ErrorCodes.LabNotFound, "Lab was not found."));
        }

        var classroomClass = await _classRepository.GetByIdAsync(lab.ClassId, cancellationToken);
        if (classroomClass is null)
        {
            return new LabAccessResult<T>(null, null, ServiceResult<T>.NotFound(ErrorCodes.ClassNotFound, "Class was not found."));
        }

        if (requireManageAccess)
        {
            if (!IsRole(RoleNames.Lecturer) && !IsRole(RoleNames.Admin))
            {
                return new LabAccessResult<T>(null, null, ServiceResult<T>.Forbidden(ErrorCodes.Forbidden, "Only lecturers and admins can manage labs."));
            }

            if (!CanManageClass(classroomClass))
            {
                return new LabAccessResult<T>(null, null, ServiceResult<T>.Forbidden(ErrorCodes.LabAccessDenied, "You do not have access to manage this lab."));
            }
        }
        else
        {
            var access = await EnsureCanViewClassAsync<T>(classroomClass, cancellationToken);
            if (access is not null)
            {
                return new LabAccessResult<T>(null, null, access);
            }
        }

        if (IsRole(RoleNames.Student) && requireActiveForStudent && lab.Status != LabStatuses.Active)
        {
            return new LabAccessResult<T>(null, null, ServiceResult<T>.Forbidden(ErrorCodes.LabNotActive, "Lab is not active for students."));
        }

        return new LabAccessResult<T>(lab, classroomClass, null);
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

        return ServiceResult<T>.Forbidden(ErrorCodes.LabAccessDenied, "You do not have access to this lab.");
    }

    private async Task<ObjectExistsResult<T>> CheckObjectExistsAsync<T>(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            return new ObjectExistsResult<T>(
                await _storagePresignService.ObjectExistsAsync(objectKey, cancellationToken),
                null);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ObjectExistsResult<T>(
                false,
                ServiceResult<T>.InternalError(
                    ErrorCodes.S3ObjectCheckFailed,
                    "Could not verify uploaded lab assets."));
        }
        catch (Exception ex)
        {
            return new ObjectExistsResult<T>(
                false,
                ServiceResult<T>.InternalError(
                    ErrorCodes.S3ObjectCheckFailed,
                    "Could not verify uploaded lab assets.",
                    ex.Message));
        }
    }

    private ServiceResult<T>? EnsureAuthenticated<T>()
    {
        return _currentUser.IsAuthenticated ? null : ServiceResult<T>.Unauthorized();
    }

    private bool IsRole(string role)
    {
        return string.Equals(_currentUser.Role, role, StringComparison.OrdinalIgnoreCase);
    }

    private ServiceResult<T>? ValidateCreateLab<T>(CreateLabRequest request)
    {
        var titleValidation = ValidateTitle<T>(request.Title);
        if (titleValidation is not null)
        {
            return titleValidation;
        }

        if (request.Deadline.ToUniversalTime() <= _clock.UtcNow)
        {
            return ServiceResult<T>.Validation(ErrorCodes.LabDeadlineInvalid, "Lab deadline must be in the future.");
        }

        if (!HasExtension(request.RequirementFileName, ".pdf"))
        {
            return ServiceResult<T>.Validation(
                ErrorCodes.LabAssetInvalidFileType,
                "Requirement asset must be a PDF file.",
                new Dictionary<string, string[]> { ["requirementFileName"] = ["Requirement file must end with .pdf."] });
        }

        if (!HasExtension(request.CollectionFileName, ".json"))
        {
            return ServiceResult<T>.Validation(
                ErrorCodes.LabAssetInvalidFileType,
                "Postman Collection asset must be a JSON file.",
                new Dictionary<string, string[]> { ["collectionFileName"] = ["Collection file must end with .json."] });
        }

        return null;
    }

    private ServiceResult<T>? ValidateUpdateLab<T>(UpdateLabRequest request)
    {
        return ValidateTitle<T>(request.Title);
    }

    private static ServiceResult<T>? ValidateTitle<T>(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ServiceResult<T>.Validation(
                ErrorCodes.ValidationError,
                "Validation failed.",
                new Dictionary<string, string[]> { ["title"] = ["Title is required."] });
        }

        if (title.Trim().Length > 200)
        {
            return ServiceResult<T>.Validation(
                ErrorCodes.ValidationError,
                "Validation failed.",
                new Dictionary<string, string[]> { ["title"] = ["Title must be 200 characters or fewer."] });
        }

        return null;
    }

    private static bool HasExtension(string? fileName, string extension)
    {
        return !string.IsNullOrWhiteSpace(fileName)
            && fileName.Trim().EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToSafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName.Trim());
        var safe = new char[name.Length];

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            safe[i] = char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-';
        }

        return new string(safe);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private OutboxEvent CreateLabCreatedEvent(Lab lab, string className)
    {
        var eventId = Guid.NewGuid();
        var occurredAt = _clock.UtcNow;
        var payload = new
        {
            eventId,
            eventType = "LabCreated",
            eventVersion = 1,
            occurredAt,
            correlationId = Guid.NewGuid(),
            causationId = Guid.NewGuid(),
            publisher = "class-service",
            data = new
            {
                labId = lab.Id,
                classId = lab.ClassId,
                className,
                title = lab.Title,
                deadline = lab.Deadline,
                createdBy = lab.CreatedBy
            }
        };

        return new OutboxEvent
        {
            Id = eventId,
            EventType = "LabCreated",
            EventVersion = 1,
            RoutingKey = "class.lab-created.v1",
            PayloadJson = JsonSerializer.Serialize(payload, EventJsonOptions),
            OccurredAt = occurredAt
        };
    }

    private OutboxEvent CreateLabUpdatedEvent(Lab lab, string className, IReadOnlyList<string> changedFields)
    {
        var eventId = Guid.NewGuid();
        var occurredAt = _clock.UtcNow;
        var payload = new
        {
            eventId,
            eventType = "LabUpdated",
            eventVersion = 1,
            occurredAt,
            correlationId = Guid.NewGuid(),
            causationId = Guid.NewGuid(),
            publisher = "class-service",
            data = new
            {
                labId = lab.Id,
                classId = lab.ClassId,
                className,
                title = lab.Title,
                deadline = lab.Deadline,
                updatedBy = _currentUser.UserId,
                changedFields
            }
        };

        return new OutboxEvent
        {
            Id = eventId,
            EventType = "LabUpdated",
            EventVersion = 1,
            RoutingKey = "class.lab-updated.v1",
            PayloadJson = JsonSerializer.Serialize(payload, EventJsonOptions),
            OccurredAt = occurredAt
        };
    }

    private sealed record LabAccessResult<T>(Lab? Lab, ClassroomClass? Class, ServiceResult<T>? ErrorResult);

    private sealed record ObjectExistsResult<T>(bool Exists, ServiceResult<T>? ErrorResult);
}

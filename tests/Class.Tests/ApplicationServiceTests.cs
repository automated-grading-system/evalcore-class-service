using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Application.Dto;
using Class.Application.Requests;
using Class.Application.Services;
using Class.Domain.Constants;
using Class.Domain.Entities;
using Class.Infrastructure.Configuration;
using Class.Infrastructure.Storage;
using FluentAssertions;
using Xunit;

namespace Class.Tests;

public sealed class ApplicationServiceTests
{
    private static readonly Guid LecturerId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherLecturerId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid StudentId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 6, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Lecturer_can_create_class()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);

        var result = await context.ClassService.CreateClassAsync(new CreateClassRequest
        {
            Name = "PRN232 - ASP.NET Core Web API",
            Description = "Demo"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value!.CreatedBy.Should().Be(LecturerId);
        context.Store.Classes.Should().ContainSingle();
    }

    [Fact]
    public async Task Student_cannot_create_class()
    {
        var context = TestContext.ForUser(StudentId, RoleNames.Student);

        var result = await context.ClassService.CreateClassAsync(new CreateClassRequest
        {
            Name = "PRN232"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error!.Code.Should().Be(ErrorCodes.Forbidden);
    }

    [Fact]
    public async Task Lecturer_can_update_own_class()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(LecturerId);

        var result = await context.ClassService.UpdateClassAsync(classroomClass.Id, new UpdateClassRequest
        {
            Name = "PRN232 Updated",
            Description = "Updated"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("PRN232 Updated");
        result.Value.Description.Should().Be("Updated");
    }

    [Fact]
    public async Task Lecturer_cannot_update_another_lecturers_class()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(OtherLecturerId);

        var result = await context.ClassService.UpdateClassAsync(classroomClass.Id, new UpdateClassRequest
        {
            Name = "Should not update"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error!.Code.Should().Be(ErrorCodes.ClassAccessDenied);
    }

    [Fact]
    public async Task Student_can_join_class()
    {
        var context = TestContext.ForUser(StudentId, RoleNames.Student);
        var classroomClass = context.Store.AddClass(LecturerId);

        var result = await context.ClassService.JoinClassAsync(classroomClass.Id, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value!.StudentId.Should().Be(StudentId);
        context.Store.ClassStudents.Should().ContainSingle(x => x.ClassId == classroomClass.Id && x.StudentId == StudentId);
    }

    [Fact]
    public async Task Duplicate_join_returns_conflict()
    {
        var context = TestContext.ForUser(StudentId, RoleNames.Student);
        var classroomClass = context.Store.AddClass(LecturerId);
        context.Store.AddStudent(classroomClass.Id, StudentId);

        var result = await context.ClassService.JoinClassAsync(classroomClass.Id, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Error!.Code.Should().Be(ErrorCodes.AlreadyJoinedClass);
    }

    [Fact]
    public async Task Student_can_list_joined_classes()
    {
        var context = TestContext.ForUser(StudentId, RoleNames.Student);
        var joined = context.Store.AddClass(LecturerId, "Joined class");
        context.Store.AddClass(LecturerId, "Other class");
        context.Store.AddStudent(joined.Id, StudentId);

        var result = await context.ClassService.ListMyClassesAsync(new PaginationQuery(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(x => x.Id == joined.Id);
    }

    [Fact]
    public async Task Create_lab_rejects_non_pdf_requirement_filename()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(LecturerId);

        var request = ValidCreateLabRequest();
        request.RequirementFileName = "requirements.txt";

        var result = await context.LabService.CreateLabAsync(classroomClass.Id, request, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error!.Code.Should().Be(ErrorCodes.LabAssetInvalidFileType);
    }

    [Fact]
    public async Task Create_lab_rejects_non_json_collection_filename()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(LecturerId);

        var request = ValidCreateLabRequest();
        request.CollectionFileName = "collection.pdf";

        var result = await context.LabService.CreateLabAsync(classroomClass.Id, request, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error!.Code.Should().Be(ErrorCodes.LabAssetInvalidFileType);
    }

    [Fact]
    public async Task Complete_assets_marks_lab_active_and_writes_lab_created_outbox_event()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(LecturerId);
        var lab = context.Store.AddPendingLab(classroomClass.Id, LecturerId);
        context.Storage.ExistingKeys.Add(lab.RequirementObjectKey);
        context.Storage.ExistingKeys.Add(lab.CollectionObjectKey);

        var result = await context.LabService.CompleteAssetsAsync(lab.Id, new CompleteLabAssetsRequest
        {
            RequirementUploaded = true,
            CollectionUploaded = true
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(LabStatuses.Active);
        result.Value.AssetsCompletedAt.Should().NotBeNull();
        context.Store.OutboxEvents.Should().ContainSingle(x => x.EventType == "LabCreated");
    }

    [Fact]
    public async Task Complete_assets_returns_clean_error_when_object_is_missing()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(LecturerId);
        var lab = context.Store.AddPendingLab(classroomClass.Id, LecturerId);
        context.Storage.ExistingKeys.Add(lab.RequirementObjectKey);

        var result = await context.LabService.CompleteAssetsAsync(lab.Id, new CompleteLabAssetsRequest
        {
            RequirementUploaded = true,
            CollectionUploaded = true
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Error!.Code.Should().Be(ErrorCodes.S3ObjectNotFound);
        lab.Status.Should().Be(LabStatuses.PendingAssets);
        lab.AssetsCompletedAt.Should().BeNull();
        context.Store.OutboxEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Complete_assets_returns_clean_error_when_object_check_fails()
    {
        var context = TestContext.ForUser(LecturerId, RoleNames.Lecturer);
        var classroomClass = context.Store.AddClass(LecturerId);
        var lab = context.Store.AddPendingLab(classroomClass.Id, LecturerId);
        context.Storage.ObjectExistsException = new TaskCanceledException("S3 object check timed out.");

        var result = await context.LabService.CompleteAssetsAsync(lab.Id, new CompleteLabAssetsRequest
        {
            RequirementUploaded = true,
            CollectionUploaded = true
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Error!.Code.Should().Be(ErrorCodes.S3ObjectCheckFailed);
        lab.Status.Should().Be(LabStatuses.PendingAssets);
        context.Store.OutboxEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Student_in_class_can_get_requirement_url_after_lab_is_active()
    {
        var context = TestContext.ForUser(StudentId, RoleNames.Student);
        var classroomClass = context.Store.AddClass(LecturerId);
        context.Store.AddStudent(classroomClass.Id, StudentId);
        var lab = context.Store.AddActiveLab(classroomClass.Id, LecturerId);

        var result = await context.LabService.GetRequirementUrlAsync(lab.Id, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Url.Should().Contain(lab.RequirementObjectKey);
    }

    [Fact]
    public async Task Student_cannot_access_collection_url()
    {
        var context = TestContext.ForUser(StudentId, RoleNames.Student);
        var classroomClass = context.Store.AddClass(LecturerId);
        context.Store.AddStudent(classroomClass.Id, StudentId);
        context.Store.AddActiveLab(classroomClass.Id, LecturerId);

        var result = await context.LabService.GetCollectionUrlAsync(context.Store.Labs.Single().Id, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error!.Code.Should().Be(ErrorCodes.LabAccessDenied);
    }

    [Fact]
    public async Task Fake_presigned_service_returns_url_shape()
    {
        var storage = new FakeStoragePresignService();

        var result = await storage.CreatePutUrlAsync("requirements/lab-1/file.pdf", CancellationToken.None);

        result.Url.Should().StartWith("http://storage.test/lab-assets/requirements/lab-1/file.pdf");
        result.Url.Should().Contain("method=PUT");
        result.ExpiresAt.Should().BeAfter(Now);
    }

    [Fact]
    public async Task Real_s3_presign_service_uses_configured_local_minio_public_endpoint()
    {
        using var storage = new S3PresignService(new S3Options
        {
            InternalEndpoint = "http://localhost:9000",
            PublicEndpoint = "http://localhost:9000",
            AccessKey = "ags",
            SecretKey = "ags_password",
            UseSsl = false,
            Bucket = "lab-assets",
            PresignedUrlExpiresMinutes = 15
        });
        const string objectKey = "requirements/lab-1/file.pdf";

        var result = await storage.CreatePutUrlAsync(objectKey, CancellationToken.None);

        result.Url.Should().Contain("localhost:9000");
        result.Url.Should().Contain("lab-assets");
        result.Url.Should().Contain(objectKey);
        result.Url.Should().NotContain("s3.amazonaws.com");
    }

    [Fact]
    public void Internal_s3_client_config_uses_internal_minio_endpoint()
    {
        var config = S3PresignService.CreateClientConfig("http://localhost:9000", useSsl: false);

        config.ServiceURL.TrimEnd('/').Should().Be("http://localhost:9000");
        config.ForcePathStyle.Should().BeTrue();
        config.UseHttp.Should().BeTrue();
        config.AuthenticationRegion.Should().Be("us-east-1");
        config.Timeout.Should().Be(TimeSpan.FromSeconds(5));
#pragma warning disable CS0618
        config.ReadWriteTimeout.Should().Be(TimeSpan.FromSeconds(5));
#pragma warning restore CS0618
        config.MaxErrorRetry.Should().Be(1);
    }

    [Theory]
    [InlineData("http://localhost:9000", "localhost:9000", false)]
    [InlineData("http://minio:9000", "minio:9000", false)]
    [InlineData("https://storage.example.com", "storage.example.com", true)]
    public void Minio_object_check_endpoint_parser_preserves_port_and_secure_flag(
        string endpoint,
        string expectedMinioEndpoint,
        bool expectedSecure)
    {
        var parsed = S3PresignService.ParseMinioEndpoint(endpoint);

        parsed.Endpoint.Should().Be(expectedMinioEndpoint);
        parsed.Secure.Should().Be(expectedSecure);
        parsed.Endpoint.Should().NotBe("localhost");
    }

    [Fact]
    public void Minio_object_check_endpoint_parser_does_not_fall_back_to_localhost_80()
    {
        var parsed = S3PresignService.ParseMinioEndpoint("http://localhost:9000");

        parsed.Endpoint.Should().Be("localhost:9000");
        parsed.Secure.Should().BeFalse();
    }

    private static CreateLabRequest ValidCreateLabRequest()
    {
        return new CreateLabRequest
        {
            Title = "Lab 01",
            Description = "Build API",
            Deadline = Now.AddDays(7),
            RequirementFileName = "lab-01-requirements.pdf",
            CollectionFileName = "lab-01-postman-collection.json"
        };
    }

    private sealed class TestContext
    {
        private TestContext(InMemoryStore store, FakeStoragePresignService storage, IClassService classService, ILabService labService)
        {
            Store = store;
            Storage = storage;
            ClassService = classService;
            LabService = labService;
        }

        public InMemoryStore Store { get; }

        public FakeStoragePresignService Storage { get; }

        public IClassService ClassService { get; }

        public ILabService LabService { get; }

        public static TestContext ForUser(Guid userId, string role)
        {
            var store = new InMemoryStore();
            var currentUser = new FakeCurrentUser(userId, role);
            var clock = new FakeClock();
            var storage = new FakeStoragePresignService();
            var classService = new ClassService(store, clock, currentUser, store);
            var labService = new LabService(store, clock, currentUser, store, store, storage, store);

            return new TestContext(store, storage, classService, labService);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public FakeCurrentUser(Guid userId, string role)
        {
            UserId = userId;
            Role = role;
        }

        public bool IsAuthenticated => UserId != Guid.Empty;

        public Guid UserId { get; }

        public string Email => "user@example.test";

        public string Role { get; }

        public string? FullName => "Test User";
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeStoragePresignService : IStoragePresignService
    {
        public HashSet<string> ExistingKeys { get; } = new(StringComparer.Ordinal);

        public Exception? ObjectExistsException { get; set; }

        public Task<PresignedUrlDto> CreatePutUrlAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateUrl(objectKey, "PUT"));
        }

        public Task<PresignedUrlDto> CreateGetUrlAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(CreateUrl(objectKey, "GET"));
        }

        public Task<bool> ObjectExistsAsync(string objectKey, CancellationToken cancellationToken)
        {
            if (ObjectExistsException is not null)
            {
                return Task.FromException<bool>(ObjectExistsException);
            }

            return Task.FromResult(ExistingKeys.Contains(objectKey));
        }

        private static PresignedUrlDto CreateUrl(string objectKey, string method)
        {
            return new PresignedUrlDto($"http://storage.test/lab-assets/{objectKey}?method={method}&signature=fake", Now.AddMinutes(15));
        }
    }

    private sealed class InMemoryStore : IClassRepository, ILabRepository, IOutboxEventRepository, IUnitOfWork
    {
        public List<ClassroomClass> Classes { get; } = [];

        public List<ClassStudent> ClassStudents { get; } = [];

        public List<Lab> Labs { get; } = [];

        public List<OutboxEvent> OutboxEvents { get; } = [];

        public ClassroomClass AddClass(Guid createdBy, string name = "PRN232")
        {
            var classroomClass = new ClassroomClass
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = "Demo",
                CreatedBy = createdBy,
                CreatedAt = Now,
                UpdatedAt = Now
            };

            Classes.Add(classroomClass);
            return classroomClass;
        }

        public ClassStudent AddStudent(Guid classId, Guid studentId)
        {
            var classStudent = new ClassStudent
            {
                Id = Guid.NewGuid(),
                ClassId = classId,
                StudentId = studentId,
                JoinedAt = Now
            };

            ClassStudents.Add(classStudent);
            return classStudent;
        }

        public Lab AddPendingLab(Guid classId, Guid createdBy)
        {
            return AddLab(classId, createdBy, LabStatuses.PendingAssets);
        }

        public Lab AddActiveLab(Guid classId, Guid createdBy)
        {
            return AddLab(classId, createdBy, LabStatuses.Active);
        }

        public Task AddAsync(ClassroomClass classroomClass, CancellationToken cancellationToken)
        {
            Classes.Add(classroomClass);
            return Task.CompletedTask;
        }

        public Task<ClassroomClass?> GetByIdAsync(Guid classId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Classes.FirstOrDefault(x => x.Id == classId));
        }

        public Task<bool> ExistsAsync(Guid classId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Classes.Any(x => x.Id == classId));
        }

        public Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListByLecturerAsync(
            Guid lecturerId,
            PageRequest page,
            CancellationToken cancellationToken)
        {
            return PageAsync(Classes.Where(x => x.CreatedBy == lecturerId), page);
        }

        public Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListAllAsync(
            PageRequest page,
            CancellationToken cancellationToken)
        {
            return PageAsync(Classes, page);
        }

        public Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> SearchByNameAsync(
            string? name,
            PageRequest page,
            CancellationToken cancellationToken)
        {
            var query = string.IsNullOrWhiteSpace(name)
                ? Classes
                : Classes.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            return PageAsync(query, page);
        }

        public Task<ClassStudent?> GetClassStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ClassStudents.FirstOrDefault(x => x.ClassId == classId && x.StudentId == studentId));
        }

        public Task<bool> IsStudentInClassAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ClassStudents.Any(x => x.ClassId == classId && x.StudentId == studentId));
        }

        public Task AddStudentAsync(ClassStudent classStudent, CancellationToken cancellationToken)
        {
            ClassStudents.Add(classStudent);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListJoinedClassesAsync(
            Guid studentId,
            PageRequest page,
            CancellationToken cancellationToken)
        {
            var joinedClassIds = ClassStudents
                .Where(x => x.StudentId == studentId)
                .Select(x => x.ClassId)
                .ToHashSet();
            return PageAsync(Classes.Where(x => joinedClassIds.Contains(x.Id)), page);
        }

        public Task<(IReadOnlyList<ClassStudent> Items, int TotalItems)> ListMembersAsync(
            Guid classId,
            PageRequest page,
            CancellationToken cancellationToken)
        {
            return PageAsync(ClassStudents.Where(x => x.ClassId == classId), page);
        }

        public void Remove(ClassroomClass classroomClass)
        {
            Classes.Remove(classroomClass);
            ClassStudents.RemoveAll(x => x.ClassId == classroomClass.Id);
            Labs.RemoveAll(x => x.ClassId == classroomClass.Id);
        }

        public Task AddAsync(Lab lab, CancellationToken cancellationToken)
        {
            Labs.Add(lab);
            return Task.CompletedTask;
        }

        Task<Lab?> ILabRepository.GetByIdAsync(Guid labId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Labs.FirstOrDefault(x => x.Id == labId));
        }

        public Task<(IReadOnlyList<Lab> Items, int TotalItems)> ListByClassAsync(
            Guid classId,
            bool activeOnly,
            PageRequest page,
            CancellationToken cancellationToken)
        {
            var query = Labs.Where(x => x.ClassId == classId);
            if (activeOnly)
            {
                query = query.Where(x => x.Status == LabStatuses.Active);
            }

            return PageAsync(query, page);
        }

        public void Remove(Lab lab)
        {
            Labs.Remove(lab);
        }

        public Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
        {
            OutboxEvents.Add(outboxEvent);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }

        private Lab AddLab(Guid classId, Guid createdBy, string status)
        {
            var labId = Guid.NewGuid();
            var lab = new Lab
            {
                Id = labId,
                ClassId = classId,
                Title = "Lab 01",
                RequirementObjectKey = $"requirements/{labId}/lab.pdf",
                CollectionObjectKey = $"postman-collections/{labId}/collection.json",
                Status = status,
                Deadline = Now.AddDays(7),
                CreatedBy = createdBy,
                CreatedAt = Now,
                UpdatedAt = Now,
                AssetsCompletedAt = status == LabStatuses.Active ? Now : null
            };

            Labs.Add(lab);
            return lab;
        }

        private static Task<(IReadOnlyList<T> Items, int TotalItems)> PageAsync<T>(IEnumerable<T> values, PageRequest page)
        {
            var list = values.ToList();
            var items = list.Skip(page.Skip).Take(page.PageSize).ToList();
            return Task.FromResult(((IReadOnlyList<T>)items, list.Count));
        }
    }
}

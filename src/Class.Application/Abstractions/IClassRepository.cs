using Class.Application.Common;
using Class.Domain.Entities;

namespace Class.Application.Abstractions;

public interface IClassRepository
{
    Task AddAsync(ClassroomClass classroomClass, CancellationToken cancellationToken);

    Task<ClassroomClass?> GetByIdAsync(Guid classId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid classId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListByLecturerAsync(
        Guid lecturerId,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListAllAsync(
        PageRequest page,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> SearchByNameAsync(
        string? name,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<ClassStudent?> GetClassStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken);

    Task<bool> IsStudentInClassAsync(Guid classId, Guid studentId, CancellationToken cancellationToken);

    Task AddStudentAsync(ClassStudent classStudent, CancellationToken cancellationToken);

    Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListJoinedClassesAsync(
        Guid studentId,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ClassStudent> Items, int TotalItems)> ListMembersAsync(
        Guid classId,
        PageRequest page,
        CancellationToken cancellationToken);

    void Remove(ClassroomClass classroomClass);
}

using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Domain.Constants;
using Class.Domain.Entities;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Class.Infrastructure.Repositories;

public sealed class ClassRepository : IClassRepository
{
    private readonly ClassDbContext _dbContext;

    public ClassRepository(ClassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClassroomClass classroomClass, CancellationToken cancellationToken)
    {
        await _dbContext.Classes.AddAsync(classroomClass, cancellationToken);
    }

    public Task<ClassroomClass?> GetByIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        return _dbContext.Classes.FirstOrDefaultAsync(x => x.Id == classId, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid classId, CancellationToken cancellationToken)
    {
        return _dbContext.Classes.AnyAsync(x => x.Id == classId, cancellationToken);
    }

    public async Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListByLecturerAsync(
        Guid lecturerId,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Classes
            .AsNoTracking()
            .Where(x => x.CreatedBy == lecturerId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Name);

        return await PageAsync(query, page, cancellationToken);
    }

    public async Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListAllAsync(
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Classes
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Name);

        return await PageAsync(query, page, cancellationToken);
    }

    public async Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> SearchByNameAsync(
        string? name,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Classes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(normalizedName));
        }

        query = query.OrderBy(x => x.Name).ThenByDescending(x => x.CreatedAt);
        return await PageAsync(query, page, cancellationToken);
    }

    public Task<ClassStudent?> GetClassStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
    {
        return _dbContext.ClassStudents
            .FirstOrDefaultAsync(x => x.ClassId == classId && x.StudentId == studentId, cancellationToken);
    }

    public Task<bool> IsStudentInClassAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
    {
        return _dbContext.ClassStudents
            .AnyAsync(x => x.ClassId == classId && x.StudentId == studentId, cancellationToken);
    }

    public async Task AddStudentAsync(ClassStudent classStudent, CancellationToken cancellationToken)
    {
        await _dbContext.ClassStudents.AddAsync(classStudent, cancellationToken);
    }

    public async Task<(IReadOnlyList<ClassroomClass> Items, int TotalItems)> ListJoinedClassesAsync(
        Guid studentId,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query =
            from classroomClass in _dbContext.Classes.AsNoTracking()
            join classStudent in _dbContext.ClassStudents.AsNoTracking()
                on classroomClass.Id equals classStudent.ClassId
            where classStudent.StudentId == studentId
            orderby classStudent.JoinedAt descending, classroomClass.Name
            select classroomClass;

        return await PageAsync(query, page, cancellationToken);
    }

    public async Task<(IReadOnlyList<ClassStudent> Items, int TotalItems)> ListMembersAsync(
        Guid classId,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ClassStudents
            .AsNoTracking()
            .Where(x => x.ClassId == classId)
            .OrderByDescending(x => x.JoinedAt);

        return await PageAsync(query, page, cancellationToken);
    }

    public void Remove(ClassroomClass classroomClass)
    {
        _dbContext.Classes.Remove(classroomClass);
    }

    private static async Task<(IReadOnlyList<T> Items, int TotalItems)> PageAsync<T>(
        IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

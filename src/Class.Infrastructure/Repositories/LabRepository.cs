using Class.Application.Abstractions;
using Class.Application.Common;
using Class.Domain.Constants;
using Class.Domain.Entities;
using Class.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Class.Infrastructure.Repositories;

public sealed class LabRepository : ILabRepository
{
    private readonly ClassDbContext _dbContext;

    public LabRepository(ClassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Lab lab, CancellationToken cancellationToken)
    {
        await _dbContext.Labs.AddAsync(lab, cancellationToken);
    }

    public Task<Lab?> GetByIdAsync(Guid labId, CancellationToken cancellationToken)
    {
        return _dbContext.Labs.FirstOrDefaultAsync(x => x.Id == labId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Lab> Items, int TotalItems)> ListByClassAsync(
        Guid classId,
        bool activeOnly,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Labs
            .AsNoTracking()
            .Where(x => x.ClassId == classId);

        query = activeOnly
            ? query.Where(x => x.Status == LabStatuses.Active)
            : query.Where(x => x.Status != LabStatuses.Archived);

        query = query.OrderBy(x => x.Deadline).ThenBy(x => x.Title);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public void Remove(Lab lab)
    {
        _dbContext.Labs.Remove(lab);
    }
}

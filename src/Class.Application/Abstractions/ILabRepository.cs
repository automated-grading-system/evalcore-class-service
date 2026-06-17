using Class.Application.Common;
using Class.Domain.Entities;

namespace Class.Application.Abstractions;

public interface ILabRepository
{
    Task AddAsync(Lab lab, CancellationToken cancellationToken);

    Task<Lab?> GetByIdAsync(Guid labId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Lab> Items, int TotalItems)> ListByClassAsync(
        Guid classId,
        bool activeOnly,
        PageRequest page,
        CancellationToken cancellationToken);

    void Remove(Lab lab);
}

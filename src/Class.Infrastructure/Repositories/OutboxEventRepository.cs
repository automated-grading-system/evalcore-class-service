using Class.Application.Abstractions;
using Class.Domain.Entities;
using Class.Infrastructure.Persistence;

namespace Class.Infrastructure.Repositories;

public sealed class OutboxEventRepository : IOutboxEventRepository
{
    private readonly ClassDbContext _dbContext;

    public OutboxEventRepository(ClassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        await _dbContext.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
    }
}

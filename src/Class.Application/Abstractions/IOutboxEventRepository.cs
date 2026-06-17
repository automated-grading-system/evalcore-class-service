using Class.Domain.Entities;

namespace Class.Application.Abstractions;

public interface IOutboxEventRepository
{
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken);
}

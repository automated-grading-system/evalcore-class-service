using Class.Application.Abstractions;
using Class.Infrastructure.Persistence;

namespace Class.Infrastructure.Repositories;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ClassDbContext _dbContext;

    public EfUnitOfWork(ClassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

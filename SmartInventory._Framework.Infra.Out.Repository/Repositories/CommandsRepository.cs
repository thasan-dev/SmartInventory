using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;

namespace SmartInventory._Framework.Infra.Out.Repository.Repositories;

public class CommandsRepository
{
    private ICommandsDbContext DbContext { get; init; }

    protected CommandsRepository(ICommandsDbContext dbContext)
    {
        DbContext = dbContext;
    }
    
    protected async Task CreateAsync<TAggregateRoot, TAggregateRootId>(DbSet<TAggregateRoot> dbSet,
        TAggregateRoot aggregateRoot)
        where TAggregateRoot : AggregateRoot<TAggregateRootId>
        where TAggregateRootId : EntityId
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            dbSet.Add(aggregateRoot);
            DbContext.DomainEvents.Add(aggregateRoot.DomainEvent);
            await DbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            await DbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
        }
    }
}
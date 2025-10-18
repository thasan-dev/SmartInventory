using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;

namespace SmartInventory._Framework.Infra.Out.Repository.Repositories;

public abstract class CommandsRepository<TAggregateRoot, TEntityId, TDomainEvent>(
    ICommandsDbContext dbContext,
    IPublishEndpoint publishEndpoint):ICommandRepository<TAggregateRoot, TEntityId, TDomainEvent>
where TAggregateRoot : AggregateRoot<TEntityId, TDomainEvent>
where TDomainEvent : DomainEvent
where TEntityId : EntityId
{
    /// <summary>
    /// The db set for the aggregate root.
    /// </summary>
    protected abstract DbSet<TAggregateRoot> DbSet { get; }
    
    public async Task CreateAsync(TAggregateRoot aggregateRoot)
    {
        await CreateAsync(DbSet, aggregateRoot);
    }

    public Task UpdateAsync(TAggregateRoot aggregateRoot)
    {
        throw new NotImplementedException();
    }

    private async Task CreateAsync(DbSet<TAggregateRoot> dbSet,
        TAggregateRoot aggregateRoot)
       
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            dbSet.Add(aggregateRoot);
            await publishEndpoint.Publish(aggregateRoot.DomainEvent);
           
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
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
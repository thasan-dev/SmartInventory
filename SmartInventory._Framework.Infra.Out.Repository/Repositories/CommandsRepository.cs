using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;

namespace SmartInventory._Framework.Infra.Out.Repository.Repositories;

public class CommandsRepository
{
    private ICommandsDbContext DbContext { get; init; }
    private IPublishEndpoint PublishEndpoint { get; init; }

    protected CommandsRepository(ICommandsDbContext dbContext, IPublishEndpoint publishEndpoint)
    {
        DbContext = dbContext;
        PublishEndpoint = publishEndpoint;
    }
    
    protected async Task CreateAsync<TAggregateRoot,TAggregateRootId,TDomainEvent>(DbSet<TAggregateRoot> dbSet,
        TAggregateRoot aggregateRoot)
        where TAggregateRoot : AggregateRoot<TAggregateRootId,TDomainEvent>
        where TDomainEvent : DomainEvent
        where TAggregateRootId : EntityId
    {

        await DbContext.Database.OpenConnectionAsync();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(100));
        await using var transaction = await DbContext.Database.BeginTransactionAsync(cts.Token);
        try
        {
            dbSet.Add(aggregateRoot);
            
            await DbContext.SaveChangesAsync(cts.Token);
            await transaction.CommitAsync(cts.Token);
            
            //await PublishEndpoint.Publish(aggregateRoot.DomainEvent);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cts.Token);
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(cts.Token);
        }
    }
}
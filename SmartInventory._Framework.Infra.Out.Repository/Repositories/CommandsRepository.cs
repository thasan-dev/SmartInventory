using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Events;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;

namespace SmartInventory._Framework.Infra.Out.Repository.Repositories;

public abstract class CommandsRepository<TAggregateRoot, TEntityId, TEventPayload>(
    ICommandsDbContext dbContext,
    IPublishEndpoint publishEndpoint) : ICommandRepository<TAggregateRoot, TEntityId, TEventPayload>
where TAggregateRoot : AggregateRoot<TEntityId>, IPublishDomainEvents<TEventPayload>
where TEventPayload : class
where TEntityId : EntityId
{
    /// <summary>
    /// The db set for the aggregate root.
    /// </summary>
    protected abstract DbSet<TAggregateRoot> DbSet { get; }

    protected abstract string MicroserviceName { get; }

    public async Task CreateAsync(TAggregateRoot aggregateRoot)
    {
        var eventName = $"{typeof(TAggregateRoot).Name}Created";
        var domainEvent = CreateDomainEvent(aggregateRoot, eventName);
        await SaveAndPublishAsync(dbSet =>
        {
            dbSet.Add(aggregateRoot);
            return domainEvent;
        });
    }

    public async Task UpdateAsync(TAggregateRoot aggregateRoot)
    {
        var eventName = $"{typeof(TAggregateRoot).Name}Updated";
        var domainEvent = CreateDomainEvent(aggregateRoot, eventName);
        await SaveAndPublishAsync(dbSet =>
        {
            dbSet.Update(aggregateRoot);
            return domainEvent;
        });
    }

    public async Task SaveAndPublishAsync(Func<DbSet<TAggregateRoot>, DomainEvent<TEventPayload>> ececute)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var domainEvent = ececute(DbSet);
            await publishEndpoint.Publish(domainEvent.ToMessage());

            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
        }
    }

    private DomainEvent<TEventPayload> CreateDomainEvent(TAggregateRoot aggregateRoot, string eventName)
    {
        var aggregateName = typeof(TAggregateRoot).Name;

        var domainEvent = new DomainEvent<TEventPayload>();
        domainEvent.Set(
            domainEventName: eventName,
            microserviceName: MicroserviceName,
            aggregateRootId: aggregateRoot.Id.Value,
            aggregateRootName: aggregateName,
            isPublished: false,
            payload: aggregateRoot.GetEventPayload());

        return domainEvent;
    }
}
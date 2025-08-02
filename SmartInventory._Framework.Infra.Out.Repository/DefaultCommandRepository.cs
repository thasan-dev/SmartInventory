using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;
using SmartInventory._Framework.Infra.Out.Repository.Repositories;

namespace SmartInventory._Framework.Infra.Out.Repository;

public abstract class DefaultCommandRepository<TAggregateRoot, TEntityId, TDomainEvent>(
    ICommandsDbContext dbContext,
    IPublishEndpoint publishEndpoint)
    : CommandsRepository(dbContext, publishEndpoint), IDefaultCommandRepository<TAggregateRoot, TEntityId, TDomainEvent>
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
        await CreateAsync<TAggregateRoot, TEntityId,TDomainEvent>(DbSet, aggregateRoot);
    }

    public Task UpdateAsync(TAggregateRoot aggregateRoot)
    {
        throw new NotImplementedException();
    }
}
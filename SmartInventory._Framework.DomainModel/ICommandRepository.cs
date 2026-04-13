using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Events;

namespace SmartInventory._Framework.DomainModel;

public interface ICommandRepository<TAggregateRoot,TEntityId,TEventPayload>
    where TAggregateRoot : AggregateRoot<TEntityId>, IPublishDomainEvents<TEventPayload>
    where TEntityId : EntityId 
    where TEventPayload : class
{
    /// <summary>
    /// Creates a new TAggregateRoot in the repository.
    /// </summary>
    Task CreateAsync(TAggregateRoot aggregateRoot);

    /// <summary>
    /// Updates an existing TAggregateRoot in the repository.
    /// </summary>
    Task UpdateAsync(TAggregateRoot aggregateRoot);
}
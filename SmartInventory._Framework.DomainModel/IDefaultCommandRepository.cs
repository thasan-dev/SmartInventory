using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

namespace SmartInventory._Framework.DomainModel;

public interface IDefaultCommandRepository<TAggregateRoot,TEntityId,TDomainEvent>
    where TAggregateRoot : AggregateRoot<TEntityId,TDomainEvent>
    where TEntityId : EntityId  
    where TDomainEvent : DomainEvent
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
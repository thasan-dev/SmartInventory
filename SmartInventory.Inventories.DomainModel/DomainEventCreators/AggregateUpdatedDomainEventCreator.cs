using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.DomainEventCreators
{
    public class AggregateUpdatedDomainEventCreator<TAggregateRoot, TEntityId>(
        TAggregateRoot aggregateRoot)
        : InventoryDomainEventCreator<TAggregateRoot, TEntityId>(aggregateRoot)
        where TAggregateRoot : AggregateRoot<TEntityId>
        where TEntityId : EntityId
    {
        protected override DomainEventName DomainEventName => DomainEventName.Create($"{AggregateRootName.Value}Updated");
    
    }
}
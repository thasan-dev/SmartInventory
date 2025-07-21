using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.DomainEventCreators
{
    public class AggregateCreatedDomainEventCreator<TAggregateRoot, TEntityId,TData>(
        TAggregateRoot aggregateRoot)
        : InventoryDomainEventCreator<TAggregateRoot, TEntityId,TData>(aggregateRoot)
        where TAggregateRoot : AggregateRoot<TEntityId,TData>
        where TEntityId : EntityId
        where TData : DomainEventData
    {
        protected override DomainEventName DomainEventName => DomainEventName.Create($"{AggregateRootName.Value}Created");
    
    }
}
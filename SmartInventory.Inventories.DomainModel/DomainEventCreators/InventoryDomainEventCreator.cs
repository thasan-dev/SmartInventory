using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.DomainEventCreators;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.DomainEventCreators
{
    public abstract class InventoryDomainEventCreator<TAggregateRoot, TEntityId, TData>(
        TAggregateRoot aggregateRoot)
        : DomainEventCreatorBase<TAggregateRoot,TEntityId,TData>(aggregateRoot) where TAggregateRoot : AggregateRoot<TEntityId,TData>
        where TEntityId : EntityId
        where TData : DomainEventData
    {
        protected override MicroserviceName MicroserviceName => new MicroserviceName("Inventory");
    }
}
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.DomainEventCreators;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.DomainEventCreators
{
    public abstract class InventoryDomainEventCreator<TAggregateRoot, TEntityId>(
        TAggregateRoot aggregateRoot)
        : DomainEventCreatorBase<TAggregateRoot, TEntityId>(aggregateRoot) where TAggregateRoot : AggregateRoot<TEntityId>
        where TEntityId : EntityId
    {
        protected override MicroserviceName MicroserviceName => new MicroserviceName("Inventory");
    }
}
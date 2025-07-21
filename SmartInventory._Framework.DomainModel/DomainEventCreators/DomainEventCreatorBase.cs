using System.Text.Json;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.DomainEventCreators;

public abstract class DomainEventCreatorBase<TAggregateRoot, TEntityId, TData>(TAggregateRoot aggregateRoot) : IDomainEventCreator<TData>
    where TAggregateRoot : AggregateRoot<TEntityId,TData>
    where TEntityId : EntityId
    where TData : DomainEventData
{
    protected DomainEventId DomainEventId => DomainEventId.Create(Guid.NewGuid());
    protected abstract DomainEventName DomainEventName { get; }
    protected abstract MicroserviceName MicroserviceName { get; }
    protected AggregateRootId AggregateRootId => AggregateRootId.Create(aggregateRoot.Id.Value);
    protected AggregateRootName AggregateRootName => AggregateRootName.Create(typeof(TAggregateRoot).Name);
    protected TData DomainEventData => (TData)Entities.DomainEventEntity.ValueObjects.DomainEventData.Create(JsonSerializer.Serialize(aggregateRoot.ToDomainEventObject()));


    public DomainEvent<TData> ToDomainEvent()
    {
        return DomainEvent<TData>.Create(DomainEventName,
            MicroserviceName,
            AggregateRootId,
            AggregateRootName,
            DomainEventData,
            new IsPublished(false));
    }
}
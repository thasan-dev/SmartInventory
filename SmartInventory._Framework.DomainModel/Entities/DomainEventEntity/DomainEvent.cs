using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

public class DomainEvent<TData> : Entity<DomainEventId>
    where TData : DomainEventData
{
    public DomainEventName Name { get; private set; }
    public MicroserviceName MicroserviceName { get; private set; }
    public AggregateRootId AggregateRootId { get; private set; }
    public AggregateRootName AggregateRootName { get; private set; }
    public TData DomainEventData { get; private set; }
    public IsPublished IsPublished { get; private set; }

    private DomainEvent(DomainEventName name,
        MicroserviceName microserviceName, 
        AggregateRootId aggregateRootId,
        AggregateRootName aggregateRootName,
        TData domainEventData,
        IsPublished isPublished)
    {
        Name = name;
        AggregateRootId = aggregateRootId;
        AggregateRootName = aggregateRootName;
        DomainEventData = domainEventData;
        IsPublished = isPublished;
        MicroserviceName = microserviceName;
    }
    
    public static DomainEvent<TData> Create(DomainEventName name,
        MicroserviceName microserviceName, 
        AggregateRootId aggregateRootId,
        AggregateRootName aggregateRootName,
        TData domainEventData,
        IsPublished isPublished)
    {
        return new DomainEvent<TData>(name, microserviceName, aggregateRootId, aggregateRootName, domainEventData, isPublished);
    }

    public override object ToDomainEventObject()
    {
        return new
        {
            Id = Id.Value,
            DomainEventName = Name.Value,
            AggregateRootId = AggregateRootId.Value,
            AggregateRootName = AggregateRootName.Value,
            MicroserviceName = MicroserviceName.Value,
            DomainEventData = DomainEventData.DataAsJson,
            IsPublished
        };
    }
}



using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

public class DomainEvent<TPayload> : Entity<DomainEventId> where TPayload : class
{
    public DomainEventName Name { get; private set; } = null!;
    public MicroserviceName MicroserviceName { get; private set; } = null!;
    public AggregateRootId AggregateRootId { get; private set; } = null!;
    public AggregateRootName AggregateRootName { get; private set; } = null!;
    public IsPublished IsPublished { get; private set; } = null!;
    public TPayload Payload { get; private set; } = null!;

    public DomainEvent()
        : base(DomainEventId.Create(Guid.NewGuid()))
    {
    }

    public void Set(
        string domainEventName,
        string microserviceName,
        Guid aggregateRootId,
        string aggregateRootName,
        bool isPublished,
        TPayload payload)
    {
        Name = DomainEventName.Create(domainEventName);
        MicroserviceName = MicroserviceName.Create(microserviceName);
        AggregateRootId = AggregateRootId.Create(aggregateRootId);
        AggregateRootName = AggregateRootName.Create(aggregateRootName);
        IsPublished = IsPublished.Create(isPublished);
        Payload = payload;

    }

    public object GetDomainEventMessage()
    {
        return new
        {
            Id = Id.Value,
            AggregateRootId = AggregateRootId.Value,
            AggregateRootName = AggregateRootName.Value,
            MicroserviceName = MicroserviceName.Value,
            IsPublished = IsPublished.Value,
            Payload = Payload
        };
    }
}



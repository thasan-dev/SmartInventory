using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Events;

public class DomainEvent<TPayload> : Entity<DomainEventId> where TPayload : class
{
    public string Name { get; private set; } = null!;
    public string MicroserviceName { get; private set; } = null!;
    public string AggregateRootId { get; private set; } = null!;
    public string AggregateRootName { get; private set; } = null!;
    public bool IsPublished { get; private set; }
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
        Name = domainEventName;
        MicroserviceName = microserviceName;
        AggregateRootId = aggregateRootId.ToString();
        AggregateRootName = aggregateRootName;
        IsPublished = isPublished;
        Payload = payload;
    }

    public DomainEventMessage<TPayload> ToMessage()
    {
        return new DomainEventMessage<TPayload>
        {
            Name = Name,
            MicroserviceName = MicroserviceName,
            AggregateRootId = AggregateRootId,
            AggregateRootName = AggregateRootName,
            IsPublished = IsPublished,
            Payload = Payload
        };
    }
}



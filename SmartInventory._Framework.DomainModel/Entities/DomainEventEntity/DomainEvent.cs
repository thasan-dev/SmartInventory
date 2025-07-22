using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

public class DomainEvent : Entity<DomainEventId>
{
    public DomainEventName Name { get; private set; } = null!;
    public MicroserviceName MicroserviceName { get; private set; } = null!;
    public AggregateRootId AggregateRootId { get; private set; } = null!;
    public AggregateRootName AggregateRootName { get; private set; } = null!;
    public IsPublished IsPublished { get; private set; } = null!;

    protected DomainEvent()
        : base(DomainEventId.Create(Guid.NewGuid()))
    {
    }
    
    public void Set(
        string domainEventName,
        string microserviceName, 
        Guid aggregateRootId,
        string aggregateRootName,
        bool isPublished)
    {
        Name = DomainEventName.Create(domainEventName);
        MicroserviceName= MicroserviceName.Create(microserviceName);
        AggregateRootId = AggregateRootId.Create(aggregateRootId);
        AggregateRootName = AggregateRootName.Create(aggregateRootName);
        IsPublished = IsPublished.Create(isPublished);
    }

    public object GetDomainEventMessage()
    {
        return new
        {
            Id = Id.Value,
            AggregateRootId = AggregateRootId.Value,
            AggregateRootName = AggregateRootName.Value,
            MicroserviceName = MicroserviceName.Value,
            IsPublished
        };
    }
}



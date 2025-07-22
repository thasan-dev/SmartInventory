using System.Diagnostics.CodeAnalysis;
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Entities;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory.Inventories.DomainModel._Common.AggregateRoot;

public abstract class InventoryAggregateRoot<TId,TDomainEvent>: AggregateRoot<TId,TDomainEvent> where TId : EntityId where TDomainEvent :DomainEvent
{
    /// <summary>
    /// Constructor - used by EntityFramework
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected InventoryAggregateRoot()
    {
    }
    
    private static string MicroserviceName => new("Inventory");
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">The unique id for this instance.</param>
    protected InventoryAggregateRoot(TId id)
        : base(id)
    { }

    protected abstract TDomainEvent GetDomainEvent();

    protected override void RaiseDomainEvent<TAggregateRoot>(DomainEventType domainEventType)
    {
        var aggregateName = typeof(TAggregateRoot).Name;
        var domainEventName = DomainEventType.IsCreated(domainEventType)
            ? $"{aggregateName}Created"
            : $"{aggregateName}Updated";

        RaiseDomainEventInternal<TAggregateRoot>(domainEventName);
    }

    protected void RaiseDomainEvent<TAggregateRoot>(string customDomainEventName)
        where TAggregateRoot : InventoryAggregateRoot<TId, TDomainEvent>
    {
        RaiseDomainEventInternal<TAggregateRoot>(customDomainEventName);
    }

    private void RaiseDomainEventInternal<TAggregateRoot>(string domainEventName)
    {
        var domainEvent = GetDomainEvent();
        var aggregateName = typeof(TAggregateRoot).Name;
        var aggregateRootId = Id.Value;
        var microserviceName = MicroserviceName;

        domainEvent.Set(
            domainEventName,
            microserviceName,
            aggregateRootId,
            aggregateName,
            false);

        DomainEvent = domainEvent;
    }
}
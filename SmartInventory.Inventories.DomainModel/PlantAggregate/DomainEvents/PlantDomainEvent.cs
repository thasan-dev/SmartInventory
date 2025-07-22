using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;

public class PlantDomainEvent: DomainEvent
{
    public PlantDomainEventMessage Message { get; init; } = null!;
}
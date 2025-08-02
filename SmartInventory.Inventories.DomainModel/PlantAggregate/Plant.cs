using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;
using SmartInventory.Inventories.DomainModel._Common.AggregateRoot;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public class Plant(PlantId id) : InventoryAggregateRoot<PlantId,PlantDomainEvent>(id)
{
    public PlantName Name { get; private set; } = null!;
    
    public void Create(CreatePlantDomainCommand command)
    {
        Name = PlantName.Create(command.Name);
        RaiseDomainEvent<Plant>(DomainEventType.Created);
 
    }

    protected override PlantDomainEvent GetDomainEvent()
    {
        return new PlantDomainEvent
        {
            Message = new PlantDomainEventMessage
            {
                Id = Id.Value,
                Name = Name.Value
            }
        };
    }
}
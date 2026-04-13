using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Events;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public class Plant(PlantId id) : AggregateRoot<PlantId>(id), IPublishDomainEvents<PlantDomainEventMessage>
{
    public PlantName Name { get; private set; } = null!;

    public void Create(CreatePlantDomainCommand command)
    {
        Name = PlantName.Create(command.Name);
    }

    public PlantDomainEventMessage GetEventPayload()
    {
        return new PlantDomainEventMessage
        {
            Id = Id.Value,
            Name = Name.Value
        };
    }
}
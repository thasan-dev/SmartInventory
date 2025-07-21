using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory.Inventories.DomainModel.DomainEventCreators;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public class Plant(PlantId id) : AggregateRoot<PlantId, PlantDomainEventData>(id)
{
    public PlantName Name { get; private set; } = null!;

    public override object ToDomainEventObject()
    {
        return new
        {
            Id = Id.Value,
            Name = Name.Value,
        };
    }
    
    public void Create(PlantId id, string name)
    {
        HandleDomainCommand(() =>
        {
            Name = PlantName.Create(name);
            return new AggregateCreatedDomainEventCreator<Plant, PlantId, PlantDomainEventData>(this);
        });
    }
}
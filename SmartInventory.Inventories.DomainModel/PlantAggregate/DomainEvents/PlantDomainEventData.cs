using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;

public class PlantDomainEventData: DomainEventData
{
    private PlantDomainEventData(string dataAsJson) : base(dataAsJson)
    {
    }
    
    public static PlantDomainEventData Create(string dataAsJson)
    {
        return new PlantDomainEventData(dataAsJson);
    }
}
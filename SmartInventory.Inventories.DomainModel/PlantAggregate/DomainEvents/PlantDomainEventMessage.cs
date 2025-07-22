namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;

public class PlantDomainEventMessage
{
    public Guid Id { get; set; } 
    public string Name { get; set; }
}
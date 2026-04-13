namespace SmartInventory.Messages.Inventories;

public class PlantDomainEventMessage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

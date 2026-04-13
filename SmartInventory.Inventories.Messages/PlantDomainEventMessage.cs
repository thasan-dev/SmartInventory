namespace SmartInventory.Inventories.Messages;

/// <summary>
/// Message DTO for Plant domain events published to the message broker.
/// </summary>
public record PlantDomainEventMessage
{
    public Guid PlantId { get; init; }
    public string Name { get; init; } = string.Empty;
}
namespace SmartInventory._Framework.DomainModel.Events;

public record DomainEventMessage<TPayload> where TPayload : class
{
    public string Name { get; init; } = null!;
    public string MicroserviceName { get; init; } = null!;
    public string AggregateRootId { get; init; } = null!;
    public string AggregateRootName { get; init; } = null!;
    public bool IsPublished { get; init; }
    public TPayload Payload { get; init; } = null!;
}

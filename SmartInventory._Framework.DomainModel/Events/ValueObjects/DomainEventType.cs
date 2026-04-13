using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

public class DomainEventType : ValueObject
{
    private DomainEventType(string value)
    {
        Value = value;
    }
    public string Value { get; }

    public static DomainEventType Created => new DomainEventType("Created");

    public static DomainEventType Updated => new DomainEventType("Updated");

    public static DomainEventType Custom => new DomainEventType("Custom");
    
    public static bool IsCreated(DomainEventType type) => type.Value == Created.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
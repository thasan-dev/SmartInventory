using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

public class PlantName:ValueObject
{
    public string Value { get; }
    
    private PlantName(string value)
    {
        Value = value;
    }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public static PlantName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Plant name cannot be null or empty.", nameof(value));
        
        return new PlantName(value);
    }
}
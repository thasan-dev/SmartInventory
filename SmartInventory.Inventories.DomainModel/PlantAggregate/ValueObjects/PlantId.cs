using SmartInventory._Framework.DomainModel.Entities;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

public class PlantId: EntityId
{
    private PlantId(Guid value) : base(value)
    {
    }
    
    /// <summary>
    /// Creates a new instance of PlantId.
    /// </summary>
    public static PlantId Create(Guid value)
    {
        return new PlantId(value);
    }
}
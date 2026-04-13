using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public interface IPlantRepository
{
    Task<Plant?> GetByIdAsync(PlantId id);
    Task CreateAsync(Plant plant);
    Task UpdateAsync(Plant plant);
}
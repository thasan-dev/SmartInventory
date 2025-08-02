namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public interface IPlantRepository
{
    public Task CreateAsync(Plant plant);
}
using SmartInventory.Inventories.Application.Plants.ApplicationCommands;

namespace SmartInventory.Inventories.Application.Plants;

public interface IPlantsApplicationService
{
    public Task CreateOrUpdateAsync(CreateOrUpdatePlantCommand command);
}
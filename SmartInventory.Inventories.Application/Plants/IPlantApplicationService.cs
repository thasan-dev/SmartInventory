using SmartInventory.Inventories.Application.Plants.ApplicationCommands;

namespace SmartInventory.Inventories.Application.Plants;

public interface IPlantApplicationService
{
    public Task CreateOrUpdateAsync(CreateOrUpdatePlantCommand command);
}
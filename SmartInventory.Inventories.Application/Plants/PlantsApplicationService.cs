using SmartInventory.Inventories.Application.Plants.ApplicationCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;

namespace SmartInventory.Inventories.Application.Plants;

public class PlantsApplicationService(IPlantRepository plantRepository):IPlantsApplicationService
{
    
    public async Task CreateOrUpdateAsync(CreateOrUpdatePlantCommand command)
    {
        var plant= PlantFactory.Create(new CreatePlantDomainCommand(command.PlantId, command.Name));
        await plantRepository.CreateAsync(plant);
    }
}
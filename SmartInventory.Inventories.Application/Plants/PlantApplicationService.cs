using SmartInventory.Inventories.Application.Plants.ApplicationCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.Application.Plants;

public class PlantApplicationService(IPlantRepository plantRepository) : IPlantApplicationService
{

    public async Task CreateOrUpdateAsync(CreateOrUpdatePlantCommand command)
    {
        var plantId = PlantId.Create(command.PlantId);
        var existingPlant = await plantRepository.GetByIdAsync(plantId);

        if (existingPlant is null)
        {
            var plant = PlantFactory.Create(new CreatePlantDomainCommand(command.PlantId, command.Name));
            await plantRepository.CreateAsync(plant);
        }
        else
        {
            existingPlant.Update(new UpdatePlantDomainCommand(command.Name));
            await plantRepository.UpdateAsync(existingPlant);
        }
    }
}
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public static class PlantFactory
{
    public static Plant Create(CreatePlantDomainCommand command)
    {
        var plantId = PlantId.Create(command.PlantId);
        var newPlant = new Plant(plantId);
        
        newPlant.Create(command);
        
        return newPlant;
    }
}
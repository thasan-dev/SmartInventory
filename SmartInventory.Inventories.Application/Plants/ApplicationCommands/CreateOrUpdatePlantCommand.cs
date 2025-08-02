namespace SmartInventory.Inventories.Application.Plants.ApplicationCommands;

public record CreateOrUpdatePlantCommand(Guid PlantId,string Name);
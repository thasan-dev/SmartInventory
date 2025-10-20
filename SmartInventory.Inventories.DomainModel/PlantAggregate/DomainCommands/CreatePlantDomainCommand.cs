namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;

public record CreatePlantDomainCommand(Guid PlantId, string Name);
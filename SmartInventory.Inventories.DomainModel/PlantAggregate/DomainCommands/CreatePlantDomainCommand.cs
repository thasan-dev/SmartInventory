using System.Windows.Input;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;

public record CreatePlantDomainCommand(Guid PlantId, string Name);
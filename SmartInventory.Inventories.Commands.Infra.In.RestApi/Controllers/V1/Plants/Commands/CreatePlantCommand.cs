using System.ComponentModel.DataAnnotations;

namespace SmartInventory.Inventories.Commands.Infra.In.RestApi.Controllers.V1.Plants.Commands;

public class CreatePlantCommand
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Name { get; set; }=null!;
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.Inventories.Application.Plants;
using SmartInventory.Inventories.Application.Plants.ApplicationCommands;
using SmartInventory.Inventories.Commands.Infra.In.RestApi.Controllers.V1.Plants.Commands;

namespace SmartInventory.Inventories.Commands.Infra.In.RestApi.Controllers.V1.Plants;


[ApiController]
[ApiVersion(1.0)]
[Route("/inventories/v{version:apiVersion}/[controller]")]
public class PlantController(IPlantApplicationService plantApplicationService): ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreatePlantCommand  command)
    {
        await plantApplicationService.CreateOrUpdateAsync(
            new CreateOrUpdatePlantCommand(command.Id, command.Name));
        return Ok();
    }
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SmartInventory.Inventories.Commands.Infra.In.RestApi.Controllers.V1.Plants.Commands;

namespace SmartInventory.Inventories.Commands.Infra.In.RestApi.Controllers.V1.Plants;


[ApiController]
[ApiVersion(1.0)]
[Route("/inventories/v{version:apiVersion}/[controller]")]
public class PlantController: ControllerBase
{
    [HttpPost]
    public IActionResult CreateAsync(CreatePlantCommand  command)
    {
        return Ok();
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmartInventory.Inventories.Queries.Infra.In.RestApi.V1
{
    [Route("/inventories/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class PlantController(ILogger<PlantController> logger) : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult GetPlantById(int id)
        {
            // Placeholder logic to retrieve a plant by its ID
            var plant = new
            {
                Id = id,
                Name = "Sample Plant",
                Species = "Sample Species"
            };

            logger.LogInformation("Retrieved plant with ID {PlantId}", id);

            return Ok(plant);
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace SmartInventory.Inventories.Queries.Infra.In.RestApi.V1
{
    [Route("/inventories/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class PlantController : ControllerBase
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

            return Ok(plant);
        }
    }
}

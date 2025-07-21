using Microsoft.EntityFrameworkCore;
using SmartInventory.Inventories.DomainModel.PlantAggregate;

namespace SmartInventory.Inventories.Repository;

public interface IInventoriesCommandsDbContext
{
    public DbSet<Plant> Plants { get; set; }
}
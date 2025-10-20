using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;
using SmartInventory.Inventories.DomainModel.PlantAggregate;

namespace SmartInventory.Inventories.Repository;

public interface IInventoriesCommandsDbContext : ICommandsDbContext
{
    public DbSet<Plant> Plants { get; set; }
}
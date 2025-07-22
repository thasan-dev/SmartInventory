using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;
using SmartInventory.Inventories.DomainModel.PlantAggregate;

namespace SmartInventory.Inventories.Repository;

public class InventoriesCommandsDbContext(DbContextOptions options) : CommandsDbContext(options),IInventoriesCommandsDbContext
{
    protected override void ConfigureEntityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plant>().ToTable("Plants");
    }
    public DbSet<Plant> Plants { get; set; }
}
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.Repository.DbConfigurations;

namespace SmartInventory.Inventories.Repository;

public class InventoriesCommandsDbContext(DbContextOptions<InventoriesCommandsDbContext> options) : CommandsDbContext<InventoriesCommandsDbContext>(options),IInventoriesCommandsDbContext
{
    public DbSet<Plant> Plants { get; set; }
    
    protected override void ConfigureEntityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PlantConfiguration());
    }
}
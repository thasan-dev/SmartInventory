using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts;
using SmartInventory.Inventories.DomainModel.PlantAggregate;

namespace SmartInventory.Inventories.Repository;

public class InventoriesCommandsDbContext<TData>(DbContextOptions options) : CommandsDbContext<TData>(options),IInventoriesCommandsDbContext where TData : DomainEventData
{
    protected override void ConfigureEntityTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plant>().ToTable("Plants");
    }
    public DbSet<Plant> Plants { get; set; }
}
using Microsoft.EntityFrameworkCore;
using SmartInventory.Inventories.Queries.Infra.Out.Repository.DbConfigurations;
using SmartInventory.Inventories.QueryModel;

namespace SmartInventory.Inventories.Queries.Infra.Out.Repository;

public class InventoriesQueriesDbContext(DbContextOptions<InventoriesQueriesDbContext> options) : DbContext(options), IInventoriesQueriesDbContext
{
    public DbSet<PlantQueryModel> Plants { get; set; }
    public IQueryable<PlantQueryModel> PlantsQueryable => Plants.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PlantDbConfiguration());
    }
}

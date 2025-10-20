using SmartInventory.Inventories.QueryModel;
using TLS._Framework.Infra.Out.Repositories.DbContexts.Interfaces;

namespace SmartInventory.Inventories.Queries.Infra.Out.Repository;

public interface IInventoriesQueriesDbContext : IQueriesDbContext
{
  public IQueryable<PlantQueryModel> PlantsQueryable { get; }
}

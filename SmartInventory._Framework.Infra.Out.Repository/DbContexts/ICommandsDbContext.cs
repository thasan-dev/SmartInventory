using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts;

public interface ICommandsDbContext
{
    /// <summary>
    /// The database of this DbContext.
    /// </summary>
    DatabaseFacade Database { get; }
    
    /// <summary>
    /// Saves all changes added to the DbSets of this DbContext.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
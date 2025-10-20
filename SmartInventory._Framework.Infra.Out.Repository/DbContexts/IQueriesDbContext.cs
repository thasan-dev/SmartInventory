using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TLS._Framework.Infra.Out.Repositories.DbContexts.Interfaces;

/// <summary>
/// Base interface for all Query-stack DbContext classes in the system.
/// </summary>
public interface IQueriesDbContext
{
    /// <summary>
    /// The database of this DbContext.
    /// </summary>
    DatabaseFacade Database { get; }
}
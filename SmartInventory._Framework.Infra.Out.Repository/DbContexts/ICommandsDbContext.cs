using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts;

public interface ICommandsDbContext
{
    /// <summary>
    /// The database of this DbContext.
    /// </summary>
    DatabaseFacade Database { get; }

    /// <summary>
    /// Represents the collection of OutboxState entities used by MassTransit to track batches of outgoing messages
    /// within the Entity Framework Outbox pattern. This table stores metadata about message batches that are part
    /// of the same database transaction.
    /// </summary>
    DbSet<OutboxState> OutboxStates { get; set; }

    /// <summary>
    /// Represents the collection of OutboxMessage entities used by MassTransit to persist individual messages/events
    /// to be published after the database transaction commits. These messages are stored atomically alongside your
    /// domain data and later dispatched by the outbox dispatcher.
    /// </summary>
    DbSet<OutboxMessage> OutboxMessages { get; set; }
    
    /// <summary>
    /// Saves all changes added to the DbSets of this DbContext.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Saves all changes added to the DbSets of this DbContext.
    /// </summary>
    int SaveChanges();

}
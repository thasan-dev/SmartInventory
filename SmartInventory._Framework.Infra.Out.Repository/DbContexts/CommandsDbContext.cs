using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts;

public abstract class CommandsDbContext(DbContextOptions options) : DbContext(options), ICommandsDbContext
{
    /// <summary>
    /// Configures how entities are mapped to database records.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();  
        
        // configure common aggregates
        ConfigureEntityTables(modelBuilder);
    }
    
    protected abstract void ConfigureEntityTables(ModelBuilder modelBuilder);
}
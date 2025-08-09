using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts;

public abstract class CommandsDbContext(DbContextOptions options) : DbContext(options), ICommandsDbContext
{
    ///<inheritdoc />
    public DbSet<OutboxState> OutboxStates { get; set; }

    ///<inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    
    /// <summary>
    /// Configures how entities are mapped to database records.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureMassTransitOutboxEntities(modelBuilder);
        
        // configure aggregates
        ConfigureEntityTables(modelBuilder);
    }
   
    protected abstract void ConfigureEntityTables(ModelBuilder modelBuilder);

    private void ConfigureMassTransitOutboxEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();  
    }
}
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts.DbConfigurations;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts;

public abstract class CommandsDbContext(DbContextOptions options) : DbContext(options), ICommandsDbContext
{
    /// <summary>
    /// Configures how entities are mapped to database records.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // configure common aggregates
        ConfigureEntityTables(modelBuilder);
    }
    
    protected abstract void ConfigureEntityTables(ModelBuilder modelBuilder);
}
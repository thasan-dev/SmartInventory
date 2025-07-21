using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;
using SmartInventory._Framework.Infra.Out.Repository.DbContexts.DbConfigurations;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts;

public abstract class CommandsDbContext<TData>(DbContextOptions options) : DbContext(options), ICommandsDbContext<TData> where TData : DomainEventData
{
    /// <inheritdoc />
    public DbSet<DomainEvent<TData>> DomainEvents { get; private set; } = default!;

    /// <summary>
    /// Configures how entities are mapped to database records.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DomainEventsDbConfiguration<TData>());
        ConfigureEntityTables(modelBuilder);
    }
    
    protected abstract void ConfigureEntityTables(ModelBuilder modelBuilder);
}
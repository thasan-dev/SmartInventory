using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts.DbConfigurations;

public class DomainEventsDbConfiguration: IEntityTypeConfiguration<DomainEvent>
{
    public void Configure(EntityTypeBuilder<DomainEvent> builder)
    {
        builder.ToTable("DomainEvents");
        
        builder.HasKey(d => d.Id);
        builder
            .Property(d => d.Id)
            .HasConversion(id => id.Value, idValue => DomainEventId.Create(idValue));
        
        builder
            .Property(d => d.AggregateRootName)
            .HasConversion(name => name.Value, nameValue => AggregateRootName.Create(nameValue))
            .IsRequired();

        builder
            .Property(d => d.AggregateRootId)
            .HasConversion(id => id.Value, idValue => AggregateRootId.Create(idValue))
            .IsRequired();

        builder
            .Property(d => d.MicroserviceName)
            .HasConversion(name => name.Value, nameValue => new MicroserviceName(nameValue));
        
        builder
            .Property(d=>d.IsPublished)
            .HasConversion(p => p.Value, value => new IsPublished(value))
            .IsRequired();
        builder
            .HasIndex(d => d.IsPublished)
            .IsClustered(false);
    }
}
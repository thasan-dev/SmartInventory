using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity.ValueObjects;

namespace SmartInventory._Framework.Infra.Out.Repository.DbContexts.DbConfigurations;

public class DomainEventsDbConfiguration<TData>: IEntityTypeConfiguration<DomainEvent<TData>>
where TData: DomainEventData
{
    public void Configure(EntityTypeBuilder<DomainEvent<TData>> builder)
    {
        builder.ToTable("DomainEvents");
        
        builder.HasKey(d => d.Id);
        builder
            .Property(d => d.Id)
            .HasConversion(id => id.Value, idValue => DomainEventId.Create(idValue));
        
        builder
            .Property(d => d.Name)
            .HasConversion(p => p.Value, value => DomainEventName.Create(value))
            .IsRequired();
        
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
            .Property(d => d.DomainEventData)
            .HasConversion(data => data.DataAsJson, value => (DomainEventData.Create(value) as TData)!)
            .IsRequired();
        
        builder
            .Property(d=>d.IsPublished)
            .HasConversion(p => p.Value, value => new IsPublished(value))
            .IsRequired();
        builder
            .HasIndex(d => d.IsPublished)
            .IsClustered(false);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.Repository.DbConfigurations;

public class PlantConfiguration: IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.ToTable("Plants");
        
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, val => PlantId.Create(val))
            .IsRequired();
        
        builder
            .Property(p => p.Name)
            .HasConversion(name => name.Value, val => PlantName.Create(val))
            .IsRequired();

        builder.Ignore(p => p.DomainEvent);
    }
}
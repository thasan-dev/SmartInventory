using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInventory.Inventories.QueryModel;

namespace SmartInventory.Inventories.Queries.Infra.Out.Repository.DbConfigurations;

public class PlantDbConfiguration: IEntityTypeConfiguration<PlantQueryModel>
{
    public void Configure(EntityTypeBuilder<PlantQueryModel> builder)
    {
        builder.ToSqlQuery("SELECT Id, PlantName FROM Plants");

        builder.Metadata.SetIsTableExcludedFromMigrations(true);
    }
}

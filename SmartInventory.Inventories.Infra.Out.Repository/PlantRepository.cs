using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository.Repositories;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Messages.Inventories;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.Repository;
public class PlantRepository(
    IInventoriesCommandsDbContext dbContext,
    IPublishEndpoint publishEndpoint)
    :
        CommandsRepository<Plant, PlantId, PlantDomainEventMessage>(dbContext, publishEndpoint), IPlantRepository
{
    protected override DbSet<Plant> DbSet  => dbContext.Plants;

    protected override string MicroserviceName => "Inventories";

    public async Task<Plant?> GetByIdAsync(PlantId id)
    {
        return await DbSet.FindAsync(id);
    }
}
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository.Repositories;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
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
}
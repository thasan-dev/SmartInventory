using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.Repository;


public class PlantRepository( IInventoriesCommandsDbContext dbContext,
    IPublishEndpoint publishEndpoint) : 
    DefaultCommandRepository<Plant, PlantId, PlantDomainEvent>(dbContext,publishEndpoint),IPlantRepository
{
    protected override DbSet<Plant> DbSet  => dbContext.Plants;
}
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SmartInventory._Framework.Infra.Out.Repository;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.Repository;


public class PlantRepository : 
    DefaultCommandRepository<Plant, PlantId, PlantDomainEvent>,IPlantRepository
{
    private readonly IInventoriesCommandsDbContext _dbContext;

    public PlantRepository(IInventoriesCommandsDbContext dbContext,
        IPublishEndpoint publishEndpoint) : base(dbContext,publishEndpoint)
    {
        _dbContext = dbContext;
    }

    protected override DbSet<Plant> DbSet  => _dbContext.Plants;
}
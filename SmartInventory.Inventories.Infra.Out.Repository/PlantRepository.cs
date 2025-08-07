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

    public new async Task CreateAsync(Plant plant)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            DbSet.Add(plant);
            
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            //await PublishEndpoint.Publish(aggregateRoot.DomainEvent);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync();
        }
    }

    protected override DbSet<Plant> DbSet  => _dbContext.Plants;
}
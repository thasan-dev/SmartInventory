using MassTransit;
using SmartInventory._Framework.DomainModel.Events;
using SmartInventory.Messages.Inventories;



namespace SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories;

public class PlantDomainEventConsumer : IConsumer<DomainEventMessage<PlantDomainEventMessage>>
{
    public Task Consume(ConsumeContext<DomainEventMessage<PlantDomainEventMessage>> context)
    {
        Console.WriteLine($"Consuming {context.Message.Payload.Name}");
        return Task.CompletedTask;
    }
}
using MassTransit;
using SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories.Messages;


namespace SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories;

public class PlantDomainEventConsumer: IConsumer<PlantDomainEvent>
{
    public Task Consume(ConsumeContext<PlantDomainEvent> context)
    {
        Console.WriteLine($"Consuming {context.Message.Message.Name}");
        return Task.CompletedTask;
    }
}
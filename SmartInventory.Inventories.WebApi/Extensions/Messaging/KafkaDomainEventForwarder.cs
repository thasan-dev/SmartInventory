using MassTransit;
using AclMessages = SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories.Messages;
using DomainEvents = SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;

namespace SmartInventory.Inventories.WebApi.Extensions.Messaging;

/// <summary>
/// Forwards domain events from the in-memory bus (EF Outbox) to Kafka topics.
/// Bridges the transactional outbox pattern with Kafka pub/sub.
/// </summary>
public class KafkaDomainEventForwarder(
    ITopicProducer<AclMessages.PlantDomainEvent> topicProducer)
    : IConsumer<DomainEvents.PlantDomainEvent>
{
    public async Task Consume(ConsumeContext<DomainEvents.PlantDomainEvent> context)
    {
        var kafkaMessage = new AclMessages.PlantDomainEvent
        {
            Message = new AclMessages.PlantDomainEventMessage
            {
                Id = context.Message.Message.Id,
                Name = context.Message.Message.Name
            }
        };

        await topicProducer.Produce(kafkaMessage, context.CancellationToken);
    }
}

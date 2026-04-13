using MassTransit;
using SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories;
using SmartInventory.Inventories.Repository;
using SmartInventory.Inventories.WebApi.Extensions.Messaging;
using AclMessages = SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories.Messages;

namespace SmartInventory.Inventories.WebApi.Extensions;

public static class KafkaServiceExtensions
{
    /// <summary>
    /// Configures MassTransit with Kafka Rider for pub/sub messaging.
    /// Uses an in-memory bus with EF Outbox to preserve transactional guarantees,
    /// and a forwarding consumer to bridge events from the outbox to Kafka topics.
    /// </summary>
    public static void AddMassTransitUsingKafka(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var topicPrefix = configuration["Kafka:TopicPrefix"] ?? "smartinventory";
        var inventoriesTopic = $"{topicPrefix}.inventories";

        services.AddMassTransit(config =>
        {
            config.AddEntityFrameworkOutbox<InventoriesCommandsDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(10);
            });

            // Forwarding consumer: receives events from the in-memory bus (via outbox)
            // and produces them to the Kafka topic
            config.AddConsumer<KafkaDomainEventForwarder>();

            // In-memory bus as the base transport for the EF Outbox
            config.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            // Kafka Rider for pub/sub
            config.AddRider(rider =>
            {
                rider.AddConsumer<PlantDomainEventConsumer>();

                rider.AddProducer<AclMessages.PlantDomainEvent>(inventoriesTopic);

                rider.UsingKafka((context, k) =>
                {
                    k.Host(bootstrapServers);

                    k.TopicEndpoint<AclMessages.PlantDomainEvent>(
                        inventoriesTopic,
                        $"{topicPrefix}-inventories-group",
                        e =>
                        {
                            e.ConfigureConsumer<PlantDomainEventConsumer>(context);
                        });
                });
            });
        });
    }
}

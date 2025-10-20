using Asp.Versioning;
using MassTransit;
using Microsoft.OpenApi.Models;
using SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents.Consumers.Inventories;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;
using SmartInventory.Inventories.Repository;

namespace SmartInventory.Inventories.WebApi.Extensions;

public static class ServiceExtensionss
{
    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1.0", new OpenApiInfo
            {
                Title = "Inventories", Description = "A Smart Inventory API", Version = "v1", Contact =
                    new OpenApiContact
                    {
                        Name = "Tanveer Hasan"
                    }
            });
            options.SwaggerDoc("v2.0", new OpenApiInfo { Title = "Inventories", Version = "v2" });
        });
        services.AddApiVersioning(option =>
        {
            option.DefaultApiVersion = ApiVersion.Default;
            option.ReportApiVersions = true;
            option.ApiVersionReader = new UrlSegmentApiVersionReader();
            option.AssumeDefaultVersionWhenUnspecified = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VV";
            options.SubstituteApiVersionInUrl = true;
        });
    }

    public static void AddMassTransit(this IServiceCollection services)
    {
        services.AddMassTransit(config =>
        {
            config.AddEntityFrameworkOutbox<InventoriesCommandsDbContext>(o =>
            {
                // configure which database lock provider to use (Postgres, SqlServer, or MySql)
                o.UseSqlServer();

                // enable the bus outbox
                o.UseBusOutbox();
                o.QueryDelay = TimeSpan.FromSeconds(10);
            });

            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                //PublishBrokerTopologyOptions.FlattenHierarchy: Prevents MassTransit from creating separate exchanges per message type. Forces all messages to use a single exchange
                // We are publishing messages to exchanges for pub/sub messaging.
                // In the next line we have defined the exchange name to : exchange.inventories
                cfg.PublishTopology.BrokerTopologyOptions =
                    PublishBrokerTopologyOptions
                        .FlattenHierarchy;

                cfg.Message<PlantDomainEvent>(m =>
                {
                    m.SetEntityName("exchange.inventories");
                }); // MassTransit will use the inventories exchange for all DomainEvent messages type

                // configure consumers.
                cfg.ReceiveEndpoint("queue.inventories", endpoint =>
                {
                    endpoint.Bind("exchange.inventories"); // Bind queue to exchange
                    endpoint.ConfigureConsumer<PlantDomainEventConsumer>(context);
                });

                //cfg.ConfigureEndpoints(context);
            });

            config.AddConsumer<PlantDomainEventConsumer>();
        });
    }
}
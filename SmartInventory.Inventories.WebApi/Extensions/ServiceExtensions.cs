using Asp.Versioning;
using MassTransit;
using Microsoft.OpenApi.Models;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;
using SmartInventory.Inventories.Repository;

namespace SmartInventory.Inventories.WebApi.Extensions;

public static class ServiceExtensions
{
    public static void AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1.0", new OpenApiInfo
            {
                Title = "Inventories", Description = "A Smart Inventory API", Version = "v1", Contact = new OpenApiContact
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
            config.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
        
                cfg.PublishTopology.BrokerTopologyOptions =
                    PublishBrokerTopologyOptions
                        .FlattenHierarchy; //Prevents MassTransit from creating separate exchanges per message type. Forces all messages to use a single exchange
                cfg.SendTopology.UseCorrelationId<DomainEvent>(x =>
                    x.Id.Value); // RabbitMQ will store DomainEventId in the CorrelationId header.
                cfg.Message<DomainEvent>(m =>
                    m.SetEntityName(
                        "exchange.inventories")); // MassTransit will use the inventories exchange for all DomainEvent messages

                // configure consumers.
                cfg.ReceiveEndpoint("queue.inventories", e =>
                {
                    e.Bind("exchange.inventories"); // Bind queue to exchange

                });
            });
    
            config.AddEntityFrameworkOutbox<InventoriesCommandsDbContext>(o =>
            {
                // configure which database lock provider to use (Postgres, SqlServer, or MySql)
                o.UseSqlServer();

                // enable the bus outbox
                o.UseBusOutbox();
            });
        });
    }
}
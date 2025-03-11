using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using SmartInventory._Framework.DomainModel.Entities.DomainEventEntity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddSwaggerGen(options =>
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
builder.Services.AddApiVersioning(option =>
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

builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq://localhost");

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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName);
        }
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
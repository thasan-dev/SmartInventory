using Asp.Versioning.ApiExplorer;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using SmartInventory._Framework.Util.Exceptions.GlobalExceptionHandlers;
using SmartInventory.Inventories.Application.Plants;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.Queries.Infra.Out.Repository;
using SmartInventory.Inventories.Repository;
using SmartInventory.Inventories.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
builder.Services.AddProblemDetails();

// Configure Serilog
builder.Logging.ClearProviders();

builder.Host.AddSerilogLogging();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
             .ConfigureResource(resource =>
            {
                resource.AddService("InventoryService", "smart-inventory");
            })
            .AddConsoleExporter()
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri("http://localhost:4318/v1/traces"); ;
                otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });

    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation() // CPU, GC, memory
            .ConfigureResource(resource =>
            {
                resource.AddService("InventoryService", "smart-inventory");
                resource.AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName
                });
            })
            //.AddConsoleExporter()
            .AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri("http://localhost:4318/v1/metrics");
                otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            });
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

// Add DbContexts
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var assemblyName = typeof(Program).Assembly.GetName().Name!;

builder.Services.AddDbContext<InventoriesCommandsDbContext>(option =>
{
    option.UseSqlServer(connectionString,
        sqlServerOptions => sqlServerOptions.MigrationsAssembly(assemblyName));
});

builder.Services.AddDbContext<InventoriesQueriesDbContext>(option =>
{
    option.UseSqlServer(connectionString,
        sqlServerOptions => sqlServerOptions.MigrationsAssembly(assemblyName));
});

builder.Services.AddScoped<IInventoriesCommandsDbContext>(service => service.GetRequiredService<InventoriesCommandsDbContext>());

// Add Application services
builder.Services.AddScoped<IPlantApplicationService, PlantApplicationService>();

// Add Repository
builder.Services.AddScoped<IPlantRepository, PlantRepository>();

builder.Services.AddMessageBroker(builder.Configuration);
builder.Services.AddSwagger();

builder.Services.AddAuthorization(option =>
{
    option.AddPolicy("UserRole", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("role", "user");
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
app.UseSerilogRequestLogging();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
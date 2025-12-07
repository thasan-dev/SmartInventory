using Asp.Versioning.ApiExplorer;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using SmartInventory.Inventories.Application.Plants;
using SmartInventory.Inventories.DomainModel.PlantAggregate;
using SmartInventory.Inventories.Queries.Infra.Out.Repository;
using SmartInventory.Inventories.Repository;
using SmartInventory.Inventories.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

// Configure Serilog

builder.Host.UseSerilog((context, configBuilder) =>
{
    configBuilder.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

// Add DbContexts
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var assemblyName = typeof(Program).Assembly.GetName().Name!;

builder.Services.AddDbContext<InventoriesCommandsDbContext>(option =>
{
    option.UseSqlServer(connectionString,
        sqlServerOptionsAction => sqlServerOptionsAction.MigrationsAssembly(assemblyName));
});

builder.Services.AddDbContext<InventoriesQueriesDbContext>(option =>
{
    option.UseSqlServer(connectionString,
        sqlServerOptionsAction => sqlServerOptionsAction.MigrationsAssembly(assemblyName));
});

builder.Services.AddScoped<IInventoriesCommandsDbContext>(service => service.GetRequiredService<InventoriesCommandsDbContext>());

// Add Application services
builder.Services.AddScoped<IPlantApplicationService, PlantApplicationService>();

// Add Repository
builder.Services.AddScoped<IPlantRepository, PlantRepository>();

builder.Services.AddMassTransitUsingRabbitMq();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
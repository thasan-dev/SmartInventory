# Copilot Instructions for SmartInventory

## Build & Run

```bash
# Build the solution
dotnet build SmartInventory.sln

# Run the API (entry point)
dotnet run --project SmartInventory.Inventories.WebApi/SmartInventory.Inventories.WebApi.csproj

# Add an EF Core migration
dotnet ef migrations add <MigrationName> --project SmartInventory.Inventories.Infra.Out.Repository --startup-project SmartInventory.Inventories.WebApi

# Apply migrations
dotnet ef database update --project SmartInventory.Inventories.Infra.Out.Repository --startup-project SmartInventory.Inventories.WebApi
```

> There are no test projects in the solution currently.

## Architecture Overview

This is a **.NET 8 CQRS + DDD microservice** for inventory management. The solution is split into **14 projects** organized by layer and direction.

### Project Naming Convention

`SmartInventory.{BoundedContext}.{Layer}.{Direction}.{Technology}`

- **Direction**: `In` = inbound (entry points), `Out` = outbound (dependencies)
- **Layer**: `Infra` = infrastructure, `Application` = app services, `DomainModel` = domain

Examples:
- `Inventories.Commands.Infra.In.RestApi` — HTTP endpoints for write operations
- `Inventories.Queries.Infra.Out.Repository` — read-side database access
- `Inventories.Commands.AntiCorruption.In.DomainEvents` — message consumer ACL

### Request Flows

**Command (write) flow:**
```
HTTP POST → Commands.Infra.In.RestApi (Controller)
         → Inventories.Application (IPlantApplicationService)
         → Inventories.DomainModel (PlantFactory + Plant aggregate)
         → Infra.Out.Repository (PlantRepository + MassTransit Outbox)
         → SQL Server + RabbitMQ/Azure Service Bus
```

**Query (read) flow:**
```
HTTP GET → Queries.Infra.In.RestApi (Controller)
         → Queries.Infra.Out.Repository (InventoriesQueriesDbContext)
         → SQL Server (dedicated read DB context, no outbox)
```

**Anti-corruption layer:**
```
RabbitMQ/ASB → Commands.AntiCorruption.In.DomainEvents (PlantDomainEventConsumer)
             → Translates external events → internal domain operations
```

### Key Projects

| Project | Role |
|---|---|
| `Inventories.WebApi` | Single host entry point: DI wiring, auth, migrations, Docker |
| `Inventories.DomainModel` | Aggregates, value objects, domain commands & events |
| `Inventories.Application` | Application services orchestrating domain logic |
| `_Framework.*` (5 projects) | Reusable DDD/CQRS base classes shared across bounded contexts |

## Key Conventions

### DDD Aggregate Structure
Each aggregate lives in `DomainModel/{AggregateName}Aggregate/` with this layout:
```
PlantAggregate/
  Plant.cs                         # Aggregate root (extends InventoryAggregateRoot)
  PlantFactory.cs                  # Static factory enforcing creation rules
  IPlantRepository.cs              # Domain repository interface
  ValueObjects/PlantId.cs          # Strongly-typed ID extending EntityId
  ValueObjects/PlantName.cs        # Value objects extending ValueObject
  DomainCommands/CreatePlantDomainCommand.cs  # Internal domain commands (records)
  DomainEvents/PlantDomainEvent.cs            # Event entity for outbox
  DomainEvents/PlantDomainEventMessage.cs     # Message DTO for publishing
```

### Base Class Hierarchy
- `AggregateRoot<TId, TDomainEvent>` (in `_Framework.DomainModel`) → `InventoryAggregateRoot<TId, TDomainEvent>` (in `Inventories.DomainModel`) → concrete aggregates
- `Entity<TId>` → aggregate and entity classes
- `ValueObject` → all value objects (equality by value components)
- `EntityId` → all strongly-typed ID value objects (wraps `Guid`)

### Repository Pattern
The generic `CommandsRepository<TAggregateRoot, TEntityId, TDomainEvent>` in `_Framework.Infra.Out.Repository` handles:
- Transactional save + domain event publishing (via MassTransit Outbox)
- `ConcurrencyException` and `DbUpdateException` handling

Domain repositories implement the domain interface and extend this generic base.

### Two DbContexts
- `InventoriesCommandsDbContext` — extends `CommandsDbContext<T>` which configures the MassTransit OutBox tables; used for all writes
- `InventoriesQueriesDbContext` — plain `DbContext` with `IQueryModel` entities; used for reads only

### Query Models
Read models implement `IQueryModel` (from `_Framework.QueryModel`) and are separate EF entities in the queries DB context.

### HTTP Command DTOs vs Domain Commands
- **HTTP layer** uses `CreatePlantCommand` (plain class with `[Required]` validation)
- **Application layer** maps these to `CreateOrUpdatePlantCommand` (record)
- **Domain layer** receives `CreatePlantDomainCommand` (record extending `DomainCommand`)

### API Versioning
URL-segment versioning (`/inventories/v1/plants`, `/inventories/v2/plants`). Swagger groups by version (`v1.0`, `v2.0`). Controllers use `[ApiVersion]` attributes.

### Message Bus
Two configurations exist side-by-side in `ServiceExtensions.cs`:
- `AddMassTransitUsingRabbitMq()` — for local/dev
- `AddMassTransitUsingAzureServiceBus()` — for production

Exchange/queue names follow the pattern: `exchange.inventories`, `queue.inventories`.

### Infrastructure
- **Auth**: Azure AD B2C JWT bearer tokens configured in `WebApi/Program.cs`
- **Logging**: Serilog → OpenTelemetry sink (`http://localhost:4318`)
- **Migrations**: Located in `Inventories.WebApi/Migrations/`, applied at startup via `app.MigrateDatabase()`
- **Exception handling**: Global `BusinessExceptionHandler` middleware; domain throws `BusinessException` or `InvalidDataException` from `_Framework.Util`

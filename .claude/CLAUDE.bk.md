# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build the solution
dotnet build SmartInventory.sln

# Run the API
dotnet run --project SmartInventory.Inventories.WebApi/SmartInventory.Inventories.WebApi.csproj

# Add an EF Core migration
dotnet ef migrations add <MigrationName> \
  --project SmartInventory.Inventories.Infra.Out.Repository \
  --startup-project SmartInventory.Inventories.WebApi

# Apply migrations manually
dotnet ef database update \
  --project SmartInventory.Inventories.Infra.Out.Repository \
  --startup-project SmartInventory.Inventories.WebApi
```

There are no test projects in the solution currently. Migrations are applied automatically at startup via `app.MigrateDatabase()`.

## Architecture Overview

**.NET 8 CQRS + DDD microservice** for inventory management. 14 projects organized by bounded context and layer.

### Project Naming Convention

`SmartInventory.{BoundedContext}.{Layer}.{Direction}.{Technology}`

- **Direction**: `In` = inbound (entry points), `Out` = outbound (dependencies)
- **Layer**: `Infra`, `Application`, `DomainModel`, `QueryModel`

Examples:
- `Inventories.Commands.Infra.In.RestApi` — HTTP POST endpoints (write side)
- `Inventories.Queries.Infra.Out.Repository` — read-side DB access
- `Inventories.Commands.AntiCorruption.In.DomainEvents` — message consumer ACL

### Request Flows

**Command (write):**
```
HTTP POST → Commands.Infra.In.RestApi (Controller)
          → Inventories.Application (IPlantApplicationService)
          → Inventories.DomainModel (PlantFactory + Plant aggregate)
          → Infra.Out.Repository (PlantRepository + MassTransit Outbox)
          → SQL Server + message broker (RabbitMQ / Azure Service Bus / Kafka)
```

**Query (read):**
```
HTTP GET → Queries.Infra.In.RestApi (Controller)
         → Queries.Infra.Out.Repository (InventoriesQueriesDbContext)
         → SQL Server (dedicated read context, no outbox)
```

**Anti-corruption layer:**
```
Message broker → Commands.AntiCorruption.In.DomainEvents (PlantDomainEventConsumer)
               → Translates external events → internal domain operations
```

### Framework Layer (`_Framework.*`)

Five reusable projects shared across bounded contexts:
- `_Framework.DomainModel` — `AggregateRoot<TId, TDomainEvent>`, `Entity<TId>`, `ValueObject`, `EntityId`
- `_Framework.Infra.Out.Repository` — Generic `CommandsRepository<T>` + `CommandsDbContext<T>` (configures MassTransit outbox tables)
- `_Framework.QueryModel` — `IQueryModel` marker interface for read models
- `_Framework.Util` — `BusinessException`, `InvalidDataException`, `BusinessExceptionHandler` middleware
- `_Framework.Infra.Out.DomainEventApiProxy` — event proxy utilities

## Key Conventions

### DDD Aggregate Structure

Each aggregate lives in `DomainModel/{AggregateName}Aggregate/`:
```
PlantAggregate/
  Plant.cs                                     # Aggregate root
  PlantFactory.cs                              # Static factory enforcing creation rules
  IPlantRepository.cs                          # Domain repository interface
  ValueObjects/PlantId.cs                      # Strongly-typed ID (extends EntityId)
  ValueObjects/PlantName.cs                    # Value object (extends ValueObject)
  DomainCommands/CreatePlantDomainCommand.cs   # Internal domain command (record)
  DomainEvents/PlantDomainEvent.cs             # Event entity stored in outbox
  DomainEvents/PlantDomainEventMessage.cs      # Message DTO for publishing
```

**Base class hierarchy:**
```
Entity<TId>
  └─ AggregateRoot<TId, TDomainEvent>          [_Framework.DomainModel]
       └─ InventoryAggregateRoot<TId, TDomainEvent>  [Inventories.DomainModel]
            └─ Plant
```

**Domain event naming:** `InventoryAggregateRoot` auto-generates event names via `RaiseDomainEvent<TAggregateRoot>(DomainEventType)` — produces `{AggregateName}Created` or `{AggregateName}Updated`. A second overload accepts a custom event name string.

### Repository Pattern

`CommandsRepository<TAggregateRoot, TEntityId, TDomainEvent>` (in `_Framework.Infra.Out.Repository`) handles transactional save + domain event publishing via MassTransit Outbox, catching `DbUpdateConcurrencyException` / `DbUpdateException` with rollback. Domain repositories implement the domain interface and extend this base. Note: `UpdateAsync()` is not yet implemented (throws `NotImplementedException`).

### Two DbContexts

- `InventoriesCommandsDbContext` — extends `CommandsDbContext<T>`; includes MassTransit outbox tables (`OutboxState`, `OutboxMessage`, `InboxState`); used for all writes
- `InventoriesQueriesDbContext` — plain `DbContext`; `IQueryModel` entities with `AsNoTracking()` queryables; used for reads only

### Command DTO Mapping (Three Layers)

1. **HTTP layer**: `CreatePlantCommand` — plain class with `[Required]` validation attributes
2. **Application layer**: `CreateOrUpdatePlantCommand` — record
3. **Domain layer**: `CreatePlantDomainCommand` — record (e.g. `(Guid PlantId, string Name)`)

### Message Bus

Configured via `"MessageBroker"` key in `appsettings.json`. Three backends:

- **`"RabbitMq"`** — local/dev default. Uses `PublishBrokerTopologyOptions.FlattenHierarchy` (single exchange, no per-message-type exchanges). Exchange: `exchange.inventories`, queue: `queue.inventories`. Configured in `ServiceExtensions.cs` → `AddMassTransitUsingRabbitMq()`.

- **`"AzureServiceBus"`** — production. Configured in `ServiceExtensions.cs` → `AddMassTransitUsingAzureServiceBus()`.

- **`"Kafka"`** — hybrid architecture using in-memory bus + EF Outbox + Kafka Rider. Domain events flow: aggregate → in-memory bus outbox → `KafkaDomainEventForwarder` (MassTransit consumer that bridges to Kafka) → Kafka topic `{TopicPrefix}.inventories`. Configured in `KafkaServiceExtensions.cs` → `AddMassTransitUsingKafka()`. Consumer group: `{TopicPrefix}-inventories-group`.

All brokers use MassTransit EF Outbox with `QueryDelay = 10 seconds` and SQL Server lock provider.

### API Versioning

URL-segment versioning: `/inventories/v{version:apiVersion}/[controller]` (e.g., `/inventories/v1/plants`). Swagger groups by `v1.0` and `v2.0`. Controllers annotated with `[ApiVersion(1.0)]`.

### Infrastructure

- **Auth**: Azure AD B2C JWT bearer; `"UserRole"` policy requires `role=user` claim; configured in `WebApi/Program.cs`
- **Logging**: Serilog → OpenTelemetry sink (`http://localhost:4318`), service name `InventoryService`
- **Tracing & Metrics**: OpenTelemetry OTLP (HTTP Protobuf) with AspNetCore, HttpClient, Runtime instrumentation for both traces and metrics
- **Exception handling**: `BusinessExceptionHandler` middleware; domain throws `BusinessException` subclasses → HTTP 422
- **Migrations**: Located in `Inventories.WebApi/Migrations/`; applied at startup

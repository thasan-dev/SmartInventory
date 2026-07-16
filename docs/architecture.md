# Architecture

**.NET 8 CQRS + DDD microservice** for inventory management. 15 projects in `SmartInventory.sln`, organized by bounded context and layer. The only aggregate currently implemented is **Plant**.

## Solution Folder Structure

```
SmartInventory.sln
│
├─ SmartInventory.Inventories.Commands.Infra.In.RestApi/   # WRITE side HTTP entry
│   └─ Controllers/V1/Plants/
│       ├─ PlantController.cs
│       └─ Commands/CreatePlantCommand.cs                  # HTTP DTO ([Required] validation)
│
├─ SmartInventory.Inventories.Queries.Infra.In.RestApi/    # READ side HTTP entry
│   └─ Controllers/V1/PlantController.cs
│
├─ SmartInventory.Inventories.Application/                 # Use-case orchestration
│   └─ Plants/
│       ├─ IPlantApplicationService.cs
│       ├─ PlantApplicationService.cs
│       └─ ApplicationCommands/CreateOrUpdatePlantCommand.cs   # Application DTO (record)
│
├─ SmartInventory.Inventories.DomainModel/                 # Aggregates / domain logic
│   └─ PlantAggregate/
│       ├─ Plant.cs                                        # Aggregate root
│       ├─ PlantFactory.cs                                 # Static factory enforcing creation rules
│       ├─ IPlantRepository.cs                             # Domain repository interface
│       ├─ ValueObjects/{PlantId,PlantName}.cs
│       ├─ DomainCommands/{CreatePlantDomainCommand,UpdatePlantDomainCommand}.cs
│       └─ DomainEvents/PlantEventData.cs
│
├─ SmartInventory.Inventories.Infra.Out.Repository/        # WRITE persistence
│   ├─ InventoriesCommandsDbContext.cs                     # extends CommandsDbContext<T> (+ outbox)
│   ├─ PlantRepository.cs                                  # implements IPlantRepository
│   └─ DbConfigurations/PlantConfiguration.cs
│
├─ SmartInventory.Inventories.Queries.Infra.Out.Repository/ # READ persistence
│   ├─ InventoriesQueriesDbContext.cs                      # plain DbContext, AsNoTracking
│   └─ DbConfigurations/PlantDbConfiguration.cs
│
├─ SmartInventory.Inventories.QueryModel/                  # Read models
│   └─ PlantQueryModel.cs
│
├─ SmartInventory.Inventories.Commands.AntiCorruption.In.DomainEvents/  # Inbound message ACL
│   └─ Consumers/Inventories/PlantDomainEventConsumer.cs
│
├─ SmartInventory.Messages.Inventories/                    # Shared published message contracts
│   └─ PlantDomainEventMessage.cs
│
├─ SmartInventory.Inventories.WebApi/                      # Host / composition root
│   ├─ Program.cs
│   ├─ Extensions/{ServiceExtensions,HostBuilderExtensions}.cs
│   └─ Migrations/                                         # EF migrations (auto-applied at startup)
│
└─ _Framework.*  (5 reusable projects — see below)
```

## Framework Layer (`_Framework.*`)

Five reusable projects shared across bounded contexts:

- `_Framework.DomainModel` — `AggregateRoot<TId, TDomainEvent>`, `Entity<TId>`, `EntityId`, `ValueObject`, `DomainCommand`, `DomainEvent`, and event value objects.
- `_Framework.Infra.Out.Repository` — generic `CommandsRepository<T>` + `CommandsDbContext<T>` (configures MassTransit outbox tables).
- `_Framework.QueryModel` — `IQueryModel` marker interface for read models.
- `_Framework.Util` — `BusinessException`, `InvalidDataException`, `BusinessExceptionHandler` middleware, `DataAssertion`.
- `_Framework.Infra.Out.DomainEventApiProxy` — `DomainEventsPublisher` / `IDomainEventsPublisher`.

## Request Flows

**Command (write):**
```
HTTP POST → Commands.Infra.In.RestApi (PlantController)
          → Inventories.Application (IPlantApplicationService)
          → Inventories.DomainModel (PlantFactory + Plant aggregate)
          → Infra.Out.Repository (PlantRepository + MassTransit Outbox)
          → SQL Server + message broker (RabbitMQ / Azure Service Bus)
```

**Query (read):**
```
HTTP GET → Queries.Infra.In.RestApi (PlantController)
         → Queries.Infra.Out.Repository (InventoriesQueriesDbContext, AsNoTracking)
         → SQL Server (dedicated read context, no outbox)
```

**Anti-corruption layer (inbound messages):**
```
Message broker → Commands.AntiCorruption.In.DomainEvents (PlantDomainEventConsumer)
               → translates external events → internal domain operations
```

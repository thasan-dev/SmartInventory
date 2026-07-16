# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build SmartInventory.sln
# Run the API
dotnet run --project SmartInventory.Inventories.WebApi/SmartInventory.Inventories.WebApi.csproj
```

## Architecture

**.NET 8 CQRS + DDD microservice** for inventory management — 15 projects in `SmartInventory.sln`, organized by bounded context + CQRS side + layer. Only the **Plant** aggregate is implemented so far.

Full solution structure, framework layer (`_Framework.*`), and command/query/ACL request flows: see [docs/architecture.md](../docs/architecture.md).

## Key Conventions

- **DDD aggregate layout**: each aggregate lives in `DomainModel/{Name}Aggregate/` with the root, a static factory, the domain repository interface, `ValueObjects/`, `DomainCommands/`, and `DomainEvents/`. New aggregates mirror this exact layout. To **scaffold a new aggregate**, use the `ddd-aggregate` skill — it owns the full file-by-file templates and build order.
- **Base class hierarchy**: `Entity<TId>` → `AggregateRoot<TId>` → `Plant`. The root extends `AggregateRoot<{Name}Id>` (single type parameter) and implements `IPublishDomainEvents<{Name}DomainEventMessage>`, exposing `GetEventPayload()`.
- **Three-layer command DTO flow**: HTTP `CreatePlantCommand` → application `CreateOrUpdatePlantCommand` → domain `CreatePlantDomainCommand` (one DTO per layer; class-vs-record rules under Code Style).
- **Two DbContexts**: `InventoriesCommandsDbContext` (writes, includes outbox tables `OutboxState`/`OutboxMessage`/`InboxState`) vs `InventoriesQueriesDbContext` (reads, plain `DbContext`, `AsNoTracking`).
- **Repository pattern**: `CommandsRepository<TAggregateRoot, TEntityId, TDomainEvent>` handles transactional save + domain-event publishing via MassTransit Outbox, with rollback on `DbUpdateConcurrencyException` / `DbUpdateException`.
- **Dependency direction**: entry points depend inward (`Infra.In → Application → DomainModel`); the domain never references an `Out` infra project. Verifiable from `.csproj` references.

### Project Naming

`SmartInventory.{BoundedContext}.{Side}.{Layer}.{Direction}.{Suffix}`

- **BoundedContext**: the DDD business area (e.g. `Inventories`).
- **Side**: CQRS side — `Commands` (write) or `Queries` (read).
- **Layer**: `Infra`, `Application`, `DomainModel`, `QueryModel`.
- **Direction**: `In` = inbound (entry points / consumers), `Out` = outbound (dependencies / infra).
- **Suffix**: the concrete adapter (e.g. `RestApi`, `Repository`, `DomainEvents`).
- **`_Framework.*`** projects are reusable building blocks shared across bounded contexts.

Examples:
- `Inventories.Commands.Infra.In.RestApi` — HTTP POST endpoints (write side)
- `Inventories.Queries.Infra.Out.Repository` — read-side DB access
- `Inventories.Commands.AntiCorruption.In.DomainEvents` — message consumer (ACL)

### Folder Naming

- **Plural folder names** — `Plants/`, `ValueObjects/`, `Controllers/`, `DomainCommands/`.
- **`_` prefix = shared/cross-cutting, not domain** — `_Framework.*` projects, `_Common/` folders. Signals "building block," and sorts to the top.

### File Naming

- **Singular type names inside plural folders** — `Plants/Plant.cs`, `Plants/PlantController.cs`; aggregate root is singular (`PlantAggregate/Plant.cs`).
- **One type per file** — aggregates, value objects, domain events, controllers, and services each get their own file (even tiny VOs, e.g. `ValueObjects/PlantId.cs`, `PlantName.cs`). DTOs may differ (can be grouped).
- **Layer-suffixed type names** — the suffix marks the layer/role: `*DomainCommand`, `*QueryModel`, `*DbContext`, `*Repository`, `*Configuration`, `*Consumer`, `*Factory`, `*EventData`, `*Message`.
- **Namespace mirrors folder path** — one namespace per folder. (File-scoped syntax is a Code Style rule.)

### Persistence (EF Core)

- **One `IEntityTypeConfiguration` class per aggregate**, under `DbConfigurations/` (e.g. `PlantConfiguration.cs`).
- **Plural table names** — `builder.ToTable("Plants")`.

## Code Style

- **DTO types follow the layer**: HTTP command DTOs are **classes** with `[Required]`/data-annotation validation; application and domain commands are **records**. Keep validation attributes at the HTTP boundary only — application/domain records stay free of annotations.
- **Construct aggregates through their static factory** (`PlantFactory`), never with `new`. Creation/update rules live in the factory and aggregate, not in the application service.
- **Value objects extend `ValueObject`; strongly-typed IDs extend `EntityId`.** Don't pass raw `Guid`/`string` across domain boundaries where a value object exists.
- **File-scoped namespaces** (`namespace X;`), not block-scoped.

### REST API (controllers)

- **Controller attributes**: `[ApiController]` + `[ApiVersion(1.0)]`, route template `/inventories/v{version:apiVersion}/[controller]`.
- **Primary-constructor DI** into controllers, inheriting `ControllerBase` — e.g. `PlantController(IPlantApplicationService svc) : ControllerBase`.
- **Async actions** suffixed `*Async`, returning `Task<IActionResult>`; use HTTP-verb attributes (`[HttpPost]`, `[HttpGet("{id}")]`).
- **Thin controllers**: translate the HTTP DTO → application command, delegate to the application service, return `Ok(...)`. No business logic in controllers.

### Persistence (EF Core)

- **Configure mappings inside the `IEntityTypeConfiguration` class**, never inline in `OnModelCreating`.
- **Map strongly-typed IDs & value objects via `HasConversion`** (VO ↔ primitive), e.g. `HasConversion(id => id.Value, val => PlantId.Create(val))`.

### Exceptions

- **`BusinessException` is abstract — never throw it directly.** Throw `InvalidDataException` for invalid input/data, or define a domain-specific subclass of `BusinessException` for a violated business rule.
- **Throw from the domain** (aggregate, factory, value objects), not from controllers or the application service — keeps controllers thin and rules in the model.
- **Pass both messages**: `base(logErrorMessage, userErrorMessage)` — the log message is internal/detailed; the user message is client-safe. Use the two-arg constructor whenever the client needs a meaningful message.
- **Guard clauses use `DataAssertion.IsTrue(condition, "user message")`** — throws `InvalidDataException` when the condition is false.
- **`BusinessExceptionHandler` (`IExceptionHandler`) maps any `BusinessException` → HTTP 422** (`application/problem+json`). Anything that isn't a `BusinessException` falls through to a 500 — so only throw `BusinessException` subclasses for expected, client-facing failures.

## Repository Etiquette

- Branch off **`develop`** for day-to-day work; **`master`** is the stable/release branch.
- Match the project naming convention exactly when adding projects — the `{Layer}.{Direction}.{Technology}` segments are load-bearing for the architecture.

## Infrastructure

Message bus (MassTransit + RabbitMQ / Azure Service Bus + EF Outbox), API versioning, Azure AD B2C auth, Serilog → OpenTelemetry, and `BusinessException` → HTTP 422 handling.

Config keys, defaults, and runtime prerequisites: see [docs/infrastructure.md](../docs/infrastructure.md).

## Notes

- **EF Core migrations** — see @../docs/ef-migrations.md (commands, rules; migrations auto-apply at startup).

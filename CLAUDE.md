# CLAUDE.md
# SmartInventory

ASP.NET Core 10 Web API with EF Core
Design philosophy: Domain Driven Design
Database: Postgres
Messaging: Masstransit with AWS SQS & SNS, Uses Transactional Outbox
Testing: xUnit with Moq
Observablity: Otel, Serilog

## Commands

```bash
# Run the API (the WebApi project is the only host / composition root)
dotnet run --project SmartInventory.Inventories.WebApi/SmartInventory.Inventories.WebApi.csproj
```

## Architecture
Full solution structure, see [docs/architecture.md](docs/architecture.md).

## Key Conventions

1. **DDD aggregates**: use the `ddd-aggregate` skill — it owns the folder layout, base classes, value-object and factory rules, file-by-file templates, and build order for anything under `DomainModel/{Name}Aggregate/`.

2. **Three-layer command DTO flow**: HTTP `CreatePlantCommand` → application `CreateOrUpdatePlantCommand` → domain `CreatePlantDomainCommand` (one DTO per layer; class-vs-record rules under Code Style).

3. **Two DbContexts**: `InventoriesCommandsDbContext` (writes, includes outbox tables `OutboxState`/`OutboxMessage`/`InboxState`) vs `InventoriesQueriesDbContext` (reads, plain `DbContext`, `AsNoTracking`).

4. **Repository pattern**: `CommandsRepository<TAggregateRoot, TEntityId, TDomainEvent>` handles transactional save + domain-event publishing via MassTransit Outbox, with rollback on `DbUpdateConcurrencyException` / `DbUpdateException`.

5. **Dependency direction**: entry points depend inward (`Infra.In → Application → DomainModel`); the domain never references an `Out` infra project. Verifiable from `.csproj` references.

6. **Business rules live in the domain**: aggregates, factories and value objects throw; controllers and application services don't. `BusinessExceptionHandler` (`IExceptionHandler`) maps any `BusinessException` → HTTP 422 (`application/problem+json`); anything else falls through to a 500 — so only expected, client-facing failures are `BusinessException` subclasses.

7. **EF mapping lives outside the DbContext**: one `IEntityTypeConfiguration` class per aggregate under `DbConfigurations/` (e.g. `PlantConfiguration.cs`) — never map inline in `OnModelCreating`.

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

### Folder/File Naming

- **`_` prefix = shared/cross-cutting, not domain** — `_Framework.*` projects, `_Common/` folders.
- **Layer-suffixed type names** — the suffix marks the layer/role: `*DomainCommand`, `*QueryModel`, `*DbContext`, `*Repository`, `*Configuration`, `*Consumer`, `*Factory`, `*EventData`, `*Message`.

## Code Style

- **DTO types follow the layer**: HTTP command DTOs are **classes** with `[Required]`/data-annotation validation; application and domain commands are **records**. Keep validation attributes at the HTTP boundary only — application/domain records stay free of annotations.
- **Primary-constructor DI** — e.g. `PlantController(IPlantApplicationService svc) : ControllerBase`.

### REST API (controllers)

- **`[ApiVersion(1.0)]`** with route template `/inventories/v{version:apiVersion}/[controller]`.

### Persistence (EF Core)

- **One `IEntityTypeConfiguration` class per aggregate**, under `DbConfigurations/` (e.g. `PlantConfiguration.cs`) — never map inline in `OnModelCreating`.
- **Map strongly-typed IDs & value objects via `HasConversion`** (VO ↔ primitive), e.g. `HasConversion(id => id.Value, val => PlantId.Create(val))`.

### Exceptions

- **`BusinessException` is abstract — never throw it directly.** Throw `InvalidDataException` for invalid input/data, or define a domain-specific subclass of `BusinessException` for a violated business rule.
- **Throw from the domain** (aggregate, factory, value objects), not from controllers or the application service.
- **Pass both messages**: `base(logErrorMessage, userErrorMessage)` — the log message is internal/detailed; the user message is client-safe.
- **Guard clauses use `DataAssertion.IsTrue(condition, "user message")`** — throws `InvalidDataException` when the condition is false.
- **`BusinessExceptionHandler` (`IExceptionHandler`) maps any `BusinessException` → HTTP 422** (`application/problem+json`). Anything else falls through to a 500 — so only throw `BusinessException` subclasses for expected, client-facing failures.

## Repository Etiquette

- Branch off **`develop`** for day-to-day work; **`master`** is the stable/release branch.

## Infrastructure

Config keys, defaults, and runtime prerequisites: see [docs/infrastructure.md](docs/infrastructure.md).

## Notes

- **EF Core migrations** — see @docs/ef-migrations.md; migrations auto-apply at startup, so don't run `database update` manually.

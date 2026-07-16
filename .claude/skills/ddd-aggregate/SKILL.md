---
name: ddd-aggregate
description: >-
  Scaffold the DomainModel layer of a new DDD aggregate in the SmartInventory
  inventory microservice, following this codebase's exact conventions (folder
  layout, base classes, static factory, value objects, domain commands, domain
  events, repository interface). Use this whenever someone adds a new aggregate
  or domain concept to the Inventories bounded context — e.g. "add a Warehouse
  aggregate", "create a domain model for SupplierOrder", "I need a new entity
  for StockItem", "add a value object for SKU", or "scaffold the domain side of
  X" — even if they don't say the words "DDD" or "aggregate". It mirrors the
  existing Plant aggregate so new code is consistent with the rest of the model.
---

# DDD Aggregate (DomainModel layer)

This skill scaffolds the **DomainModel** layer of a new aggregate in the
SmartInventory inventory microservice. The reference implementation is the
**Plant** aggregate — when in doubt, open the real Plant files and mirror them
exactly. The goal is consistency: a new aggregate should be indistinguishable in
style from Plant.

## Scope

This skill stops at the domain boundary. It produces the files under
`SmartInventory.Inventories.DomainModel/{Name}Aggregate/` plus the one event
**message** type that the aggregate references (which lives in the `Messages`
project — see "The message boundary" below). It does **not** scaffold the
application service, controller, repository implementation, or EF
configuration — those are separate layers the user wires up afterward.

If the user clearly wants the full vertical slice (controller → service →
repository → EF config), say so and offer to continue past the domain layer by
mirroring the corresponding Plant files in those projects — but lead with the
domain layer, which is what this skill guarantees.

## The dependency rule that governs everything

The domain never references an `Out` infra project, and never references the
application or REST layers. It depends only inward:

```
SmartInventory.Inventories.DomainModel
  → SmartInventory._Framework.DomainModel   (base classes)
  → SmartInventory.Messages.Inventories      (event message contracts)
```

If you find yourself wanting to import EF Core, MassTransit, a DbContext, or an
application command into a domain file, stop — that dependency belongs in
another layer. Domain code is pure C# + the framework base classes.

## Aggregate folder layout

Every aggregate mirrors this exact layout. Replace `{Name}` with the aggregate
name (PascalCase, singular — `Warehouse`, `SupplierOrder`, `StockItem`):

```
SmartInventory.Inventories.DomainModel/
└── {Name}Aggregate/
    ├── {Name}.cs                              ← aggregate root
    ├── {Name}Factory.cs                       ← static factory
    ├── I{Name}Repository.cs                   ← domain repository interface
    ├── ValueObjects/
    │   ├── {Name}Id.cs                         ← strongly-typed id (always)
    │   └── {OtherVO}.cs                        ← one file per value object
    ├── DomainCommands/
    │   ├── Create{Name}DomainCommand.cs
    │   └── Update{Name}DomainCommand.cs
    └── DomainEvents/
        └── {Name}EventData.cs
```

Plus, in a different project, the event message the root publishes:

```
SmartInventory.Messages.Inventories/
└── {Name}DomainEventMessage.cs
```

Folder names are **plural** (`ValueObjects/`, `DomainCommands/`,
`DomainEvents/`), type names inside are **singular**, **one type per file**, and
the **namespace mirrors the folder path** with file-scoped syntax
(`namespace X;`).

## Build order

Create the pieces leaf-first so each file's dependencies already exist:

1. `{Name}Id` and any other value objects
2. `{Name}DomainEventMessage` (in the Messages project)
3. The domain commands
4. `{Name}EventData`
5. The aggregate root `{Name}`
6. `{Name}Factory`
7. `I{Name}Repository`

Then build the solution (`dotnet build SmartInventory.sln`) to confirm the
domain layer compiles before handing off to the other layers.

---

## Templates

These are distilled from the real Plant aggregate. Read them as patterns, not
fill-in-the-blank forms — adapt the value objects, command fields, and event
payload to the actual domain concept the user described.

### Strongly-typed id — `ValueObjects/{Name}Id.cs`

Every aggregate has an id that extends `EntityId` (which extends `ValueObject`
and already rejects `Guid.Empty`). Private constructor + static `Create`.

```csharp
using SmartInventory._Framework.DomainModel.Entities;

namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate.ValueObjects;

public class {Name}Id : EntityId
{
    private {Name}Id(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new instance of {Name}Id.
    /// </summary>
    public static {Name}Id Create(Guid value)
    {
        return new {Name}Id(value);
    }
}
```

### Other value objects — `ValueObjects/{VO}.cs`

Domain attributes that have rules (a name, a code, a quantity) are value
objects, not raw `string`/`int`. They extend `ValueObject`, are immutable,
implement `GetEqualityComponents`, validate in `Create`, and have a private
constructor.

Validation belongs here, in the value object — this is where invalid data is
rejected at the edge of the model. Note that the existing `PlantName` throws
`ArgumentException`; prefer the codebase's own exception types instead:
`DataAssertion.IsTrue(condition, "client-safe message")` for input validation
(it throws `InvalidDataException`, which the global handler maps to HTTP 422).
Plain `ArgumentException` would fall through to a 500.

```csharp
using SmartInventory._Framework.DomainModel.ValueObjects;
using SmartInventory._Framework.Util.Assertions;

namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate.ValueObjects;

public class {VO} : ValueObject
{
    public string Value { get; }

    private {VO}(string value)
    {
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static {VO} Create(string value)
    {
        DataAssertion.IsTrue(!string.IsNullOrWhiteSpace(value), "{VO} cannot be empty.");

        return new {VO}(value);
    }
}
```

### Event message — `SmartInventory.Messages.Inventories/{Name}DomainEventMessage.cs`

This is the cross-service contract the aggregate publishes when it changes. It
is a plain DTO **class** (not a record, not a value object) and lives in the
`Messages` project so consumers in other bounded contexts can reference it
without depending on the domain model. Flatten the aggregate's published state
into primitives here.

```csharp
namespace SmartInventory.Messages.Inventories;

public class {Name}DomainEventMessage
{
    public Guid Id { get; set; }
    // one primitive property per piece of published state
    public string Name { get; set; } = null!;
}
```

### Domain commands — `DomainCommands/Create{Name}DomainCommand.cs`

Domain commands are **records** (immutable, no validation attributes —
validation lives in the value objects and aggregate). The create command
carries the id; the update command does not (the id identifies the existing
aggregate being updated).

```csharp
namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate.DomainCommands;

public record Create{Name}DomainCommand(Guid {Name}Id, string Name);
```

```csharp
namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate.DomainCommands;

public record Update{Name}DomainCommand(string Name);
```

### Event data — `DomainEvents/{Name}EventData.cs`

A record capturing the event payload shape, per the aggregate-folder
convention.

```csharp
namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate.DomainEvents;

public record {Name}EventData(Guid Id, string Name);
```

### Aggregate root — `{Name}.cs`

The root extends `AggregateRoot<{Name}Id>` (single type parameter — there is no
`InventoryAggregateRoot` despite what older docs say) and implements
`IPublishDomainEvents<{Name}DomainEventMessage>`, which forces it to expose
`GetEventPayload()` — the snapshot the repository publishes via the outbox.

Key idioms:
- Primary constructor takes the id and passes it to the base.
- Mutable state has `private set` and is assigned only through `Create` /
  `Update`, which take **domain** commands and rebuild value objects via their
  `Create` factories. Business rules live here and in the value objects, never
  in the application service.
- `GetEventPayload()` flattens current state into the message DTO.

```csharp
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Events;
using SmartInventory.Inventories.DomainModel.{Name}Aggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.{Name}Aggregate.ValueObjects;
using SmartInventory.Messages.Inventories;

namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate;

public class {Name}({Name}Id id) : AggregateRoot<{Name}Id>(id), IPublishDomainEvents<{Name}DomainEventMessage>
{
    public {Name}Name Name { get; private set; } = null!;

    public void Create(Create{Name}DomainCommand command)
    {
        Name = {Name}Name.Create(command.Name);
    }

    public void Update(Update{Name}DomainCommand command)
    {
        Name = {Name}Name.Create(command.Name);
    }

    public {Name}DomainEventMessage GetEventPayload()
    {
        return new {Name}DomainEventMessage
        {
            Id = Id.Value,
            Name = Name.Value
        };
    }
}
```

### Static factory — `{Name}Factory.cs`

Aggregates are **never** constructed with `new` outside the factory. The factory
builds the id from the command, constructs the root, and runs `Create` so all
creation rules sit in one place.

```csharp
using SmartInventory.Inventories.DomainModel.{Name}Aggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.{Name}Aggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate;

public static class {Name}Factory
{
    public static {Name} Create(Create{Name}DomainCommand command)
    {
        var {nameCamel}Id = {Name}Id.Create(command.{Name}Id);
        var new{Name} = new {Name}({nameCamel}Id);

        new{Name}.Create(command);

        return new{Name};
    }
}
```

### Repository interface — `I{Name}Repository.cs`

The domain declares *what* persistence it needs; the implementation lives in the
infra layer (out of scope here). Methods take/return the aggregate and its
strongly-typed id — never raw `Guid`. Mirror Plant's three methods unless the
user needs more.

```csharp
using SmartInventory.Inventories.DomainModel.{Name}Aggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.{Name}Aggregate;

public interface I{Name}Repository
{
    Task<{Name}?> GetByIdAsync({Name}Id id);
    Task CreateAsync({Name} {nameCamel});
    Task UpdateAsync({Name} {nameCamel});
}
```

---

## Conventions checklist

Before declaring the domain layer done, verify:

- [ ] Folder is `{Name}Aggregate/` with plural sub-folders `ValueObjects/`,
      `DomainCommands/`, `DomainEvents/`.
- [ ] One type per file; file name matches the type; file-scoped namespaces
      mirroring the folder path.
- [ ] Aggregate extends `AggregateRoot<{Name}Id>` and implements
      `IPublishDomainEvents<{Name}DomainEventMessage>` with `GetEventPayload()`.
- [ ] Id extends `EntityId`; other value objects extend `ValueObject`; both use
      private constructor + static `Create`.
- [ ] No raw `Guid`/`string` crossing the aggregate's public surface where a
      value object exists.
- [ ] Domain commands are **records**; the event message is a **class** in
      `SmartInventory.Messages.Inventories`.
- [ ] Aggregate built only through the static `{Name}Factory`, never `new`
      outside it.
- [ ] No EF Core / MassTransit / application / infra imports in any domain file.
- [ ] Validation lives in value objects / the aggregate, using
      `DataAssertion.IsTrue(...)` or a `BusinessException` subclass — never
      thrown from controllers or services. `BusinessException` is abstract:
      throw `InvalidDataException` or a domain-specific subclass, not the base.
- [ ] `dotnet build SmartInventory.sln` succeeds.

## Worked reference

For the complete, verbatim Plant aggregate as a side-by-side comparison, see
[references/plant-example.md](references/plant-example.md). When a template
feels ambiguous, that file is ground truth.

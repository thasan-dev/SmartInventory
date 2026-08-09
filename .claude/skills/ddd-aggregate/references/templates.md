# Aggregate file templates

Read them as patterns, not fill-in-the-blank forms — adapt the value objects,
command fields, and event payload to the actual domain concept the user
described. For a complete worked aggregate see
[plant-example.md](plant-example.md).

Replace `{Context}` with the bounded context (e.g. `Inventories`), `{Name}` with
the aggregate name (PascalCase, singular), `{nameCamel}` with its camelCase
form, and `{VO}` with a value object name. Note the namespace order differs
between the two projects: `SmartInventory.{Context}.DomainModel` but
`SmartInventory.Messages.{Context}`.

## Contents

- [Strongly-typed id](#strongly-typed-id--valueobjectsnameidcs)
- [Other value objects](#other-value-objects--valueobjectsvocs)
- [Event message](#event-message--namedomaineventmessagecs)
- [Domain commands](#domain-commands--domaincommands)
- [Event data](#event-data--domaineventsnameeventdatacs)
- [Aggregate root](#aggregate-root--namecs)
- [Static factory](#static-factory--namefactorycs)
- [Repository interface](#repository-interface--inamerepositorycs)

---

## Strongly-typed id — `ValueObjects/{Name}Id.cs`

Every aggregate has an id extending `EntityId` (which extends `ValueObject` and
already rejects `Guid.Empty`, so you don't re-validate that here). Private
constructor + static `Create`.

```csharp
using SmartInventory._Framework.DomainModel.Entities;

namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate.ValueObjects;

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

## Other value objects — `ValueObjects/{VO}.cs`

Domain attributes that carry rules (a name, a code, a quantity) are value
objects, not raw `string`/`int` — that is what keeps the rule in one place
instead of scattered across services.

Validation belongs here, at the edge of the model. Use
`DataAssertion.IsTrue(condition, "client-safe message")`, which throws
`InvalidDataException` — the global handler maps that to HTTP 422. A plain
`ArgumentException` would fall through as a 500 and leak an internal message.
Some older value objects in the repo still throw `ArgumentException`; don't copy
that.

```csharp
using SmartInventory._Framework.DomainModel.ValueObjects;
using SmartInventory._Framework.Util.Assertions;

namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate.ValueObjects;

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

`GetEqualityComponents` is what makes equality structural — yield every field
that participates in identity, or two equal values will compare unequal.

## Event message — `{Name}DomainEventMessage.cs`

Lives in `SmartInventory.Messages.{Context}/`, not the domain project. This is
the cross-service contract the aggregate publishes when it changes, and it sits
in `Messages` so consumers in other bounded contexts can reference it without
taking a dependency on the domain model. It is a plain DTO **class** — not a
record, not a value object — with the aggregate's published state flattened into
primitives.

```csharp
namespace SmartInventory.Messages.{Context};

public class {Name}DomainEventMessage
{
    public Guid Id { get; set; }
    // one primitive property per piece of published state
    public string Name { get; set; } = null!;
}
```

## Domain commands — `DomainCommands/`

Domain commands are **records** — immutable, with no validation attributes,
because validation lives in the value objects and the aggregate. The create
command carries the id; the update command does not, since the id already
identifies the aggregate being updated.

```csharp
namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate.DomainCommands;

public record Create{Name}DomainCommand(Guid {Name}Id, string Name);
```

```csharp
namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate.DomainCommands;

public record Update{Name}DomainCommand(string Name);
```

## Event data — `DomainEvents/{Name}EventData.cs`

A record capturing the event payload shape, per the aggregate-folder convention.

```csharp
namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate.DomainEvents;

public record {Name}EventData(Guid Id, string Name);
```

## Aggregate root — `{Name}.cs`

The root extends `AggregateRoot<{Name}Id>` — a single type parameter, and the
same base class in every bounded context; there is no per-context aggregate base
despite what older docs suggest. It implements
`IPublishDomainEvents<{Name}DomainEventMessage>`, which forces it to expose
`GetEventPayload()` — the snapshot the repository publishes through the outbox.

Idioms worth preserving:

- Primary constructor takes the id and passes it to the base.
- Mutable state has `private set` and changes only through `Create` / `Update`,
  which take **domain** commands and rebuild value objects via their `Create`
  factories. Keeping the setters private is what stops an application service
  from mutating the aggregate behind its own rules.
- `GetEventPayload()` flattens current state into the message DTO.

```csharp
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Events;
using SmartInventory.{Context}.DomainModel.{Name}Aggregate.DomainCommands;
using SmartInventory.{Context}.DomainModel.{Name}Aggregate.ValueObjects;
using SmartInventory.Messages.{Context};

namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate;

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

If the aggregate owns child entities, expose them as a read-only collection and
mutate them only through methods on the root — reaching into a child from the
outside is how invariants that span the aggregate quietly break.

## Static factory — `{Name}Factory.cs`

Aggregates are constructed only here, never with `new` elsewhere. The factory
builds the id from the command, constructs the root, and runs `Create`, so every
creation rule sits in one place rather than being duplicated at each call site.

```csharp
using SmartInventory.{Context}.DomainModel.{Name}Aggregate.DomainCommands;
using SmartInventory.{Context}.DomainModel.{Name}Aggregate.ValueObjects;

namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate;

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

## Repository interface — `I{Name}Repository.cs`

The domain declares *what* persistence it needs; the implementation lives in the
infra layer, out of scope for this skill. Methods take and return the aggregate
and its strongly-typed id, never a raw `Guid` — that is what keeps persistence
from leaking primitives back into the model. These three methods are the default
surface — add more only when the user needs them.

```csharp
using SmartInventory.{Context}.DomainModel.{Name}Aggregate.ValueObjects;

namespace SmartInventory.{Context}.DomainModel.{Name}Aggregate;

public interface I{Name}Repository
{
    Task<{Name}?> GetByIdAsync({Name}Id id);
    Task CreateAsync({Name} {nameCamel});
    Task UpdateAsync({Name} {nameCamel});
}
```

# Plant aggregate — verbatim reference

The complete DomainModel layer of the **Plant** aggregate, exactly as it exists
in the codebase. This is the ground truth the SKILL.md templates are distilled
from. When a template feels ambiguous, copy the style here.

> Note: `PlantName.Create` currently throws `ArgumentException`. That is the
> existing code, but it is the one spot the SKILL.md deliberately improves on —
> new value objects should validate with `DataAssertion.IsTrue(...)` so failures
> map to HTTP 422 instead of 500. Everything else here is the pattern to follow.

## Base classes (framework — read-only, do not modify)

`AggregateRoot<TId>` takes a single type parameter; the published-event contract
is supplied by implementing `IPublishDomainEvents<TPayload>` on the root, not by
a second generic parameter.

```csharp
// SmartInventory._Framework.DomainModel/Aggregates/AggregateRoot.cs
public abstract class AggregateRoot<TId> : Entity<TId> where TId : EntityId
{
    protected AggregateRoot() { }              // EF
    protected AggregateRoot(TId id) : base(id) { }
}

// SmartInventory._Framework.DomainModel/Entities/EntityId.cs
public abstract class EntityId : ValueObject
{
    public Guid Value { get; }
    protected EntityId(Guid value)
    {
        if (value == Guid.Empty)
            throw new InvalidDataException($"{typeof(EntityId)}: Value cannot be an empty Guid.");
        Value = value;
    }
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
}

// SmartInventory._Framework.DomainModel/Events/IPublishDomainEvents.cs
public interface IPublishDomainEvents<TPayload> where TPayload : class
{
    TPayload GetEventPayload();
}
```

## Plant.cs

```csharp
using SmartInventory._Framework.DomainModel.Aggregates;
using SmartInventory._Framework.DomainModel.Events;
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;
using SmartInventory.Messages.Inventories;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public class Plant(PlantId id) : AggregateRoot<PlantId>(id), IPublishDomainEvents<PlantDomainEventMessage>
{
    public PlantName Name { get; private set; } = null!;

    public void Create(CreatePlantDomainCommand command)
    {
        Name = PlantName.Create(command.Name);
    }

    public void Update(UpdatePlantDomainCommand command)
    {
        Name = PlantName.Create(command.Name);
    }

    public PlantDomainEventMessage GetEventPayload()
    {
        return new PlantDomainEventMessage
        {
            Id = Id.Value,
            Name = Name.Value
        };
    }
}
```

## PlantFactory.cs

```csharp
using SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public static class PlantFactory
{
    public static Plant Create(CreatePlantDomainCommand command)
    {
        var plantId = PlantId.Create(command.PlantId);
        var newPlant = new Plant(plantId);

        newPlant.Create(command);

        return newPlant;
    }
}
```

## IPlantRepository.cs

```csharp
using SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate;

public interface IPlantRepository
{
    Task<Plant?> GetByIdAsync(PlantId id);
    Task CreateAsync(Plant plant);
    Task UpdateAsync(Plant plant);
}
```

## ValueObjects/PlantId.cs

```csharp
using SmartInventory._Framework.DomainModel.Entities;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

public class PlantId : EntityId
{
    private PlantId(Guid value) : base(value)
    {
    }

    /// <summary>
    /// Creates a new instance of PlantId.
    /// </summary>
    public static PlantId Create(Guid value)
    {
        return new PlantId(value);
    }
}
```

## ValueObjects/PlantName.cs

```csharp
using SmartInventory._Framework.DomainModel.ValueObjects;

namespace SmartInventory.Inventories.DomainModel.PlantAggregate.ValueObjects;

public class PlantName : ValueObject
{
    public string Value { get; }

    private PlantName(string value)
    {
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static PlantName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Plant name cannot be null or empty.", nameof(value));

        return new PlantName(value);
    }
}
```

## DomainCommands/CreatePlantDomainCommand.cs

```csharp
namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;

public record CreatePlantDomainCommand(Guid PlantId, string Name);
```

## DomainCommands/UpdatePlantDomainCommand.cs

```csharp
namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainCommands;

public record UpdatePlantDomainCommand(string Name);
```

## DomainEvents/PlantEventData.cs

```csharp
namespace SmartInventory.Inventories.DomainModel.PlantAggregate.DomainEvents;

public record PlantEventData(Guid Id, string Name);
```

## SmartInventory.Messages.Inventories/PlantDomainEventMessage.cs

```csharp
namespace SmartInventory.Messages.Inventories;

public class PlantDomainEventMessage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}
```

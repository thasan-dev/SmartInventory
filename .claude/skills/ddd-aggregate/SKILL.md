---
name: ddd-aggregate
description: >-
  Scaffolds the DomainModel layer of a new DDD aggregate in the SmartInventory
  microservices — aggregate root, static factory, value objects, domain
  commands, domain events, and repository interface — mirroring the existing
  codebase's conventions so new code matches the rest of the model. Use this whenever
  someone adds a new aggregate, entity, value object, or domain concept, e.g.
  "add a Warehouse aggregate", "create a domain model for SupplierOrder", "I
  need a new entity for StockItem", "add a value object for SKU", or "scaffold
  the domain side of X" — even if they never say the words "DDD" or "aggregate".
---

# DDD Aggregate (DomainModel layer)

Scaffold the **DomainModel** layer of a new aggregate in any SmartInventory
bounded context. Everything needed is bundled in this skill: the templates in
[references/templates.md](references/templates.md) and the worked example in
[references/plant-example.md](references/plant-example.md) are the source of
truth. Work from those rather than reading an existing aggregate out of the
repo — repo code drifts, gets renamed, and may carry patterns that predate these
conventions. The goal is consistency: a new aggregate should be
indistinguishable in style from the worked example.

## Principles

- **Ubiquitous Language** — name types, properties, and tables with the exact business terms.
- **Aggregates** — business logic lives in the Aggregate Root; never modify child entities from outside it.
- **Value Objects** — immutable, structural equality (not identity).
- **Domain Events** — raise locally in the entity; dispatch integration events from the Application layer / Outbox, never from the domain.

## Placeholders

Every path and namespace below is parameterized. Substitute:

- `{Context}` — the bounded context / microservice, e.g. `Inventories`
- `{Name}` — the aggregate name, PascalCase singular, e.g. `Warehouse`

## Scope

This skill stops at the domain boundary. It produces the files under
`SmartInventory.{Context}.DomainModel/{Name}Aggregate/` plus the one event
**message** type the aggregate references, which lives in the `Messages` project
so other bounded contexts can consume it without depending on the domain model.

It does **not** scaffold the application service, controller, repository
implementation, or EF configuration — those are separate layers, wired up
afterward.

If the user wants the full vertical slice (controller → service → repository →
EF config), lead with the domain layer — that is the part this skill guarantees
— then offer to continue into those projects, where you will need to follow
their existing conventions since this skill does not cover them.

## Build order

Create the pieces leaf-first so each file's dependencies already exist. The code
template for every step is in
[references/templates.md](references/templates.md) — read that file before
writing the first one.

1. `ValueObjects/{Name}Id.cs`, then one file per other value object
2. `{Name}DomainEventMessage.cs` — in `SmartInventory.Messages.{Context}`
3. `DomainCommands/Create{Name}DomainCommand.cs` and `Update{Name}DomainCommand.cs`
4. `DomainEvents/{Name}EventData.cs`
5. `{Name}.cs` — the aggregate root
6. `{Name}Factory.cs`
7. `I{Name}Repository.cs`

Then run `dotnet build SmartInventory.sln` to confirm the domain layer compiles
before handing off to the other layers.

## Aggregate folder layout

The finished shape, for checking your work:

```
SmartInventory.{Context}.DomainModel/
└── {Name}Aggregate/
    ├── {Name}.cs                              ← aggregate root
    ├── {Name}Factory.cs                       ← static factory
    ├── I{Name}Repository.cs                   ← domain repository interface
    ├── ValueObjects/
    │   ├── {Name}Id.cs                        ← strongly-typed id (always)
    │   └── {OtherVO}.cs                       ← one file per value object
    ├── DomainCommands/
    │   ├── Create{Name}DomainCommand.cs
    │   └── Update{Name}DomainCommand.cs
    └── DomainEvents/
        └── {Name}EventData.cs

SmartInventory.Messages.{Context}/
└── {Name}DomainEventMessage.cs                ← published contract
```

Note the two project names put `{Context}` in different positions —
`SmartInventory.{Context}.DomainModel` but `SmartInventory.Messages.{Context}`.

Folder names are **plural**, type names inside are **singular**, **one type per
file**, and the **namespace mirrors the folder path** with file-scoped syntax
(`namespace X;`).

## The dependency rule that governs everything

The domain never references an `Out` infra project, and never references the
application or REST layers. It depends only inward:

```
SmartInventory.{Context}.DomainModel
  → SmartInventory._Framework.DomainModel   (base classes)
  → SmartInventory.Messages.{Context}       (event message contracts)
```

The `_Framework.*` projects are shared across every bounded context; the domain
never references another context's projects directly — cross-context
communication goes through published messages.

Wanting to import EF Core, MassTransit, a DbContext, or an application command
into a domain file is the signal that the logic belongs in another layer, not
that the reference is missing. Domain code is pure C# plus the framework base
classes.

## Conventions checklist

Verify before declaring the domain layer done:

- [ ] Folder is `{Name}Aggregate/` with plural sub-folders `ValueObjects/`,
      `DomainCommands/`, `DomainEvents/`.
- [ ] One type per file; file name matches the type; file-scoped namespaces
      mirroring the folder path.
- [ ] Aggregate extends `AggregateRoot<{Name}Id>` and implements
      `IPublishDomainEvents<{Name}DomainEventMessage>` with `GetEventPayload()`.
- [ ] Id extends `EntityId`; other value objects extend `ValueObject`; both use
      a private constructor + static `Create`, and implement
      `GetEqualityComponents` so equality is structural.
- [ ] No raw `Guid`/`string` crossing the aggregate's public surface where a
      value object exists.
- [ ] Domain commands are **records**; the event message is a **class** in
      `SmartInventory.Messages.{Context}` — not in the domain project.
- [ ] Every namespace carries the right `{Context}`, and no file references
      another bounded context's projects.
- [ ] Aggregate built only through the static `{Name}Factory`, never `new`
      outside it.
- [ ] No EF Core / MassTransit / application / infra imports in any domain file.
- [ ] Validation lives in value objects and the aggregate, via
      `DataAssertion.IsTrue(...)` or a `BusinessException` subclass — never
      thrown from controllers or services. `BusinessException` is abstract, so
      throw `InvalidDataException` or a domain-specific subclass, not the base.
- [ ] `dotnet build SmartInventory.sln` succeeds.

## References

- [references/templates.md](references/templates.md) — code template for each
  file, with the reasoning behind each idiom. Read this while scaffolding.
- [references/plant-example.md](references/plant-example.md) — a complete worked
  aggregate, captured verbatim. Ground truth when a template feels ambiguous.

These two files are self-contained. If something a task needs genuinely isn't
covered here, say so and propose adding it to the reference, rather than
inferring it from repo code — that keeps the skill authoritative instead of
letting it drift with whatever happens to be committed.

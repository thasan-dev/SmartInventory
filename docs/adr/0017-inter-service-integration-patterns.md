# Inter-service integration: async-events-only, outbox/inbox, lightweight CQRS, choreography-first, thin shared contracts

Cross-cutting integration rules for the physical decomposition ([ADR-0014](./0014-physical-microservice-decomposition.md)).
These apply to every service.

## 1. Service-to-service communication is async-events-only

Services **do not call each other synchronously on the critical path.** They communicate by publishing
and consuming events over the bus (MassTransit + **Amazon SNS + SQS** — see
[ADR-0022](./0022-message-broker-sns-sqs-via-masstransit.md), which supersedes the earlier "RabbitMQ /
Azure Service Bus" naming). Each service keeps a **local read-model** of any cross-service data it needs,
kept current by the other's events.

Rationale: sync calls create runtime/availability coupling (Catalog down → Sales can't sell) and
cascading failure; events + local replicas keep each service independently available. A rare,
read-only, back-office "live lookup" may use a sync call as an explicit exception — **never** on the
storefront checkout path.

## 2. Cross-service data via minimal local replicas

The one **required** replica is **Sales' pricing replica**: `{skuId, price, catalogueDiscount,
active}`, fed by Catalog events (`price changed`, `discount changed`, `SKU activated/retired`). Sales
reads *its own copy* at placement to snapshot the line price (ADR-0007) atomically with the Hold — no
sync call, no trusting client-supplied prices. **Catalog holds no stock replica** — storefront
availability is composed at the client edge ([ADR-0018](./0018-client-edge-aws-api-gateway-no-bff.md)),
and authoritative availability is only ever checked at placement (ADR-0007).

Consequence (accepted): price edits are **eventually consistent but self-consistent** — the storefront
and placement read the same replica, so a customer is charged what they saw even if it lags the master
by seconds. Missed/duplicated events are healed by inbox idempotency + a reconciliation sweep.

## 3. Outbox on every emitter, inbox on every consumer

The **transactional outbox** (MassTransit EF Outbox, `OutboxState`/`OutboxMessage`) is standard on
**every** event emitter: the state change and the event-to-publish commit in one DB transaction, so
there are **no lost or phantom events**. The **inbox** (`InboxState`) is standard on **every**
consumer: dedupe by message id / business key so at-least-once delivery + idempotent consumers =
effectively-once processing. Not optional — the replicas and reconciliations depend on it.

## 4. Lightweight CQRS everywhere; no event sourcing

The default is **lightweight CQRS** — separate read/write models over the **same database** (the
existing two-`DbContext` pattern). **Catalog additionally** maintains an **Elasticsearch search index**
as a specialized read projection for browse/search (a projection, not a second source of truth).
**No event sourcing** — services use state-based aggregates + the outbox; the stock **movement ledger**
(ADR-0016) is a *domain* ledger, not an event-sourced write model. Trivial services (Notification, much
of Accounts) keep the same framework shape without inventing separate read stores.

## 5. Choreography-first; no orchestrator in the MVP

Multi-step flows are **choreographed**: a service does its bit locally and emits an event; others
react. There is **no central orchestrator / saga state-machine** in the MVP, because the one hard
cross-service invariant (Order+Stock) was **co-located in Sales** (ADR-0014) precisely to avoid a
placement saga, and the one multi-step process — **shop onboarding** — is a **human-driven Shop Status
machine** (ADR-0013), not an automated saga. Cross-flow tracing uses **correlation ids** on events plus
the Order **Status History**. Orchestration is reserved for a future genuinely multi-service,
compensatable process — none exists in the MVP.

## 6. Thin shared contracts + ACL at consumers; additive-only versioning

Event/message contracts live in **thin, producer-owned, DTO-only packages** (the existing
`SmartInventory.Messages.*` pattern) — treated as a **versioned public API**, no domain logic inside.
Every consumer applies an **anti-corruption layer** (the existing `*DomainEventConsumer` pattern) to
translate a contract into its internal model, so internals never couple to another service's schema.
Evolution is **additive-only with tolerant readers** (add optional fields; never remove/rename/
repurpose), which lets producer and consumer deploy independently; a **breaking change is a new
message version** run in parallel, then the old is retired.

## Considered and rejected

- **Synchronous inter-service calls** (REST/gRPC) as a default — rejected: runtime coupling and
  cascading failure; replicas + events keep services independently available.
- **A schema registry (Avro/Protobuf/JSON-Schema)** — the standard for Kafka/polyglot shops; rejected
  here as overkill and against the MassTransit grain for a single-team .NET monorepo.
- **Share-nothing contract copies per service** — buys autonomy a single team doesn't need and loses
  MassTransit's type-safety/DX; the thin shared package + additive discipline gives independent deploys
  without it.
- **Orchestration / saga coordinator in the MVP** — rejected: no multi-service compensatable process
  exists (the hard invariant was co-located).
- **Event sourcing** — rejected: a tax not needed; state-based aggregates + domain ledgers suffice.

## Relates to

Builds on ADR-0014 (decomposition). The pricing replica realizes ADR-0015 / ADR-0007. Inbox idempotency
supports ADR-0016. The client edge (gateway, composition, no BFF) is
[ADR-0018](./0018-client-edge-aws-api-gateway-no-bff.md).

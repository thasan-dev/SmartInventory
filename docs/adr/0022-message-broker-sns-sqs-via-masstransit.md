# Message broker: Amazon SNS + SQS via MassTransit

The async integration of [ADR-0017](./0017-inter-service-integration-patterns.md) needs a concrete broker.
The stack docs said "RabbitMQ / Azure Service Bus," but the platform is **AWS**. Decision: the bus is
**Amazon SNS + SQS through MassTransit's native SQS/SNS transport**. This **supersedes the RabbitMQ / Azure
Service Bus** naming in ADR-0017 and the infrastructure docs.

## Why

AWS-native, consistent with every other edge/runtime choice (Cognito, API Gateway, EKS/ECS): **fully
managed, no broker to run/patch/scale**, cheap at MVP volume, scales without ops. MassTransit abstracts the
transport, so the publish/consume code and the **EF outbox on emit + inbox dedupe on consume** (ADR-0017 §3)
are unchanged in pattern — only the transport registration differs. The 256 KB SNS/SQS message limit is a
non-issue for the thin DTO contracts (ADR-0017 §6).

## Topology

MassTransit maps message types to an **SNS topic per published message type** and an **SQS queue per
consumer endpoint** (topic→queue subscriptions managed by MassTransit). This is *topic + subscription*
shaped rather than RabbitMQ *exchange* shaped, but the choreography of ADR-0017 is expressed the same way in
MassTransit.

## Consequences

- **Standard queues, not FIFO.** Ordering is *not* required because consumers are **idempotent** (inbox,
  ADR-0017) and are either **commutative** — the stock **movement ledger / `onHand` projection** (ADR-0016)
  applies signed deltas, and addition commutes, so out-of-order-but-exactly-once still sums correctly — or
  **version-aware** — the Sales **pricing replica** (ADR-0015/0017) applies last-writer-wins by event
  version/timestamp and a reconciliation sweep heals gaps. **SQS FIFO with `MessageGroupId` = the partition
  key** (e.g. `StockUnitId`) is held in reserve *only if* a genuine per-key ordering need ever appears.
- **At-least-once delivery** is the model (SNS→SQS) — exactly what the inbox is for; no change to ADR-0017.
- **Infra-doc reconciliation:** `backend/docs/infrastructure.md`, `backend/CLAUDE.md`, and ADR-0017 mention
  RabbitMQ / Azure Service Bus; those references are superseded by this ADR (tracked with the broader
  Azure→AWS / Entra→Cognito reconciliation pass).

## Considered and rejected

- **Amazon MQ for RabbitMQ** — lowest-effort reconciliation (docs already say RabbitMQ; MassTransit's
  RabbitMQ transport points straight at it), but it is a **brokered instance you run, patch, and scale** —
  reintroducing the ops that SNS+SQS avoids — for fanout-exchange semantics we don't need.
- **Kafka / Amazon MSK** — already rejected in ADR-0017 (Kafka/schema-registry world is overkill for a
  single-team .NET monorepo).

## Relates to

Realizes the transport for [ADR-0017](./0017-inter-service-integration-patterns.md); the outbox/inbox,
pricing replica, and stock-movement consumers ride on it (ADR-0015/0016).
</content>

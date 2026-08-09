# Contract testing: schema-snapshot + tolerant-reader, no Pact broker

ADR-0017 §6 set the contract *policy* — thin producer-owned `SmartInventory.Messages.*` packages, ACL at
consumers, **additive-only** evolution with tolerant readers. This fixes how that policy is **enforced in
CI** across the SNS/SQS bus ([ADR-0022](./0022-message-broker-sns-sqs-via-masstransit.md)), so a producer
cannot silently break a consumer.

## Decision

Two lightweight CI checks, no Pact and no broker:

1. **Schema-snapshot / approval test** on each message contract in `SmartInventory.Messages.*`: the message
   shape is snapshotted, and CI **fails on any non-additive change** — a field removed, renamed, or retyped.
   This mechanically enforces "additive-only" (new optional fields pass; breaking changes require a new
   message version, per ADR-0017 §6).
2. **Tolerant-reader tests per consumer**: each consumer proves it still deserializes the current message
   **and** an additively-evolved variant (extra unknown fields ignored), so producers can add fields and
   deploy independently.

## Why not Pact

The contracts live in **one shared, typed MassTransit package** consumed by a **single team**, so a removed
field already **fails to compile** at the consumer — most of what a consumer-driven-contract broker buys is
free. Pact's real value is coordinating **independently-versioned services owned by different teams** that
don't share code; that situation doesn't exist here. The snapshot test (guards additive-only) + tolerant
reader (proves forward-compatibility) close the residual **semantic / discipline-slip** gap at a fraction of
the ceremony.

## Considered and rejected

- **Pact + a Pact broker** — cross-team CDC machinery; overkill for a single-team shared-package monorepo.
- **Integration tests only** — misses additive-breaking changes until runtime, in the one consumer the
  producer's author never touched.

## Relates to

Enforces the contract policy of [ADR-0017](./0017-inter-service-integration-patterns.md) §6 over the
transport of [ADR-0022](./0022-message-broker-sns-sqs-via-masstransit.md). **Complements — does not
replace — the per-service integration-test seam** (revising SI-32's single-context
`WebApplicationFactory`-per-aggregate approach into per-service vertical-slice tests), which remains open.
</content>

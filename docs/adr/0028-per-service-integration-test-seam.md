# Testing seam: per-service vertical-slice integration tests + real Postgres

SI-32's testing plan assumed a **single context** — "one `WebApplicationFactory` per aggregate" in one
`Inventories` host. Under the decomposition ([ADR-0014](./0014-physical-microservice-decomposition.md)) that
no longer fits. Decision: **per-service** integration tests, plus domain unit tests, plus the inter-service
**contract tests** of [ADR-0024](./0024-contract-testing-snapshot-tolerant-reader.md).

## The three seams

1. **Per-service HTTP integration (primary)** — one `WebApplicationFactory`-based vertical slice **per
   service** (Sales, Catalog, Logistics, Accounts, Notification): command endpoint → application → domain →
   repository → DB, reads verified via query endpoints and the outbox/consumer. Runs against a **real
   PostgreSQL via Testcontainers** (not in-memory) so **EF mappings, `HasConversion` VO conversions, the
   outbox, and — critically — RLS policies** behave as in production.
2. **Domain unit tests (pure)** — decision-rich logic where HTTP routing would be indirect: order
   state-machine legality, `available` / Hold arithmetic, discount composition, per-order profit.
3. **Contract tests** — schema-snapshot + tolerant-reader across the SNS/SQS message contracts
   ([ADR-0024](./0024-contract-testing-snapshot-tolerant-reader.md)), replacing the cross-aggregate coverage
   that a single host used to give.

## RLS must be exercised

Because tenant isolation is now **DB-enforced** ([ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md)),
integration tests **assert it**: a request in shop A's context cannot read/write shop B's rows, and a
**missing `app.shop_id` yields zero rows** (fail-closed). This is new, load-bearing coverage a single-context
host never needed.

## Considered and rejected

- **SI-32's single-context `WebApplicationFactory`-per-aggregate** — superseded: it assumes one host owning
  all aggregates; there are now N services.
- **In-memory EF provider** — rejected: hides RLS, `HasConversion`, and outbox behavior — exactly the
  production-critical parts.

## Relates to

Supersedes SI-32's Testing Decisions; pairs with the contract tests of
[ADR-0024](./0024-contract-testing-snapshot-tolerant-reader.md); the RLS assertions verify
[ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md); runs against the per-service RDS Postgres of
[ADR-0027](./0027-database-per-service-rds-postgres.md).
</content>

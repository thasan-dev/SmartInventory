# Database-per-service on Amazon RDS for PostgreSQL

The physical decomposition ([ADR-0014](./0014-physical-microservice-decomposition.md)) needs a data-hosting
rule. Decision: **each service owns its own database — Amazon RDS for PostgreSQL — with no shared database
across services.** Within a service's database, multi-tenancy is **RLS** (shared tables + `shop_id`,
[ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md)).

## Decision

- **Database-per-service.** Sales, Catalog, Logistics, Accounts, and Notification each own a **separate
  database**; **only events cross the boundary** ([ADR-0017](./0017-inter-service-integration-patterns.md)),
  never a shared table or cross-service join. This is what makes independent schema, migration, and scaling
  real.
- **Engine = PostgreSQL on Amazon RDS.** Postgres is required by the RLS tenancy model (ADR-0019); RDS is
  the managed, AWS-native host (consistent with Cognito / API Gateway / SNS-SQS / EKS-ECS).
- **Two axes, not to be confused:** *services* are isolated by **separate databases**; *tenants* within a
  service are isolated by **RLS rows** in one shared schema. One DB holds many shops; one platform holds
  many service DBs.

## Considered and rejected

- **A single shared database across services** — rejected: couples services at the schema, enables sneaky
  cross-service joins, and destroys the independent-deploy/scale property the decomposition exists for.
- **Database-per-shop** — already rejected in ADR-0019 (RLS instead); this ADR is the orthogonal
  *service* axis.
- **Aurora PostgreSQL** — a fine future scale option (same engine); plain RDS Postgres is sufficient for the
  MVP and cheaper. Swappable without model change.
- **A non-relational store (DynamoDB)** — rejected: the domain is relational and the tenancy model depends
  on Postgres RLS.

## Relates to

The service boundary is [ADR-0014](./0014-physical-microservice-decomposition.md); cross-service contact is
events only ([ADR-0017](./0017-inter-service-integration-patterns.md)); in-DB tenant isolation is
[ADR-0019](./0019-per-shop-tenancy-via-postgres-rls.md).
</content>

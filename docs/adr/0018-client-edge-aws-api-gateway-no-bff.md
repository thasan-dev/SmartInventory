# Client edge: AWS API Gateway for routing + auth; no BFF in the MVP

How the clients (Angular SPA / storefront, back office) reach the services, and where read composition
happens.

## AWS API Gateway is the single client entry point

The existing **AWS API Gateway** is the client edge — nothing to build. It provides:

- **Routing** to the N services.
- **Auth for all three token surfaces** (ADR-0006/0012/0013) via authorizers: a **JWT authorizer** for
  the merchant and platform-admin **Entra** tokens, and a **Lambda authorizer** for the customer
  **app-issued phone-OTP** token. The structural separation of surfaces is enforced here — a token for
  one surface cannot reach another's routes.
- CORS, TLS, rate-limiting — managed.

## No BFF in the MVP

The "BFF" bundled two jobs: **auth/routing** (now the gateway's) and **read composition** (search
Catalog → collect SKUs → enrich with Sales availability). The composition job is **not built for the
MVP**:

- **The SPA composes client-side** — it calls Catalog **search** (which returns products + SKUs), then
  calls **Sales' bulk availability** endpoint (`availability(skuIds[])`), and merges. Two calls, not
  one; the enrich step is sequential (needs the SKUs from search first).
- **"One trip" is deferred, not designed away.** If listing-page latency ever justifies it, add a
  **single Lambda composition endpoint behind API Gateway** — the AWS-idiomatic "BFF-in-a-Lambda" — not
  a standalone BFF service. A separate BFF service is revisited only when multiple divergent clients
  (e.g. a native mobile app) exist.

## Consequences

- **Sales exposes a bulk availability endpoint** (`availability(skuIds[])`) for the client's enrich
  step (and for a future Lambda composer). Non-negotiable given client-side composition.
- **Stock is not a search facet/filter** — composition happens after Catalog has paginated, so the SPA
  can show an "in stock" badge but cannot "filter/sort by in stock." Adding that later means projecting
  a stock feed into Catalog's Elasticsearch index (a confined replica) — deferred (ADR-0015).
- **Writes go directly to the owning service** through the gateway; no write orchestration at the edge
  (choreography-first, ADR-0017).

## Considered and rejected

- **A standalone BFF service** — premature for a single primary client; its two jobs are covered by the
  gateway (auth/routing) and the SPA (composition). Revisit for multiple divergent clients.
- **Building a custom gateway** — unnecessary; AWS API Gateway already exists and covers routing + auth.
- **Catalog holding a stock replica to enable a one-trip endpoint** — rejected (ADR-0015): display
  availability is advisory and composed at the edge; the replica would exist only to serve composition.

## Relates to

Realizes the client-facing side of ADR-0014/0017; enforces the three-surface separation of
ADR-0006/0012/0013; depends on Catalog search (ADR-0014) and Sales availability (ADR-0016).

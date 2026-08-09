# Observability: OpenTelemetry end-to-end, correlation propagated through MassTransit

Choreographed multi-service flows (ADR-0017) are hard to follow when a request fans out over the bus. This
fixes how we trace them. Decision: **OpenTelemetry end-to-end**, with trace context and a **correlation id
auto-propagated across the SNS/SQS hop by MassTransit**, exported to **AWS X-Ray** via the ADOT collector.

## Model

- **One trace spans the whole flow.** MassTransit natively propagates **W3C trace context (`traceparent`)
  and `CorrelationId`** through publish/consume, so a single trace covers HTTP → EF outbox → SNS/SQS →
  consumer with **no hand-rolled plumbing**. A choreographed flow (e.g. price-changed → Sales pricing-replica
  update, or `ShopDeactivated` → Sales order-freeze) is one connected waterfall.
- **Correlation id** is seeded at the client edge (API Gateway request id, else generated at the first
  service) and rides **every event envelope** — beside `shop_id` (ADR-0019) — as MassTransit's
  `CorrelationId`.
- **Logs join traces.** The existing **Serilog → OpenTelemetry** pipeline (backend CLAUDE.md) stamps
  trace/span/correlation ids on every log line, so logs and spans cross-reference.
- **Export:** an **ADOT (AWS Distro for OpenTelemetry) collector** (sidecar on ECS / daemonset on EKS)
  receives OTLP and ships to **AWS X-Ray** (traces) and CloudWatch (logs/metrics).

## Domain trail vs technical trace — kept separate

The **Order Status History** + `CorrelationId` remain the **domain** audit trail (ADR-0017 §5) — "who did
what when," durable, business-readable. OpenTelemetry is the **technical** trace — spans, latency, failure
paths — retained per sampling, not a system of record. Neither replaces the other.

## Consequences

- **Sampling** is head-based with a tunable rate; errors always sampled. Traces are not a durable audit
  store (that's Status History / the domain trails, ADR-0020).
- **No tenant data in spans.** `shop_id` and ids are acceptable span attributes; customer/order PII is not
  put in spans (it lives in the domain stores under RLS, ADR-0019).

## Considered and rejected

- **Structured logs + a correlation id, no distributed tracing** — cheaper, but loses the cross-service
  span waterfall exactly where choreography makes failures hardest to follow.
- **Self-hosted Jaeger / Grafana Tempo** — capable, but reintroduces ops that managed X-Ray avoids; the OTel
  instrumentation is backend-agnostic, so this stays swappable if ever wanted.

## Relates to

Makes the correlation-id tracing of [ADR-0017](./0017-inter-service-integration-patterns.md) §5 concrete;
rides the SNS/SQS transport of [ADR-0022](./0022-message-broker-sns-sqs-via-masstransit.md); complements the
domain audit trails of [ADR-0020](./0020-domain-audit-trails-and-pricing-history.md).
</content>

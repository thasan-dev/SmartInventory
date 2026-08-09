# Infrastructure

> **Target architecture (decided, not yet built):** the system is being re-shaped from this single
> **.NET monolith on Azure** into **physical microservices on AWS** — see **ADR-0014–0028**. Superseding
> choices: broker **RabbitMQ / Azure Service Bus → Amazon SNS + SQS** (ADR-0022); DB **SQL Server →
> RDS PostgreSQL per service** with **RLS** tenancy (ADR-0019/0027); auth **Azure AD B2C / Entra →
> AWS Cognito** with a Pre-Token-Gen Lambda (ADR-0012 amendment); tracing → **OpenTelemetry → AWS X-Ray**
> (ADR-0023). **The sections below describe the current monolith as it runs today**, not the target.

- **Message bus**: configured via the `"MessageBroker"` key in `appsettings.json`, dispatched in `ServiceExtensions.AddMessageBroker()`.
  - **`"RabbitMq"`** (default) — `AddMassTransitUsingRabbitMq()`. Uses `PublishBrokerTopologyOptions.FlattenHierarchy` (single exchange). Exchange `exchange.inventories`, queue `queue.inventories`.
  - **`"AzureServiceBus"`** — `AddMassTransitUsingAzureServiceBus()` (production).
  - All brokers use the MassTransit EF Outbox with `QueryDelay = 10s` and SQL Server lock provider.
- **API versioning**: URL-segment versioning — `/inventories/v{version:apiVersion}/[controller]` (e.g. `/inventories/v1/plants`). Controllers annotated with `[ApiVersion(1.0)]`; Swagger groups by `v1.0` / `v2.0`.
- **Auth**: Azure AD B2C JWT bearer; `"UserRole"` policy requires a `role=user` claim (configured in `Program.cs`).
- **Logging**: Serilog → OpenTelemetry sink (`http://localhost:4318`), service name `InventoryService`.
- **Tracing & metrics**: OpenTelemetry OTLP (HTTP/Protobuf) with AspNetCore, HttpClient, and Runtime instrumentation.
- **Exception handling**: `BusinessExceptionHandler` middleware maps `BusinessException` subclasses → HTTP 422.

## Runtime prerequisites

- Running the API requires a reachable **SQL Server** and **RabbitMQ** broker per `appsettings.json`.
- **EF Core migrations** auto-apply at startup — see [ef-migrations.md](ef-migrations.md) for commands and rules.

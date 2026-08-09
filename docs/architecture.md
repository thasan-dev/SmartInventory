# Architecture
Architechtural pattern: event driven Microservices with Clean Architechture

## Solution Folder Structure

```
backend/                                                 # SmartInventory.sln
│
├── COMMANDS (write side)
│   ├── ...Commands.Infra.In.RestApi/                    # REST entry — controllers + HTTP command DTOs
│   ├── ...Commands.AntiCorruption.In.DomainEvents/      # Message entry — inbound MassTransit consumers (ACL)
│   ├── ...Application/                                  # Use cases — application services + application command DTOs
│   ├── ...Infra.Out.Repository/                         # Persistence — commands DbContext, repositories,EF configs, outbox
│   └── ...DomainModel/                                  # Aggregates, value objects, domain commands/events, repository interfaces
│
├── QUERIES (read side)
│   ├── ...Queries.Infra.In.RestApi/                     # REST entry — query controllers
|   ├── ...Application/                                  # Use cases — application services + application Queries DTOs
│   ├── ...Queries.Infra.Out.Repository/                 # Persistence — queries DbContext, EF configs
│   └── ...QueryModel/                                   # Read models returned to clients
│
└── SHARED
    ├── ...WebApi/                                       # Host / composition root — Program.cs, DI, Serilog/OTEL, EF migrations
    ├── SmartInventory.Messages.Inventories/             # Published message contracts shared with other services
    └── SmartInventory._Framework.*/                     # 5 reusable building blocks shared across bounded contexts
```

All `...` prefixes are `SmartInventory.Inventories`.


# Microservices Ecosystem — .NET 10 / C# 14

Clean Architecture, DDD, and event-driven microservices reference solution.

## Layout

```
Directory.Build.props                        Shared MSBuild properties (TargetFramework, LangVersion, CPM switch) — applies to every project whether opened via the .sln or standalone
Directory.Packages.props                      Central Package Management — every package version for the whole solution, in one place
src/
├── BuildingBlocks/
│   └── BuildingBlocks.Observability/         Shared correlation-id middleware + Serilog bootstrap, referenced by every API
├── Gateway/ApiGateway/                       YARP reverse proxy, rate limiting, correlation-id propagation
├── Identity/
│   ├── IdentityService.Domain/               User aggregate, invariants
│   ├── IdentityService.Application/          CQRS commands (Register/Login/Refresh), validators
│   ├── IdentityService.Infrastructure/       EF Core 10 + PostgreSQL, JWT issuance, auditing interceptor
│   └── IdentityService.Api/                  Minimal API endpoints, JwtBearer auth wiring
└── Services/
    └── InventoryService/                     THE GOLD MASTER TEMPLATE
        ├── InventoryService.Domain/          StockItem aggregate, domain events, domain errors
        ├── InventoryService.Application/     CQRS (MediatR), FluentValidation, CachingBehavior, consumer
        ├── InventoryService.Infrastructure/  EF Core 10 + PostgreSQL, Redis, MassTransit/RabbitMQ
        └── InventoryService.Api/             Minimal APIs, correlation middleware, IExceptionHandler, OTel
```

## Opening the solution

- **`MicroservicesEcosystem.sln`** opens the whole solution with Solution Folders (Gateway,
  Identity, Services, BuildingBlocks) grouping the projects for easier navigation.
- **Any individual `.csproj`** (e.g. `File > Open > Project/Solution` on
  `src/Services/InventoryService/InventoryService.Api/InventoryService.Api.csproj`) also opens
  and builds correctly on its own. `Directory.Build.props` and `Directory.Packages.props` sit at
  the repository root, and MSBuild automatically walks up the folder tree from any project to
  find them — Visual Studio doesn't need the `.sln` loaded for that to work, so package versions
  and shared settings stay consistent either way.

## Shared packages across services (Central Package Management)

Every `<PackageReference>` in every `.csproj` omits a `Version` attribute
(`<PackageReference Include="MediatR" />`). The actual version comes from the single
`<PackageVersion>` entry for that package in **`Directory.Packages.props`** at the repo root.
Bumping a package (e.g. MassTransit, Serilog, EF Core) for every service that uses it is a
one-line change in that file — there is no way for two services to silently drift onto
different versions of the same package. `Directory.Build.props` also centralizes the common
`PropertyGroup` (`TargetFramework`, `LangVersion`, `Nullable`, `ImplicitUsings`) so each
`.csproj` only declares what's actually specific to it (its `RootNamespace` and its package
list).

## Logging & observability

`BuildingBlocks.Observability` is a shared class library referenced by `ApiGateway`,
`IdentityService.Api`, and `InventoryService.Api` (and by `InventoryService.Infrastructure`,
for MassTransit correlation propagation). It provides:

- **`CorrelationIdMiddleware`** — one implementation, not copy-pasted per service. Captures or
  generates `X-Correlation-ID`, pushes it into Serilog's `LogContext`, and exposes it via
  `CorrelationIdMiddleware.GetCurrentCorrelationId(HttpContext)` for anything that needs to
  forward it (outbound HTTP calls, YARP-proxied requests, MassTransit message headers).
- **`SerilogBootstrap.AddSharedLogging(serviceName)`** — called once from each service's
  `Program.cs`. Configures structured JSON logging to **both** the console (for container log
  collectors / OpenTelemetry pipelines) **and** a daily rolling file under `./logs` (for local
  debugging without a log aggregator), enriched with service name, environment, machine name,
  thread id, and — for anything logged during a request — the correlation id.
- **`SerilogBootstrap.UseSharedRequestLogging()`** — wires Serilog's request-logging
  middleware so every HTTP request produces one structured summary line (method, path, status
  code, elapsed time, correlation id, authenticated user if any).

Each API's `appsettings.json` also has a `Serilog` section (minimum levels, per-namespace
overrides) that `ReadFrom.Configuration` picks up automatically, so you can tune log verbosity
per environment (`appsettings.Development.json`, `appsettings.Production.json`, environment
variables, etc.) without touching code.

## Cloning the Gold Master

To stand up a new module (e.g. `OrderService`):

1. Copy the entire `src/Services/InventoryService` directory to `src/Services/OrderService`.
2. Rename the four `.csproj` files, their `RootNamespace`, and every `namespace InventoryService.*` to `OrderService.*`.
3. Replace `StockItem` (Domain/Entities) with your own aggregate; replace the `AddStock`/`GetStock`
   vertical slice with your own commands/queries.
4. Update the connection string names (`OrderDb`) and RabbitMQ queue prefix in `appsettings.json`.
5. Everything else — auditing interceptor, correlation-id middleware, `IExceptionHandler`,
   `ValidationBehavior`, `CachingBehavior`, MassTransit retry/DLQ policy, OpenTelemetry wiring,
   and Serilog bootstrap — is inherited unchanged because it either lives in each layer's
   `DependencyInjection.cs` (copy verbatim) or is referenced from `BuildingBlocks.Observability`
   (no copying at all — just keep the `ProjectReference`).
6. Add a route + cluster for the new service in `ApiGateway/appsettings.json`.
7. Add the new projects to `MicroservicesEcosystem.sln` under the `Services` solution folder
   (or just open the new `.csproj` files standalone — see "Opening the solution" above).

## Vertical slice reference (Inventory)

- **Write path — `AddStockCommand`**: FluentValidation (via `ValidationBehavior`) →
  `StockItem.AddStock()` domain method → EF Core/PostgreSQL persistence →
  `StockAddedIntegrationEvent` published via MassTransit/RabbitMQ → Redis cache key
  for the SKU invalidated.
- **Read path — `GetStockQuery`**: implements `ICacheableQuery` → `CachingBehavior`
  checks Redis first → on miss, `GetStockQueryHandler` reads PostgreSQL → result is
  written back to Redis before being returned.
- **Consumer — `StockAddedEventConsumer`**: subscribes to `StockAddedIntegrationEvent`,
  demonstrating safe event processing inside MassTransit's own retry + DLQ pipeline.

## Running locally

Each API expects PostgreSQL (`Npgsql`), Redis, and RabbitMQ reachable via the connection
strings in its `appsettings.json`. `docker compose` definitions for these dependencies are
not included here; point the connection strings at your own local or containerized instances.

```bash
dotnet restore MicroservicesEcosystem.sln
dotnet build MicroservicesEcosystem.sln
dotnet run --project src/Identity/IdentityService.Api
dotnet run --project src/Services/InventoryService/InventoryService.Api
dotnet run --project src/Gateway/ApiGateway
```

Logs land in the console (structured JSON) and under `<project>/logs/<ServiceName>-<date>.log`
for each service while it's running.

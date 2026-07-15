# Development Guidelines

## Purpose

This document defines KhaiKang's engineering rules. `AGENTS.md` is the concise
operational entry point; this document explains the design decisions that apply
to human and automated contributors.

## Engineering Priorities

When principles compete, use this order:

1. Correctness and security
2. Readability and maintainability
3. Consistency with accepted module boundaries
4. Simplicity
5. Reuse and abstraction

Do not optimize for fewer files, shorter code, or a fashionable pattern at the
expense of the priorities above.

## Delivery Workflow

- Read the relevant spec and data model before implementation.
- Resolve material open questions before encoding them as behavior.
- Implement the smallest complete vertical slice that can be verified.
- Keep specs, code, tests, database migrations, and API contracts consistent.
- Use a short design note or architecture decision record for decisions that
  change module boundaries, persistence strategy, public contracts, security,
  or deployment topology.
- A plan is useful for cross-cutting work, but the repository does not require
  role-based agent handoffs.

## Modular Monolith

KhaiKang is a modular monolith deployed as a small set of process hosts.
Multiple projects in the solution express compile-time boundaries; they do not
imply independently deployed microservices.

### Hosts

- `KhaiKang.Api` is the HTTP host and composition root.
- `KhaiKang.Worker` is the background-processing host.
- Hosts register modules and infrastructure but do not own business rules.

### Feature modules

Backend business capabilities are organized by feature under
`backend/src/modules/` and named `KhaiKang.Modules.<Feature>`.

Start with one class-library project per feature module. Inside that project,
use only the folders the module needs:

```text
KhaiKang.Modules.ProjectManagement/
  Domain/
  Application/
  Infrastructure/
  Endpoints/
```

- `Domain` contains entities, value objects, and invariants.
- `Application` contains use cases and module-facing contracts.
- `Infrastructure` contains EF Core configuration and external adapters.
- `Endpoints` maps HTTP requests to application use cases.

Do not create separate Domain, Application, and Infrastructure projects until
their independent compilation boundary provides measurable value.
Do not create empty layer folders as architecture placeholders; add a folder
only when the current vertical slice has code that belongs there.

### Module interaction

- A module owns its data and persistence configuration.
- Other modules must not read or update its tables directly.
- Cross-module behavior uses an explicit application contract or domain event.
- Do not reference endpoint types, EF entities, or infrastructure classes from
  another module.
- Prefer an in-process call. Add messaging only for behavior that genuinely
  benefits from asynchronous delivery or retry semantics.

## Common Components

`KhaiKang.CommonUtils` and `KhaiKang.CommonUtils.Web` are backend-only shared
components. Code belongs there only when all of the following are true:

- it is domain-neutral;
- at least two modules need the same behavior;
- its API is stable enough to support multiple callers;
- moving it does not cause feature modules to depend on each other's concepts.

Valid examples include time abstractions, middleware, and cross-cutting
observability primitives. Domain entities, feature DTOs, project permissions,
and workflow rules remain in their owning module. Configuration uses the
standard Configuration and Options patterns; do not create a custom
environment-variable helper.

Code adapted from another repository must be reviewed as new KhaiKang code. It
must not retain company-specific names, headers, secrets, infrastructure
assumptions, or business behavior, and its license must permit reuse.

## API Design

### Routes and methods

- Version public APIs under `/api/v1`.
- Use lowercase plural resource names, for example `/api/v1/projects`.
- Use `GET` for reads, `POST` for creation, `PUT` for full replacement,
  `PATCH` for partial updates, and `DELETE` for deletion.
- Use query parameters for ordinary filtering, sorting, and paging. Use a
  dedicated search endpoint only when the query cannot be represented safely
  and clearly in a URL.
- Do not put implementation actions such as `create`, `update`, or `query` in a
  resource route.

### Responses and errors

- Return the resource or an explicit operation DTO for successful requests.
- Return `200` with an empty collection or page when a collection query has no
  matches.
- Use `201 Created` and a location for newly created resources when practical.
- Use ASP.NET Core `ProblemDetails` and `ValidationProblemDetails` for errors.
- HTTP status codes are authoritative. Do not duplicate them in an envelope.
- Use a stable Problem Details `type` as the error category identifier. Add a
  stable `code` extension when a client must branch on a business condition;
  never use a human-readable message as an identifier.
- Keep trace and correlation identifiers in observability data rather than the
  public error contract. Server logs still retain trace context for request
  correlation.
- Do not expose exception messages, stack traces, connection details, or other
  sensitive diagnostics to clients.

Application and Domain code must not reference ASP.NET Core, HTTP status codes,
or `ProblemDetails`. Modules define domain and application error semantics; the
Web layer maps them centrally to HTTP responses. Use these default mappings:

| Error semantics | HTTP status |
| --- | --- |
| Request shape, format, or boundary validation failure | `400 Bad Request` |
| Missing authenticated identity | `401 Unauthorized` |
| Authenticated identity lacks permission | `403 Forbidden` |
| Resource does not exist | `404 Not Found` |
| Uniqueness, version, or state-transition conflict | `409 Conflict` |
| Unexpected error | `500 Internal Server Error` |

### Contracts

- OpenAPI is the source of truth for the HTTP wire contract.
- Frontend API types are generated from OpenAPI and are not edited manually.
- The C# contract project may contain host-facing DTOs and shared wire concepts,
  but TypeScript does not consume its source directly.
- Treat route, status, request, response, nullability, and enum changes as API
  contract changes.
- Isolate external systems behind adapters that translate their wire contract
  into KhaiKang concepts.

## .NET Rules

See [the .NET development guidelines](./dotnet-development-guidelines.md) for
the detailed backend baseline. This section keeps only the cross-project
summary.

- Target .NET 10, enable nullable reference types, and use file-scoped
  namespaces.
- Treat the root `.editorconfig` as the enforceable formatting source. C# uses
  four spaces, braces for control flow, and one primary public type per file.
- Use PascalCase for public members and types, camelCase for locals and
  parameters, and an `Async` suffix for awaitable methods.
- Use English identifiers, comments, log templates, and XML documentation.
- Prefer immutable request and response records or init-only properties.
- Public collections should expose read-only abstractions where mutation is not
  part of the contract.
- Use constructor injection. Do not use service locator or property injection.
- Use async I/O and propagate `CancellationToken`.
- Avoid automatic mapping, mediator, generic repository, and result-wrapper
  libraries by default. Add one only after documenting the problem it solves.
- Log structured data with stable templates. Do not catch an exception only to
  log and rethrow it.

## Configuration and Secrets

- Use the standard ASP.NET Core configuration pipeline.
- The host or feature module that consumes a setting owns its options type. Do
  not centralize settings in a project-wide environment-variable enum or
  helper.
- Bind related settings with `BindConfiguration` and validate them with
  `ValidateDataAnnotations`, custom validation, or an equivalent mechanism.
- Use `ValidateOnStart` for required settings so invalid configuration fails
  during startup.
- Feature code consumes `IOptions<TOptions>`, `IOptionsSnapshot<TOptions>`, or
  `IOptionsMonitor<TOptions>` and does not call
  `Environment.GetEnvironmentVariable` directly.
- Use `appsettings.json` only for non-secret defaults.
- Use standard hierarchical environment variable names such as
  `Postgres__ConnectionString` for deployment overrides and .NET user secrets
  for local secrets.
- Commit an `.env.example` only when it contains placeholders, never real
  values.
- Fail startup with a clear error when required configuration is missing.

## Persistence

- PostgreSQL is the system of record and EF Core is the default persistence
  mechanism.
- Use `snake_case` for tables, columns, indexes, and constraints.
- Use UUID primary keys unless a model has a documented reason to use another
  strategy.
- Store timestamps as UTC `timestamp with time zone` values.
- Keep one database for the modular monolith. Each module owns its entity and
  EF Core configuration.
- Start with one application `DbContext`; split contexts only when module
  isolation or operational needs justify the added migration complexity.
- Every schema change requires a migration and a rollback or recovery
  consideration.
- Seed only stable system reference data. Do not hide environment-specific or
  user data in migrations.
- Transactions belong at the application use-case boundary. Do not hold a
  database transaction open across external network calls.

## Validation, Errors, and Authorization

- Validate request shape, required values, formats, and basic ranges at the API
  boundary.
- Validate business invariants and state transitions in the application or
  domain layer.
- Do not duplicate authorization logic in endpoints. Use policies and explicit
  project membership checks.
- Global system roles and project-scoped roles remain separate.
- Error identifiers must be stable and machine-readable when clients need to
  branch on a business condition; human messages are not identifiers.
- Audit security-sensitive and business-significant state changes without
  logging credentials or secret values.

## Frontend Rules

- Use Vue 3, TypeScript strict mode, Composition API, and
  `<script setup lang="ts">`.
- Use PascalCase for components, `use` prefixes for composables, and clear
  feature-oriented folders.
- Do not use `any`, unchecked casts, or duplicated handwritten API models.
- Views and stores use feature API modules or composables instead of calling
  the HTTP client directly.
- Keep shared stores for cross-component application state. Keep page-local
  state in the page or a page-scoped composable.
- Components must provide loading, empty, error, and disabled states where the
  workflow requires them.
- Accessibility, keyboard operation, responsive layout, and readable focus
  states are part of feature completion.

## Testing

- Test behavior and public contracts, not private implementation details.
- Bug fixes should include a regression test when practical.
- Unit tests must be deterministic and isolated from live databases, queues,
  external APIs, wall-clock time, and arbitrary sleeps.
- Integration tests cover the HTTP pipeline, authorization, EF Core mappings,
  migrations, and infrastructure boundaries.
- Prefer real PostgreSQL-compatible integration behavior over an EF in-memory
  provider when relational semantics matter.
- Contract tests protect routes, status codes, problem details, JSON shape,
  nullability, and generated clients.
- Use Arrange, Act, and Assert structure when it improves readability; do not
  add comments that only repeat the code.

## Git and Review

- Follow Conventional Commits.
- Keep commits and pull requests focused and reviewable.
- Do not mix formatting, dependency upgrades, refactoring, and feature behavior
  unless they are required by the same change.
- Pull requests describe behavior, design tradeoffs, migration impact, and
  verification.
- Run the available build, tests, formatting, and static analysis before
  requesting review.

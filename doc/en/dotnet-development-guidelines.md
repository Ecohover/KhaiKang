# .NET Development Guidelines

> Status: Accepted. This document defines the adopted development baseline. SDK, MSBuild, NuGet, analyzer, and CI enforcement have not yet been applied and will be introduced as a separate change.

## Purpose

This document defines KhaiKang's shared .NET engineering baseline. The rules favor open-source collaboration, verifiability, and long-term compatibility without requiring company-specific packages, naming, or infrastructure.

The normative terms mean:

- **MUST**: required before merge unless an ADR records an exception.
- **SHOULD**: the default; explain deviations in the pull request.
- **MAY**: optional based on the needs of a module.

## Minimum Development Baseline

These rules do not prescribe a fixed template. They allow different contributors to modify one codebase safely.

- Standard .NET CLI commands are sufficient to restore, build, and test. No company-internal tooling or package is required.
- Planner, Builder, Reviewer, handoff documents, and a specific AI workflow are not required.
- A service does not automatically need an interface, base class, factory, repository, or partial class.
- Small features remain small. Add abstractions and layers only when real complexity appears.
- Security, public contracts, data migrations, module boundaries, and behavioral correctness are the non-optional review gates.
- A rule that is not yet enforced by the compiler, an analyzer, or CI remains review guidance and should not block a contribution based only on personal preference.
- Add a repository-wide rule only after confirming that it addresses a recurring problem rather than a one-off implementation preference.

## Toolchain and Build

- Use .NET 10 and C# 14.
- `global.json` is the source of truth for the SDK version and preview policy. Release branches MUST NOT build with preview SDKs.
- `Directory.Build.props` is the source of truth for compiler, analyzer, and warning policy.
- `Directory.Packages.props` centrally manages NuGet versions. Ordinary project files do not specify package `Version` values.
- Nullable reference types, .NET analyzers, and build-time code-style enforcement MUST remain enabled.
- Release CI SHOULD treat compiler warnings and adopted analyzer warnings as errors. Local development still uses ordinary `dotnet build` without a custom wrapper.
- Do not hide issues with a global `NoWarn`; a narrow suppression MUST include a specific rationale.
- `dotnet restore`, `dotnet build`, `dotnet format --verify-no-changes`, and `dotnet test` are the baseline backend pull-request checks.

## Projects and Dependency Direction

KhaiKang is a modular monolith. Project references establish dependency direction; they do not imply independent deployment.

```text
KhaiKang.Api
  -> Feature modules
  -> KhaiKang.CommonUtils.Web

KhaiKang.Worker
  -> Feature modules
  -> KhaiKang.CommonUtils

KhaiKang.CommonUtils.Web
  -> KhaiKang.CommonUtils
```

- API and Worker own composition, hosting, and cross-cutting pipelines, not business rules.
- A feature starts as one `KhaiKang.Modules.<Feature>` class library. Add `Domain`, `Application`, `Infrastructure`, and `Endpoints` folders only when real code needs them.
- Domain code MUST NOT depend on ASP.NET Core, EF Core implementations, HTTP DTOs, or external-system SDKs.
- Feature-owned HTTP DTOs remain in the owning module. Host-only DTOs remain in
  the host. Their wire shapes implement the canonical OpenAPI contract.
- CommonUtils remains domain-neutral and requires at least two real callers before an abstraction moves there.
- A module MUST NOT reference another module's entities, DbContext configuration, endpoint types, or internal implementation.

## C# Code

- Use file-scoped namespaces, four-space indentation, and braces for control flow.
- Keep one primary public type per file. A small private nested type may remain with its owner.
- Use PascalCase for public APIs, camelCase for parameters and locals, and `_camelCase` for private fields.
- Awaitable methods use an `Async` suffix; ASP.NET endpoint handlers and framework overrides may follow framework conventions.
- Prefer immutable records, constructors, or init-only properties for requests, responses, and value objects.
- Required reference members use a constructor or `required`; do not fake initialization with `string.Empty` or the null-forgiving operator.
- Public collections expose `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, or `IEnumerable<T>` when callers should not mutate them.
- Consider `sealed` for classes that are not extension points, while allowing inheritance where EF Core or testing genuinely needs it.
- Primary constructors are allowed but not mandatory. Readability and explicit dependencies take precedence.
- Write XML documentation in English for public extension points, library APIs, and contracts whose behavior is not clear from their names. Do not restate every public member.
- Do not introduce abstractions, base services, or helpers merely to reduce line count.

## Dependency Injection

- Express dependencies through constructor injection.
- Property injection and business-code service location through `IServiceProvider` are prohibited.
- Each feature module exposes one clear registration entry point, such as `AddProjectManagementModule`.
- A singleton MUST NOT directly depend on a scoped service. Lifetimes must match state ownership and thread-safety.
- Endpoints and use cases do not instantiate database, HTTP, clock, queue, or other external dependencies directly.
- Do not require an interface for every service. Add one for multiple implementations, a cross-module contract, a test boundary, or another demonstrated need.

## Async, Cancellation, and Time

- Database, network, file, and queue I/O use asynchronous APIs.
- Public application use cases and endpoints accept and propagate `CancellationToken`; do not replace the caller token with `default`.
- Library code does not use `.Result`, `.Wait()`, or arbitrary `Thread.Sleep` calls.
- Store and exchange timestamps as UTC using `DateTimeOffset`.
- Business logic that needs the current time receives `TimeProvider` instead of depending directly on `DateTime.Now` or an uncontrollable `UtcNow`.

## Configuration and Secrets

- Use the standard .NET Configuration pipeline and Options Pattern.
- The host or feature module that consumes a setting owns its options type.
- Bind with `BindConfiguration`, validate values, and use `ValidateOnStart` for required settings.
- Feature code consumes `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`.
- Do not create a project-wide environment-variable enum/helper or call `Environment.GetEnvironmentVariable` from feature code.
- `appsettings.json` contains only non-secret defaults. Use User Secrets locally and environment variables or a secret provider in deployments.
- Never commit secrets, tokens, real credentials, production URLs, or production data.

## HTTP API and Contracts

- Use resource-oriented routes under `/api/v1`.
- Minimal APIs and controllers are both valid, but a feature module SHOULD be internally consistent and endpoints do not own business rules.
- Return resources or operation DTOs directly. Do not use a generic `ApiResponse<T>` envelope.
- Resource creation SHOULD return `201 Created` with a location. Successful deletion without a body returns `204 No Content`.
- Errors use RFC 9457 Problem Details. HTTP status is authoritative; stable `type` values and an optional `code` extension provide machine-readable identity.
- Trace IDs, stack traces, exception messages, and connection details are not part of the public API contract.
- Application and Domain code MUST NOT reference `ProblemDetails` or HTTP status codes. The Web boundary maps application error semantics centrally.
- Validate request shape at the HTTP boundary and business invariants in Application or Domain.
- OpenAPI is the source of truth for the HTTP wire contract. Endpoints declare their actual request, response, status, and authorization metadata.
- Client-visible route, status, JSON shape, nullability, and enum changes are contract changes and require tests.

## Persistence

- PostgreSQL is the system of record and EF Core is the default data-access technology.
- Start with an explicit `DbContext` and feature queries/use cases. Do not add a generic repository or unit-of-work wrapper by default.
- A feature module owns its entities and EF Core configuration. Other modules do not modify its tables directly.
- Schema changes require migrations. Production startup MUST NOT substitute `EnsureCreated` for migrations.
- Read-only queries SHOULD project required data and use `AsNoTracking` when tracking is unnecessary.
- Avoid N+1 queries, unbounded collections, and loading complete entities only to discard most fields.
- Database transactions align with application use cases and do not remain open across external network calls.
- Relational behavior tests do not use the EF Core in-memory provider as a substitute for PostgreSQL semantics.

## Logging and Observability

- Use `ILogger<T>` with structured message templates instead of interpolated log strings.
- Use `LoggerMessage` source generation for hot or high-volume paths. Ordinary low-volume logs do not require a partial logging file.
- Include actionable domain identifiers and outcomes without logging credentials, tokens, complete personal data, or sensitive payloads.
- Expected validation, not-found, and conflict outcomes should not all be logged as errors.
- Do not catch an exception only to log and rethrow it unchanged. Boundaries record unexpected exceptions centrally.
- Keep trace context in the observability pipeline without promising it as a response-body field.

## Testing

- Use xUnit. Test names describe scenario and outcome, for example `CreateProject_WhenNameIsDuplicate_ReturnsConflict`.
- Test observable behavior rather than private methods or implementation steps.
- When practical, reproduce a bug with a failing regression test before fixing it.
- Domain and application unit tests do not connect to databases, queues, networks, or wall-clock time.
- Use `WebApplicationFactory` for HTTP pipeline integration tests.
- Use a reproducible PostgreSQL integration environment for persistence, migrations, and PostgreSQL-specific behavior.
- Tests do not depend on execution order, arbitrary sleeps, developer-machine data, or pre-existing external services.
- Separate Arrange, Act, and Assert with whitespace when it improves readability; do not add comments that only restate code.
- New features cover the success path, material validation, authorization, and important conflict/not-found contracts.

## Dependency Governance

- Prefer built-in .NET, ASP.NET Core, EF Core, and existing project capabilities.
- A new package must solve a concrete problem. The pull request explains maintenance status, license, transitive dependencies, and alternatives.
- Do not add mediator, AutoMapper, generic repository, result-wrapper, or validation frameworks by default.
- Update versions only in `Directory.Packages.props`; Microsoft runtime-related packages SHOULD remain in the same patch family.
- Do not globally disable NuGet vulnerability auditing for convenience.

## Definition of Done

Before requesting review for a backend change, run:

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
dotnet build backend/KhaiKang.Backend.slnx --configuration Release --no-restore
dotnet format backend/KhaiKang.Backend.slnx --verify-no-changes --no-restore
dotnet test backend/KhaiKang.Backend.slnx --configuration Release --no-build
```

If an environment prevents a check, the pull request states which check was skipped, why, and what alternative verification was performed.

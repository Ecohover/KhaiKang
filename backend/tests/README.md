# Backend Test Structure

Backend tests are separated first by test layer and then by feature module. A
test should live beside tests that protect the same public behavior, not beside
an implementation detail that happens to be used by the test.

The refactoring safety baseline and behavior matrix are recorded in
[`refactoring-test-baseline.md`](refactoring-test-baseline.md).

## Directory Layout

```text
backend/tests/
|-- unit/
|   `-- KhaiKang.Domain.UnitTests/
|       |-- Identity/
|       |-- ProjectManagement/
|       `-- TestManagement/
`-- integration/
    `-- KhaiKang.Api.IntegrationTests/
        |-- Identity/
        |-- Infrastructure/
        |-- ProjectManagement/
        |-- System/
        `-- TestManagement/
```

## Domain Unit Tests

Domain unit tests protect business state transitions, invariants, snapshots,
and audit/version behavior. They run without HTTP, databases, file systems,
queues, clocks tied to wall time, or other external services.

- Use real domain objects.
- Pass fixed timestamps and explicit identifiers.
- Mock or fake an external boundary only when testing an application use case.
- Do not mock EF Core `DbSet` or use EF Core InMemory as a unit-test database.
- Keep one test class focused on one subject or one closely related metadata
  group.

When application behavior can be isolated through real application contracts,
add a separate `KhaiKang.Application.UnitTests` project instead of mixing those
tests into the domain project. Services that directly depend on a concrete
`DbContext` must first gain a justified boundary; do not add an interface only
to satisfy a mocking framework.

## Fast API Integration Tests

`KhaiKang.Api.IntegrationTests` starts the real ASP.NET Core application through
`WebApplicationFactory`. It exercises routing, authentication, dependency
injection, application services, EF Core mappings, and HTTP contracts.

The current test host uses:

- in-memory SQLite instead of PostgreSQL;
- a unique temporary local directory for attachment storage;
- the real application pipeline and module registrations.

These tests are fast API integration tests. They do not prove PostgreSQL
migration, locking, index, type, or concurrency semantics.

## External PostgreSQL and Deployment Tests

The repository already has an external deployment-level test in
[`deploy/Test-MvpSmoke.ps1`](../../deploy/Test-MvpSmoke.ps1). It exercises
published application images with fresh PostgreSQL and attachment volumes,
including restart and persistence checks. This is the current external-system
test layer.

The deployment smoke test proves that the assembled system can run, but its
large workflow is not a replacement for focused persistence regression tests.

PostgreSQL-specific behavior should live in a separate
`KhaiKang.PostgreSql.IntegrationTests` project when that suite is introduced.
It must run against an explicitly provisioned disposable PostgreSQL instance
and apply the real migrations before testing persistence behavior.

Keep this suite separate so developers can distinguish:

- fast tests that run on every change;
- deployment smoke tests that validate assembled images and volumes;
- external integration tests that require a database container or CI service.

## Naming

Use `Subject_Scenario_ExpectedOutcome` for focused tests. A scenario test that
crosses several API calls may use `FeatureFlow_ExpectedGuarantee` when the
complete workflow is the behavior under test.

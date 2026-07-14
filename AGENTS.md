# Repository Guidelines for Coding Agents

## Purpose

This is the tool-neutral entry point for coding agents. Keep changes compatible
with `CONTRIBUTING.md` and the detailed engineering rules in
`doc/en/development-guidelines.md`.

## Source of Truth

Read these sources before editing, in order:

1. The relevant functional spec under `doc/zh-TW/02-specs/`.
2. The relevant data model under `doc/zh-TW/03-data-model/`.
3. `doc/en/development-guidelines.md`.
4. Existing code and tests in the affected module.

Do not silently resolve conflicts between specs, data models, tests, and code.
Report the conflict and keep the authoritative document aligned with behavior.

## Repository Map

```text
src/
  backend/
    common/                    Domain-neutral backend components
    modules/                   Feature modules
    KhaiKang.Api/              HTTP host and composition root
    KhaiKang.Worker/           Background-processing host
  frontend/                    Vue 3 application
  contract/                    HTTP contract support
tests/                         Automated tests
doc/                           Architecture, specs, models, and planning
deploy/                        Self-hosted deployment assets
```

## Architecture Rules

- Build a modular monolith. Multiple projects express compile-time boundaries,
  not independently deployed microservices.
- Organize business code by feature under `src/backend/modules/`, using
  `KhaiKang.Modules.<Feature>` for module projects.
- Keep API and Worker hosts thin. Business rules belong to feature modules.
- A module owns its entities and persistence configuration. Cross-module access
  uses an explicit application contract or domain event.
- Start with one project per feature module. Add `Domain`, `Application`,
  `Infrastructure`, or `Endpoints` folders only when they contain real code.
- CommonUtils contains only stable, domain-neutral behavior needed by multiple
  modules. Never move feature DTOs, permissions, or workflow rules into common.
- Do not import company-specific names, headers, credentials, infrastructure,
  or business behavior from another repository.

## API and Contract Rules

- Use resource-oriented routes under `/api/v1`, standard HTTP methods, and
  meaningful status codes.
- Successful responses return resources or explicit operation DTOs. Errors use
  ASP.NET Core `ProblemDetails` or `ValidationProblemDetails`.
- Keep trace and correlation identifiers in observability data, not in the
  public error contract.
- Empty collection queries return `200` with an empty collection or page.
- OpenAPI is the HTTP wire-contract source for generated frontend models.
- Treat route, status, request, response, nullability, and enum changes as
  compatibility-sensitive contract changes.
- Isolate external wire formats behind adapters.

## Engineering Rules

- Target .NET 10, enable nullable reference types, and follow `.editorconfig`.
- Use constructor injection, async I/O, and propagated `CancellationToken`.
- Use standard .NET Configuration and validated options. Secrets belong in
  environment variables or user secrets, never committed files.
- Do not read environment variables directly from feature code or introduce a
  custom environment-variable abstraction.
- Use PostgreSQL and EF Core by default. Schema changes require migrations.
- Store time in UTC and convert only at presentation boundaries.
- Use Vue 3, TypeScript strict mode, Composition API, and generated API models.
- Do not add mapping, mediator, repository, or result-wrapper libraries without
  a demonstrated need and an accepted project decision.

## Testing and Verification

- Add or update tests for behavior changes. Bug fixes should include a
  regression test when practical.
- Unit tests must not depend on live databases, queues, external APIs, current
  time, or arbitrary sleeps.
- Use integration tests for HTTP, authorization, persistence, migrations, and
  infrastructure boundaries.
- Test public behavior and contracts, not private implementation details.

Current backend commands:

```shell
dotnet restore KhaiKang.slnx --configfile NuGet.config
dotnet build KhaiKang.slnx --no-restore --disable-build-servers -m:1
dotnet test KhaiKang.slnx --no-build --disable-build-servers -m:1
```

Add frontend commands after the frontend workspace is scaffolded.

## Change Hygiene

- Preserve unrelated work in a dirty worktree. Do not reset changes outside the
  task.
- Keep changes focused and avoid unrelated refactors or dependency upgrades.
- Never weaken authentication, authorization, validation, or audit behavior to
  make a test pass.
- Update specs, migrations, OpenAPI output, and tests with behavior changes.
- Do not commit, push, publish, or deploy unless explicitly requested.

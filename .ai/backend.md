# Backend Context

Read this file only for changes under `backend/`.

## Required References

- Read `doc/en/dotnet-development-guidelines.md` for detailed .NET rules.
- Read the relevant specification and data model under `doc/zh-TW/` when
  changing business behavior.
- Use `.ai/verification.md` before reporting completion.

## Architecture Boundaries

- Build a modular monolith. Projects express compile-time boundaries, not
  independently deployed microservices.
- Put feature code under `backend/src/modules/` and use
  `KhaiKang.Modules.<Feature>` for module projects.
- Keep API and Worker hosts thin. Business behavior belongs to feature modules.
- A module owns its entities and persistence configuration. Cross-module access
  uses an explicit application contract or domain event.
- Start with one project per feature. Split layers only when real complexity
  requires it.
- CommonUtils contains only stable, domain-neutral behavior used by multiple
  modules. Do not put feature DTOs, permissions, or workflow rules there.

## Implementation Rules

- Target .NET 10, enable nullable reference types, and follow `.editorconfig`.
- Use constructor injection, async I/O, and propagated `CancellationToken`.
- Use standard .NET Configuration and validated options. Do not add a custom
  environment-variable abstraction or read environment variables in features.
- Use PostgreSQL and EF Core by default. Schema changes require migrations.
- Store time in UTC and convert only at presentation boundaries.
- Do not add mapping, mediator, repository, or result-wrapper libraries without
  a demonstrated need and an accepted project decision.
- Add or update tests for behavior changes. Bug fixes should include a
  regression test when practical.

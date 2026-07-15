# Contributing to KhaiKang

Thank you for contributing to KhaiKang. The project is developed as a
self-hostable modular monolith and favors small, reviewable changes.

## Before You Start

- Read `AGENTS.md` for repository boundaries and verification rules.
- Read `doc/en/dotnet-development-guidelines.md` before backend changes.
- Read the relevant spec under `doc/zh-TW/02-specs/` and data model under
  `doc/zh-TW/03-data-model/`.
- Discuss large architecture changes, new infrastructure dependencies, or
  public API redesigns before implementation.
- Do not include secrets, production data, or company-specific configuration.

## Development Setup

The backend currently requires the stable .NET 10 SDK. Standard .NET CLI
commands are sufficient; no company-internal tooling is required.

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile NuGet.config
dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1
```

```shell
dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1
```

Frontend and Docker Compose setup instructions will be added when those
workspaces are scaffolded.

## Making Changes

1. Keep one pull request focused on one behavior or cohesive change.
2. Update or add tests for changed behavior.
3. Update specs, data models, migrations, and OpenAPI artifacts when applicable.
4. Run the relevant build, test, formatting, and static-analysis commands.
5. Explain user-visible behavior, design decisions, and verification in the
   pull request description.

## Commit Messages

Use Conventional Commits:

```text
<type>[optional scope]: <description>
```

Common types are `feat`, `fix`, `docs`, `test`, `refactor`, `build`, and
`chore`. Use an imperative, concise description. Mark breaking changes with
`!` or a `BREAKING CHANGE:` footer.

Examples:

```text
feat(projects): add project creation endpoint
fix(auth): revoke sessions for disabled accounts
docs: define backend module boundaries
```

## Pull Request Checklist

- [ ] The change matches an accepted spec or explains the required spec update.
- [ ] Tests cover new or changed behavior.
- [ ] Public API and database changes are documented.
- [ ] No secrets, generated build output, or unrelated changes are included.
- [ ] Local verification commands pass.

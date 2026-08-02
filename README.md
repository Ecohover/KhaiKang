# KhaiKang

KhaiKang is an open-source, self-hostable development and test management platform for individual developers and small-to-medium software teams.

It connects requirements, delivery planning, implementation, test management, quality gates, and CI/CD feedback while continuing to use specialized external tools such as Jenkins, GitHub Actions, Playwright, and other test runners.

KhaiKang is Kanban-first and Scrum-compatible. AI can assist at every workflow stage, but projects may require human review before work is allowed to continue.

> AI proposes, humans approve when required, automation executes, and KhaiKang tracks.

## What is included today

- Project workspaces, members, settings, and issue management.
- Test suites, versioned test cases, test plans, and recorded test runs.
- A Vue web application, .NET API, PostgreSQL persistence, and OpenAPI contract.
- Self-hosted Docker Compose deployment using published Docker Hub images.

KhaiKang is under active development. The project is suitable for evaluation and
self-hosted use; review the deployment and security guidance before exposing an
instance to the internet.

## Product direction

KhaiKang is intended to become the management and orchestration layer above existing engineering tools. It does not aim to replace source control, CI servers, deployment engines, or test frameworks.

The target delivery flow is:

1. Record a requirement.
2. Let AI analyze the requirement and relevant repository context.
3. Propose the technical approach, development plan, acceptance criteria, and test cases.
4. Develop the feature and its tests in parallel.
5. Build and run unit, integration, API, end-to-end, and manual tests as applicable.
6. Promote the same build artifact through project-defined environments.
7. Evaluate configurable quality conditions and optional human approvals at every stage.
8. Produce a requirement-level quality report and approve production deployment.

For the MVP, environment workflows remain linear and template-based. The data model should preserve future configurability without requiring a complete visual workflow designer in the first release.

See [Product Vision and MVP Workflow](./doc/en/product-vision-and-mvp-workflow.md).

## Quick Start

### Run with Docker (recommended for evaluation)

The published Docker Hub images start the web application, API, and PostgreSQL
as one Compose stack. Docker Engine with Docker Compose v2 is the only
prerequisite.

```shell
git clone https://github.com/Ecohover/KhaiKang.git
cd KhaiKang/deploy/compose
cp .env.example .env
# Edit .env and replace POSTGRES_PASSWORD with a long random value.
docker compose pull
docker compose up -d
```

Open `http://localhost:8080` and create the first system administrator. See
[Docker deployment](./deploy/README.md) for upgrades, backups, HTTPS, and
production guidance. Use a fixed `sha-...` image tag rather than `latest` for
repeatable production deployments.

For an internet-facing installation, use the included Caddy HTTPS override and
set `KHAIKANG_REQUIRE_HTTPS=true` only after TLS is working. Do not expose the
API or PostgreSQL service directly.

### Run from source

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0),
PostgreSQL, Node.js 22, and pnpm 10.

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
dotnet tool restore
dotnet user-secrets set --project backend/src/KhaiKang.Api "ConnectionStrings:KhaiKang" "Host=localhost;Port=5432;Database=khaikang;Username=khaikang;Password=<your-local-password>"
dotnet ef database update --project backend/src/modules/KhaiKang.Modules.Identity --startup-project backend/src/KhaiKang.Api
dotnet run --project backend/src/KhaiKang.Api/KhaiKang.Api.csproj
```

The development profile listens on `http://localhost:5220`:

- Health: `GET /health/live`
- System information: `GET /api/v1/system/info`
- Canonical OpenAPI contract: `GET /openapi/v1.yaml`

Run backend verification with:

```shell
dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1
dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1
```

Start the frontend in another terminal:

```shell
cd frontend
pnpm install
pnpm dev
```

See [frontend/README.md](./frontend/README.md) for its workspace commands.

## Documentation

- [Traditional Chinese README](./README.zh-TW.md)
- [Docker deployment](./deploy/README.md)
- [Security policy](./SECURITY.md)
- [Code of Conduct](./CODE_OF_CONDUCT.md)
- [Changelog](./CHANGELOG.md)
- [Product Vision and MVP Workflow](./doc/en/product-vision-and-mvp-workflow.md)
- [Traditional Chinese Product Vision and MVP Workflow](./doc/zh-TW/01-overview/04-product-vision-and-mvp-workflow.md)
- [Contributing](./CONTRIBUTING.md)
- [Coding Agent Guidelines](./AGENTS.md)
- [Documentation Guidelines](./doc/documentation-guidelines.md)
- [Development Guidelines](./doc/en/development-guidelines.md)
- [AI and OpenAPI Development Workflow](./doc/en/ai-openapi-development-workflow.md)
- [Canonical OpenAPI Contract](./contract/openapi/khaikang.v1.yaml)
- [English Architecture Overview](./doc/en/architecture/overview.md)
- [Traditional Chinese Documentation Map](./doc/zh-TW/01-overview/01-documentation-map.md)
- [Traditional Chinese Architecture Overview](./doc/zh-TW/01-overview/02-architecture-overview.md)
- [Current Planning](./doc/zh-TW/04-planning/01-phase-overview.md)

## License

Licensed under the [Apache License 2.0](./LICENSE).

Copyright 2026 KhaiKang Contributors.

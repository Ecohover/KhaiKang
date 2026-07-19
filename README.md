# KhaiKang

KhaiKang is an open-source, self-hostable development and test management platform for individual developers and small-to-medium software teams.

It connects requirements, delivery planning, implementation, test management, quality gates, and CI/CD feedback while continuing to use specialized external tools such as Jenkins, GitHub Actions, Playwright, and other test runners.

KhaiKang is Kanban-first and Scrum-compatible. AI can assist at every workflow stage, but projects may require human review before work is allowed to continue.

> AI proposes, humans approve when required, automation executes, and KhaiKang tracks.

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

Prerequisite: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile NuGet.config
dotnet run --project backend/src/KhaiKang.Api/KhaiKang.Api.csproj
```

The development profile listens on `http://localhost:5220`:

- Health: `GET /health/live`
- System information: `GET /api/v1/system/info`

Run backend verification with:

```shell
dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1
dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1
```

The frontend and Docker Compose workspaces have not been scaffolded yet.

## Documentation

- [Traditional Chinese README](./README.zh-TW.md)
- [Product Vision and MVP Workflow](./doc/en/product-vision-and-mvp-workflow.md)
- [Traditional Chinese Product Vision and MVP Workflow](./doc/zh-TW/01-overview/04-product-vision-and-mvp-workflow.md)
- [Contributing](./CONTRIBUTING.md)
- [Coding Agent Guidelines](./AGENTS.md)
- [Documentation Guidelines](./doc/documentation-guidelines.md)
- [Development Guidelines](./doc/en/development-guidelines.md)
- [English Architecture Overview](./doc/en/architecture/overview.md)
- [Traditional Chinese Documentation Map](./doc/zh-TW/01-overview/01-documentation-map.md)
- [Traditional Chinese Architecture Overview](./doc/zh-TW/01-overview/02-architecture-overview.md)
- [Current Planning](./doc/zh-TW/04-planning/01-phase-overview.md)

## License

Licensed under the [Apache License 2.0](./LICENSE).

Copyright 2026 KhaiKang Contributors.

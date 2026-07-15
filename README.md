# KhaiKang

KhaiKang is an open-source, self-hostable workflow platform for engineering teams.

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
- [Contributing](./CONTRIBUTING.md)
- [Coding Agent Guidelines](./AGENTS.md)
- [Documentation Guidelines](./doc/documentation-guidelines.md)
- [Development Guidelines](./doc/en/development-guidelines.md)
- [English Architecture Overview](./doc/en/architecture/overview.md)
- [Traditional Chinese Documentation Map](./doc/zh-TW/01-overview/01-documentation-map.md)
- [Traditional Chinese Architecture Overview](./doc/zh-TW/01-overview/02-architecture-overview.md)
- [Current Planning](./doc/zh-TW/04-planning/01-phase-overview.md)

## License

Licensed under the Apache License 2.0.

Copyright 2026 KhaiKang Contributors.

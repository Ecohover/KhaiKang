# Architecture Overview

## Product Direction

KhaiKang is an open-source, self-hostable workflow platform for engineering
teams. It is designed as a modular monolith so small installations remain easy
to operate while feature boundaries stay explicit.

## Technology Stack

- Frontend: Vue 3 and TypeScript
- Backend: ASP.NET Core on .NET 10
- Database: PostgreSQL
- Cache: Redis
- Message broker: RabbitMQ
- Local deployment: Docker Compose

PostgreSQL is the system of record. Redis and RabbitMQ are supporting
infrastructure and must not become mandatory for a feature that does not need
them.

## Runtime Shape

- `KhaiKang.Api` hosts HTTP endpoints and composes feature modules.
- `KhaiKang.Worker` hosts background jobs and asynchronous consumers.
- Feature modules run in-process and are referenced as .NET projects.
- Modules share deployment but retain ownership of their business behavior and
  persistence configuration.

Multiple projects do not imply microservices. A new network service requires an
explicit architecture decision and an operational reason.

## Repository Layout

```text
doc/
  en/
  zh-TW/
deploy/
  docker/
  compose/
  scripts/
src/
  backend/
    common/
    modules/
    KhaiKang.Api/
    KhaiKang.Worker/
  frontend/
  contract/
```

- `doc/en/`: public English architecture and engineering guidance
- `doc/zh-TW/`: current product specs, data models, planning, and translations
- `deploy/`: Docker Compose, scripts, and environment examples
- `src/backend/common/`: stable, domain-neutral backend components
- `src/backend/modules/`: feature-oriented modular-monolith projects
- `src/backend/KhaiKang.Api/`: HTTP host and composition root
- `src/backend/KhaiKang.Worker/`: background-processing host
- `src/frontend/`: Vue 3 frontend application
- `src/contract/`: API contract support and generated-model inputs

Detailed module, API, persistence, and testing rules are defined in
`doc/en/development-guidelines.md`.

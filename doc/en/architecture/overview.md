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
backend/
  KhaiKang.Backend.slnx
  src/
    common/
    modules/
    KhaiKang.Api/
    KhaiKang.Worker/
  tests/
    integration/
frontend/
contract/
tests/                 # Created when cross-component E2E tests exist
```

- `doc/en/`: public English architecture and engineering guidance
- `doc/zh-TW/`: current product specs, data models, planning, and translations
- `deploy/`: Docker Compose, scripts, and environment examples
- `backend/`: the .NET workspace, solution, source, and backend tests
- `backend/src/common/`: stable, domain-neutral backend components
- `backend/src/modules/`: feature-oriented modular-monolith projects
- `backend/src/KhaiKang.Api/`: HTTP host and composition root
- `backend/src/KhaiKang.Worker/`: background-processing host
- `backend/tests/`: backend unit and integration tests
- `frontend/`: Vue 3 frontend application
- `contract/`: API contract support and generated-model inputs
- root `tests/`: added only for cross-component or deployment-level E2E tests

Detailed module, API, persistence, and testing rules are defined in
`doc/en/development-guidelines.md`.

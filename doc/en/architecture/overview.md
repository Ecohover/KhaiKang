# Architecture Overview

## Product Direction

KhaiKang is designed as a modular monolith.
It keeps deployment simple for self-hosted environments while preserving clear internal module boundaries.

## Tech Stack

- Frontend: Vue 3 + TypeScript
- Backend: C# + ASP.NET Core on .NET 10
- Database: PostgreSQL
- Cache: Redis
- Message Broker: RabbitMQ
- Deployment: Docker Compose

## Repository Layout

```text
doc/
  en/
    architecture/
    roadmap/
    specs/
  zh-TW/
    architecture/
    roadmap/
    specs/
  internal/
deploy/
  docker/
  compose/
  scripts/
src/
  contracts/
  backend/
  frontend/
  worker/
```

- `doc/en/`: English product docs, architecture notes, roadmap, and specs
- `doc/zh-TW/`: Traditional Chinese product docs, architecture notes, roadmap, and specs
- `doc/internal/`: internal strategy and comparison notes
- `deploy/`: Docker Compose, scripts, and environment examples
- `src/contracts/`: API, DTO, enum, error, and event contracts
- `src/backend/`: ASP.NET Core backend application and modules
- `src/frontend/`: Vue 3 frontend application
- `src/worker/`: background jobs, queue consumers, and async processing

# API Contract Context

Read this file only when changing `contract/`, HTTP endpoints, or public API
behavior.

## Required References

- Read the relevant specification under `doc/zh-TW/02-specs/`.
- Read `doc/en/development-guidelines.md` for general engineering policy.
- Read `.ai/backend.md` as well when backend API code is modified.

## Contract Rules

- Use resource-oriented routes under `/api/v1`, standard HTTP methods, and
  meaningful status codes.
- Successful responses return resources or explicit operation DTOs.
- Errors use ASP.NET Core `ProblemDetails` or `ValidationProblemDetails`.
- Keep trace and correlation identifiers in observability data, not in the
  public error contract.
- Empty collection queries return `200` with an empty collection or page.
- `contract/openapi/khaikang.v1.yaml` is the HTTP wire-contract source of truth.
- Route, status, request, response, nullability, and enum changes are
  compatibility-sensitive.
- Isolate external wire formats behind adapters.
- C# and TypeScript contract implementations are maintained source code, not
  disposable generated output. Read and preserve compatible custom behavior.
- Update OpenAPI first, then synchronize backend DTOs/endpoints, TypeScript
  types/client methods, and tests.
- Report conflicts among specs, OpenAPI, and code instead of resolving them
  silently.

## AI Synchronization Check

- Map every changed `operationId` to its backend endpoint and frontend client.
- Compare routes, methods, status codes, required and nullable properties,
  enums, security, CSRF, and Problem Details codes.
- Run the applicable commands from `.ai/verification.md`; AI review alone is
  not contract verification.

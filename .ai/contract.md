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
- OpenAPI is the HTTP wire-contract source for generated frontend models.
- Route, status, request, response, nullability, and enum changes are
  compatibility-sensitive.
- Isolate external wire formats behind adapters.
- Do not manually edit generated API models. Change the source contract and
  regenerate them with the documented command.

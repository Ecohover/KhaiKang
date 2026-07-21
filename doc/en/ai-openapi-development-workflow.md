# AI and OpenAPI Development Workflow

## Decision

KhaiKang uses an **OpenAPI-first, AI-implemented, compiler-verified** workflow.

- `contract/openapi/khaikang.v1.yaml` is the single source of truth for the
  public HTTP wire contract.
- C# DTOs and endpoints and TypeScript DTOs and HTTP clients are maintained
  source code.
- NSwag, OpenAPI Generator, or another code generator is not required.
- AI synchronizes both implementations after reading existing code, while
  compilers, tests, and review remain the verification gates.

This keeps authentication, CSRF, error handling, module boundaries, and UI
behavior customizable without generator templates or overwritten patches.

## Responsibility of each source

1. Feature specifications define use cases, business rules, security, and
   acceptance criteria.
2. OpenAPI defines routes, operation IDs, methods, request and response shapes,
   status codes, nullability, enums, security requirements, and examples.
3. C# and TypeScript implement that contract within their architecture.
4. Migrations define database evolution; entities and tables are not generated
   from OpenAPI.

AI must report conflicts among these sources rather than silently selecting
one.

## Workflow for an API change

1. Resolve requirements, failure scenarios, and acceptance criteria with AI.
2. Update the feature specification and canonical OpenAPI document first.
3. Review affected operation IDs, schemas, and compatibility.
4. Have AI update the owning C# module, TypeScript HTTP client, UI, and tests
   after inspecting their existing custom behavior.
5. Preserve custom behavior that remains compatible with the new contract;
   maintained files are not disposable generated output.
6. Run backend build/tests, frontend type-check/tests/build, and relevant HTTP
   contract integration tests.
7. Report the mapping from changed OpenAPI operations to backend and frontend
   implementations.

## Code locations

```text
contract/openapi/                 Canonical HTTP wire contract
backend/src/modules/<Feature>/
  Contracts/                      Feature-owned C# HTTP DTOs
  Endpoints/                      HTTP adapters
frontend/apps/web/src/api/
  contracts.ts                    TypeScript wire types
  client.ts                       Project-specific HTTP client
```

Host-specific DTOs may remain in their host. The `contract/` directory does
not contain a language-specific implementation project.

## Reconsidering generation

Reconsider a generator only when API volume, external SDKs, cross-repository
consumers, or parallel contributors make synchronization a measured recurring
problem. Any adoption must document template fit, version pinning, licensing,
upgrade policy, and generated-code review.

# API Contract

`openapi/khaikang.v1.yaml` is the source of truth for KhaiKang's public HTTP
wire contract.

KhaiKang uses an OpenAPI-first, AI-implemented, compiler-verified workflow:

1. Update the feature specification and OpenAPI contract together.
2. Review routes, operation IDs, status codes, schemas, nullability, security,
   and examples before implementation.
3. Use AI to update the backend DTOs and endpoints and the TypeScript HTTP
   client while preserving project-specific behavior.
4. Verify the result with backend build/tests, frontend type-check/tests, and
   contract-focused integration tests.

The repository intentionally does not require a code generator. C# and
TypeScript implementations are normal maintained source code, not disposable
generated output. Do not edit one side of a public contract without reviewing
the OpenAPI document and the other implementation.

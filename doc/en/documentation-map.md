# Documentation Map

Status: synchronized with the Traditional Chinese MVP documentation on 2026-08-10.

## Purpose

This page is the English entry point for KhaiKang documentation. Current MVP product behavior is documented in paired English and Traditional Chinese files. Historical implementation notes remain in Traditional Chinese unless they are listed below as current release guidance.

## Recommended Reading Order

1. [Architecture overview](./architecture/overview.md)
2. [Development guidelines](./development-guidelines.md)
3. [.NET development guidelines](./dotnet-development-guidelines.md)
4. [Product vision and MVP workflow](./product-vision-and-mvp-workflow.md)
5. [Domain glossary](./domain-glossary.md)
6. [UI guidelines](./ui-guidelines.md)
7. [Functional spec index](./specs/spec-index.md)
8. [Data-model overview](./data-model/overview.md)
9. [MVP release closure plan](./planning/mvp-release-closure-plan.md)

## Functional Specifications

- [User and authentication](./specs/user-authentication.md)
- [Project management](./specs/project-management.md)
- [Test management](./specs/test-management.md)
- [Issue-Test Traceability](./specs/issue-test-traceability.md)

## Data Models

- [Overview](./data-model/overview.md)
- [Authentication](./data-model/authentication.md)
- [Project management](./data-model/project-management.md)
- [Test management](./data-model/test-management.md)
- [Issue-Test Traceability](./data-model/issue-test-traceability.md)
- [Audit metadata](./data-model/audit-metadata-fields.md)

## Current Planning

- [Phase overview](./planning/phase-overview.md)
- [MVP release closure](./planning/mvp-release-closure-plan.md)
- [Release automation and delivery drill](./planning/release-automation.md)
- [Cross-module attachments](./planning/cross-module-attachments.md)
- [Backlog](./planning/backlog.md)

The canonical HTTP wire contract is [`contract/openapi/khaikang.v1.yaml`](../../contract/openapi/khaikang.v1.yaml). Planning documents describe delivery state; they do not override functional specifications, data models, OpenAPI, or accepted tests.

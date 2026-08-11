# Phase Overview

Status: synchronized on 2026-08-11. Traditional Chinese counterpart: [階段規劃總覽](../../zh-TW/04-planning/01-phase-overview.md).

The current phase is stable `v0.1.0` promotion. Identity, Project/Issue, manual Test Management, Workspace–Project links, attachments, the baseline migrations, shared versioning, and RC image publication are implemented. Immutable `sha-0fd82c0` API/Web images passed fresh-volume startup, the complete MVP smoke flow, attachment hash checks, Test Run snapshot checks, and restart persistence.

Current priorities are stable release notes, version consistency, and the reviewed `rc` to `main` promotion. Backup/restore automation remains required before hosting real user data but is explicitly deferred from this MVP promotion. Full desktop/mobile acceptance, Playwright, automation import, AI execution, and custom roles/workflows/fields remain later phases.

The [Issue-Test Traceability specification](../specs/issue-test-traceability.md) and [data model](../data-model/issue-test-traceability.md) are implemented. OpenAPI, one final migration per affected module context, backend integration tests, frontend tests/build, fresh PostgreSQL startup, and the isolated Docker MVP smoke flow have passed.

See [Release automation and delivery drill](./release-automation.md) for the active work, [MVP release closure](./mvp-release-closure-plan.md) for the accepted MVP boundary, and [Backlog](./backlog.md) for deferred work.

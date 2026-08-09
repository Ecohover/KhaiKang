# MVP Release Closure Plan

Status: core implementation and baseline verification complete as of 2026-08-09. Traditional Chinese counterpart: [MVP 收尾與發布準備計畫](../../zh-TW/04-planning/06-mvp-release-closure-plan.md).

## Included MVP

- Local accounts, browser authentication, fixed roles, permissions, and first-run setup.
- Projects, members, Issues, list/board workflow, assignment, completion information, and Issue attachments.
- Test Workspaces, Project links, members, Suites, Cases, Tags, Plans, Runs, manual results, immutable snapshots, and Case/Run Item attachments.
- Local `IFileStorage`, PostgreSQL metadata, canonical OpenAPI, English/Traditional Chinese UI copy, and optimistic concurrency.

## Explicitly Deferred

- CI/Jenkins, Playwright, external result import, AI generation/execution, repositories, environments, execution agents, and credentials.
- Custom roles, workflows, fields, Issue tags/sub-tasks/comments/relations/watchers/notifications.
- Advanced attachment providers, virus scanning, previews, sharing, versioning, and retention cleanup.
- Full desktop/mobile manual acceptance, production upgrade rehearsal, and backup/restore rehearsal.

## Completed Closure Work

- Test Run execution locks snapshot scope and structure but allows any authorized Workspace executor to contribute results.
- The three DbContexts each have one baseline migration.
- A clean `khaikang` PostgreSQL test database applied all three baselines successfully and created 34 public tables including `__EFMigrationsHistory`.
- Backend build completed with zero warnings/errors and 21 automated tests passed.
- A local attachment-storage probe remained readable after an API restart.

## Remaining Before Publishing a Usable MVP Build

- Perform one short smoke pass for setup/login, Project/Issue, Workspace/Case/Plan/Run, and attachment upload/download.
- Review and commit the baseline migration replacement and synchronized documentation.
- Production Docker-volume, upgrade-path, and backup/restore verification remain release-hardening work, not a blocker for the local MVP milestone.

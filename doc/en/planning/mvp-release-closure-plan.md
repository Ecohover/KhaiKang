# MVP Release Closure Plan

Status: core implementation and baseline verification complete as of 2026-08-09. Traditional Chinese counterpart: [MVP 收尾與發布準備計畫](../../zh-TW/04-planning/06-mvp-release-closure-plan.md).

## Included MVP

- Local accounts, browser authentication, fixed roles, permissions, and first-run setup.
- Projects, members, Issues, list/board workflow, assignment, completion information, and Issue attachments.
- Test Workspaces, Project links, members, Suites, Cases, Tags, Plans, Runs, manual results, immutable snapshots, and Case/Run Item attachments.
- Fixed Issue relations plus Case requirement, Plan Test Issue, Run snapshot, and Run-originated Bug traceability.
- Local `IFileStorage`, PostgreSQL metadata, canonical OpenAPI, English/Traditional Chinese UI copy, and optimistic concurrency.

## Explicitly Deferred

- CI/Jenkins, Playwright, external result import, AI generation/execution, repositories, environments, execution agents, and credentials.
- Custom roles, workflows, fields, Issue tags/sub-tasks/comments/watchers/notifications, and configurable relation types.
- Advanced attachment providers, virus scanning, previews, sharing, versioning, and retention cleanup.
- Full desktop/mobile manual acceptance, production upgrade rehearsal, and backup/restore rehearsal.

## Completed Closure Work

- Test Run execution locks snapshot scope and structure but allows any authorized Workspace executor to contribute results.
- The three DbContexts each have one baseline migration.
- A clean `khaikang` PostgreSQL test database applied all three baselines successfully and created 34 public tables including `__EFMigrationsHistory`.
- Backend build completed with zero warnings/errors and 23 automated tests passed.
- A local attachment-storage probe remained readable after an API restart.

## Current MVP Status

- Core implementation, baseline migration replacement, synchronized documentation, automated checks, and the basic local restart-persistence check are complete and committed.
- A short setup/login, Project/Issue, Workspace/Case/Plan/Run, and attachment smoke pass is sufficient when producing a new usable build; full desktop/mobile acceptance is deferred.
- Production Docker-volume, upgrade-path, and backup/restore verification remain release-hardening work, not a blocker for the local MVP milestone.
- Release automation and the isolated backup/restore drill are now tracked in the [Release Automation and Delivery Drill Plan](./release-automation.md).

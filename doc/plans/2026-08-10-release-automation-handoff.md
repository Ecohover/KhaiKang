# Release Automation and Delivery Drill Handoff

## Objective

Implement the accepted release-preparation, guarded publication, pull-request
verification, and isolated backup/restore drill defined in the formal release
automation plan.

Out of scope for this handoff:

- performing a real release without a new explicit user request;
- automatically merging pull requests;
- deploying to a production host;
- modifying or deleting existing developer databases, containers, or volumes.

## Accepted Decisions

- Human gates remain for version selection, release-note review, PR merge, and
  final publication authorization.
- Verification, packaging, Git tagging, GitHub Release creation, Docker image
  publication, and digest evidence should be automated.
- `rc` remains the release-candidate integration branch; `main` remains stable.
- `VERSION` remains the current product-version source of truth.
- Pull-request verification must not receive Docker Hub secrets or publish images.
- Delivery and restore drills must use uniquely named disposable resources.

## Current State

- Working branch: `ecohover/chore/deployment-readiness-smoke`.
- Base commit: `eaebcc8` on `rc`.
- Current release: `v0.1.0-rc.2`.
- The existing `.github/workflows/docker-publish.yml` publishes `rc`, `latest`,
  semantic-version, and SHA tags according to the current branch/tag rules.
- Compose now has an API migration-aware healthcheck, an end-to-end Web/API
  healthcheck, and a one-shot non-root volume ownership initializer.
- `deploy/Test-MvpSmoke.ps1` covers Project/Issue, multi-Project Workspace links,
  Case/Plan/Run, all attachment scopes, Run snapshots, result completion, and
  optional restart persistence.
- Preparation, publication, backup, and restore scripts are not implemented and
  are explicitly deferred by the current MVP delivery decision.
- No real release is authorized by this handoff.

## Verification

- `docker compose config --quiet` passed for the updated Compose definition.
- Published immutable RC2 images reached healthy state from fresh PostgreSQL,
  attachment, and data-protection volumes without a host `chown`.
- `deploy/Test-MvpSmoke.ps1` passed the full MVP flow and restarted PostgreSQL,
  API, and Web before rechecking records, snapshots, and attachment hashes.
- The updated API Dockerfile built as
  `khaikang-api:deployment-readiness-smoke`; a fresh mounted attachment volume
  was owned by and writable as UID 1654.
- Run the repository verification and documentation checks again after the final
  documentation edits, following [`.ai/verification.md`](../../.ai/verification.md).

## Remaining Work

1. Finish repository verification and open a reviewed PR to `rc` when authorized.
2. Publish the next immutable RC image only after explicit authorization.
3. Rerun `deploy/Test-MvpSmoke.ps1` against that immutable image and record its
   tag, digests, Compose project name, and generated record IDs.
4. Before stable release or real user data, add backup/restore scripts, a
   release-set manifest, and an isolated restore drill.
5. Add `Prepare-Release.ps1` and guarded `Publish-Release.ps1` when manual owner
   gates are no longer sufficient.
6. Commit, push, tag, or publish only when the user explicitly requests it.

## Context To Load

- [Repository guidelines](../../AGENTS.md)
- [AI context index](../../.ai/INDEX.md)
- [Release AI context](../../.ai/release.md)
- [Verification context](../../.ai/verification.md)
- [English release automation plan](../en/planning/release-automation.md)
- [Traditional Chinese release automation plan](../zh-TW/04-planning/08-release-automation-plan.md)
- [Existing Docker publication workflow](../../.github/workflows/docker-publish.yml)
- [Deployment documentation](../../deploy/README.md)

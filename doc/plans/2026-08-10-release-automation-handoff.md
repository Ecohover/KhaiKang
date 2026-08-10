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

- Working branch: `ecohover/chore/rc2-release-drill`.
- Base commit: `32eb196 fix: stabilize rc release verification`.
- Current release: `v0.1.0-rc.2`.
- The existing `.github/workflows/docker-publish.yml` publishes `rc`, `latest`,
  semantic-version, and SHA tags according to the current branch/tag rules.
- The formal English and Traditional Chinese plans and `.ai/release.md` routing
  context have been drafted in the working tree.
- Preparation, publication, backup, and restore scripts are not implemented yet.
- No real release is authorized by this handoff.

## Verification

- Run `git diff --check` after document changes.
- Check all relative links in the files listed under **Context To Load**.
- Before reporting implementation complete, follow [`.ai/verification.md`](../../.ai/verification.md)
  and record the actual command results here.

## Remaining Work

1. Remove non-historical hard-coded current-version examples.
2. Add a release-note template and `deploy/Prepare-Release.ps1`.
3. Add PR verification that cannot publish images.
4. Add guarded `deploy/Publish-Release.ps1` with validation-only mode.
5. Add backup/restore scripts and a release-set manifest.
6. Run the isolated `v0.1.0-rc.2` deployment, restart, backup, and restore drill.
7. Update both formal plans and this handoff with command output and remaining risks.
8. Commit or publish only when the user explicitly requests it.

## Context To Load

- [Repository guidelines](../../AGENTS.md)
- [AI context index](../../.ai/INDEX.md)
- [Release AI context](../../.ai/release.md)
- [Verification context](../../.ai/verification.md)
- [English release automation plan](../en/planning/release-automation.md)
- [Traditional Chinese release automation plan](../zh-TW/04-planning/08-release-automation-plan.md)
- [Existing Docker publication workflow](../../.github/workflows/docker-publish.yml)
- [Deployment documentation](../../deploy/README.md)

# Release Automation and Delivery Drill Plan

Status: accepted implementation plan as of 2026-08-10; automation and the
isolated delivery drill are not yet complete. Traditional Chinese counterpart:
[發布自動化與交付演練計畫](../../zh-TW/04-planning/08-release-automation-plan.md).

## Purpose

Define one repeatable path from an accepted change to a Git tag, GitHub Release,
and immutable API/Web Docker images. Keep release decisions human-controlled
while automating verification, packaging, publication, and evidence collection.

This document is the source of truth for release-process implementation. It
does not authorize an agent or script to perform a real release by itself.

## Outcomes

- A contributor can prepare an RC or stable version without manually editing
  version references in multiple files.
- Pull requests are verified without publishing images.
- An accepted `rc` or `main` commit can be published with one guarded command.
- A release records its source commit, Git tag, GitHub Actions run, image tags,
  and image digests.
- Published images can be tested with fresh PostgreSQL and attachment volumes,
  then backed up and restored into an isolated environment.

## Non-goals

- Automatically merging pull requests.
- Publishing on every feature-branch push.
- Continuous deployment to a production host.
- Historical security-maintenance branches.
- Object storage, image signing, vulnerability-policy enforcement, or SBOM
  policy beyond metadata already produced by the image build.

## Branch and Version Model

| Source | Purpose | Allowed version | Published tags |
| --- | --- | --- | --- |
| `ecohover/feature/*`, `ecohover/chore/*` | Isolated development | Inherits the current base until release preparation | None |
| `rc` | Accepted release-candidate integration | Prerelease or stable candidate | `rc`, `sha-<commit>` |
| `main` | Stable releases | Stable semantic version | `latest`, `sha-<commit>` |
| Git tag `vX.Y.Z-rc.N` | Immutable release candidate | Exact matching prerelease | `X.Y.Z-rc.N`, `sha-<commit>` |
| Git tag `vX.Y.Z` | Immutable stable release | Exact matching stable version | `X.Y.Z`, `latest`, `sha-<commit>` |

`VERSION` is the only current product-version source of truth. Historical
release notes retain their original version numbers. Documentation examples
should prefer placeholders or `rc` unless an immutable historical release is
being described.

## Human Gates

The project owner must perform or explicitly approve these decisions:

1. Choose the next semantic version.
2. Review and complete the release notes.
3. Review and merge the pull request into `rc`, or from `rc` into `main` for a
   stable release.
4. Explicitly invoke or authorize the publish command after required checks
   pass.

No human should need to run Docker build/tag/push commands, manually create a
Git tag, or manually calculate image digests during the normal path.

## Planned Automation

### `deploy/Prepare-Release.ps1`

The preparation command will:

- require a clean feature/chore branch based on the intended target;
- validate the requested semantic version and release channel;
- update `VERSION`;
- create `doc/releases/v<version>.md` from a template without overwriting an
  existing note;
- report the required target branch and next commands;
- never commit, push, merge, tag, or publish.

### Pull-request verification

A verification workflow will run for pull requests targeting `rc` or `main`:

- restore, Release build, and backend unit/integration tests;
- frontend frozen install, type check, tests, and production build;
- semantic-version and release-note consistency checks when `VERSION` changes;
- workflow and documentation checks that can run without publication secrets.

The verification workflow must not log in to Docker Hub or publish images.

### Existing Docker publication workflow

The Docker workflow remains the remote packager:

- a push to `rc` publishes `rc` and `sha-<commit>`;
- a push to `main` publishes `latest` and `sha-<commit>`;
- an exact Git tag publishes the matching immutable semantic-version tag;
- backend and frontend verification must pass before either image is pushed.

### `deploy/Publish-Release.ps1`

The guarded publish command will:

- require `rc` for a prerelease or `main` for a stable release;
- require a clean worktree whose `HEAD` equals the corresponding remote branch;
- require an exact `VERSION`, matching release-note file, and absent local and
  remote Git tag;
- require the branch Docker workflow for the same commit to have succeeded;
- show the version, branch, commit, and target image tags before confirmation;
- create and push an annotated Git tag only after confirmation;
- wait for the tag workflow and fail if it does not succeed;
- create the matching GitHub prerelease or stable release;
- verify the API and Web immutable tags and record their digests;
- never move an existing tag or replace an existing GitHub Release.

An optional validation-only mode must perform every preflight check without
creating or changing remote state.

## Release Candidate Flow

1. Develop and verify on an `ecohover/feature/*` or `ecohover/chore/*` branch.
2. Run `Prepare-Release.ps1 -Version X.Y.Z-rc.N`.
3. Complete the generated release note, commit, push, and open a PR to `rc`.
4. Review the PR and wait for verification checks.
5. Merge into `rc`; wait for the `rc` image workflow.
6. Perform the required acceptance or delivery drill.
7. Run `Publish-Release.ps1`; it publishes the immutable tag and prerelease.

## Stable Release Flow

1. Prepare the stable `X.Y.Z` version through a reviewed change on `rc`.
2. Complete RC acceptance and merge `rc` into `main`.
3. Wait for the `main` workflow to publish `latest` from that exact commit.
4. Run `Publish-Release.ps1` on `main` to create `vX.Y.Z`, the immutable image
   tags, and the stable GitHub Release.

## Isolated Delivery and Restore Drill

The first implementation must be exercised without touching existing developer
data:

1. Use a unique Compose project name and new PostgreSQL, attachment, and data
   protection volumes.
2. Start the published immutable RC images, not local source builds.
3. Verify health, initial setup/login, Project/Issue, Workspace/Case/Plan/Run,
   and attachment upload/download.
4. Restart all application containers and confirm database and attachment
   persistence.
5. Back up PostgreSQL and the attachment volume as one release set with a
   manifest containing version, timestamp, and source image tags.
6. Restore into a second unique set of resources.
7. Confirm the restored records, Test Run snapshots, original attachment names,
   and downloadable file content.
8. Remove only the explicitly named disposable drill resources after their
   resolved names are reviewed.

## Acceptance Criteria

- An RC can be prepared without scattered manual version edits.
- PR verification does not require publication secrets and cannot publish.
- Validation-only publication reports all intended remote changes without
  performing them.
- A real authorized RC publication creates one immutable Git tag, one GitHub
  prerelease, and exact API/Web image tags from the same commit.
- Re-running publication for an existing version fails without changing it.
- A fresh-image deployment, restart-persistence check, backup, and isolated
  restore all succeed with recorded evidence.
- The implementation and actual drill results are synchronized in both
  language versions of this plan.

## Implementation Order

1. Remove non-historical hard-coded current-version examples.
2. Add release-note template and preparation validation.
3. Add publication preflight and validation-only mode.
4. Separate PR verification from Docker publication.
5. Add backup/restore scripts and manifest format.
6. Run the isolated RC2 drill and record evidence.
7. Perform an authorized dry release only when requested by the project owner.

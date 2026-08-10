# Release and Delivery Context

Use this context for release preparation, Git tags, GitHub Releases, Docker
image publishing, and deployment backup/restore drills.

## Authoritative Documents

- Read [`doc/en/planning/release-automation.md`](../doc/en/planning/release-automation.md)
  before changing release automation.
- The synchronized Traditional Chinese plan is
  [`doc/zh-TW/04-planning/08-release-automation-plan.md`](../doc/zh-TW/04-planning/08-release-automation-plan.md).
- Read [`.ai/verification.md`](./verification.md) before claiming that a release
  or drill is complete.

## Required Boundaries

- Treat `VERSION` as the product-version source of truth.
- Keep `rc` as the release-candidate integration branch and `main` as the
  stable branch.
- Never overwrite or move an existing Git tag.
- Never merge a pull request, push a tag, create a GitHub Release, publish an
  image, or mutate deployment data unless the user explicitly authorizes that
  action in the current request.
- Implementing or testing release automation is not authorization to perform a
  real release.
- Use isolated container names, Compose project names, databases, and volumes
  for drills. Do not delete or reset a developer's existing data.
- Do not print, commit, or copy Docker Hub tokens, database passwords, or other
  secrets into logs or documentation.

## Completion Evidence

Report the exact branch and commit, verification commands, GitHub Actions run,
published tags and digests, drill resources used, and anything not verified.

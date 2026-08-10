# Changelog

All notable changes are documented in GitHub Releases and pull requests. This
file follows the spirit of [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

## 0.1.0 - 2026-08-10

### Added

- A repository-wide `VERSION` source of truth for backend assemblies, frontend
  display, Docker labels, and release image tags.
- A simple `rc` to `main` publishing flow with matching semantic Git tags such
  as `v0.1.0-rc.1`.
- Docker Hub publishing from verified commits merged into `main`.
- Docker Compose deployment documentation and optional Caddy HTTPS example.
- Project and Issue management with memberships, fixed roles, status workflows,
  Markdown content, and evidence attachments.
- Manual Test Management with Workspaces, Suites, Cases, Plans, Runs, stable
  execution snapshots, result recording, and evidence attachments.
- An executable fresh-volume MVP smoke test with restart-persistence checks.

### Fixed

- Local HTTP Docker deployments can opt out of secure cookies while HTTPS
  deployments retain secure cookie requirements.
- Docker Compose now waits for PostgreSQL, API migrations, and the Web-to-API
  route before reporting the stack ready.
- Compose-managed attachment and data-protection volumes are initialized for
  the API's non-root user without host-side ownership commands.

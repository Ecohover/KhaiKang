# Changelog

All notable changes are documented in GitHub Releases and pull requests. This
file follows the spirit of [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

### Added

- A repository-wide `VERSION` source of truth for backend assemblies, frontend
  display, Docker labels, and release image tags.
- A simple `rc` to `main` publishing flow with matching semantic Git tags such
  as `v0.1.0-rc.1`.
- Docker Hub publishing from verified commits merged into `main`.
- Docker Compose deployment documentation and optional Caddy HTTPS example.

### Fixed

- Local HTTP Docker deployments can opt out of secure cookies while HTTPS
  deployments retain secure cookie requirements.

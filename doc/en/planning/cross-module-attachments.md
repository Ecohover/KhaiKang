# Cross-Module Attachments

Status: MVP implementation complete; local API-restart persistence check complete on 2026-08-09. Traditional Chinese counterpart: [跨模組附件實作計畫](../../zh-TW/04-planning/07-cross-module-attachments-implementation-plan.md).

## Scope

Attachments are implemented for existing Project Issues, Test Cases, and Test Run Items. Run Item attachments are evidence for one executed Case, not files attached to the whole Run or an individual Step.

## Storage

- Feature modules call the domain-neutral `IFileStorage` abstraction.
- The MVP provider is `LocalFileStorage`; deployments mount `/var/lib/khaikang/attachments` as a named volume.
- PostgreSQL stores metadata, an opaque UUID-based `storage_key`, content type, size, hash, uploader, soft-delete state, and audit metadata.
- Original names are used only for display and safe `Content-Disposition` download names.
- Database soft delete does not immediately remove bytes. Orphan and retention cleanup is deferred.

## Authorization and Lifecycle

- Issue attachment access follows the parent Project and Issue permissions.
- Active Workspace members may read Case attachments; Owner/Manager may upload or delete them.
- Authorized executors may upload or delete Run Item evidence only while the Run is `in_progress`.
- Completed Runs are fully read-only.
- Download routes remain nested under their parent resource, preventing a global attachment-ID bypass.

## Validation

Empty and oversized files, invalid names/paths, missing parents, inactive parent resources, unauthorized access, and cross-Project/Workspace requests are rejected. The configured maximum defaults to 20 MiB. Downloads set `X-Content-Type-Options: nosniff`.

## Deferred

S3-compatible providers, external sharing, version history, thumbnails, virus scanning, retention cleanup, and production backup/restore exercises are post-MVP work.

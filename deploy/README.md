# KhaiKang Docker deployment

This folder contains the reproducible deployment definition for the Docker Hub
images. The Dockerfiles are used by GitHub Actions; users normally start the
published images through `compose/docker-compose.yml`.

## Quick start

1. Install Docker Engine with Docker Compose v2.
2. Copy `compose/.env.example` to `compose/.env`.
3. Set a unique, long `POSTGRES_PASSWORD`. A safe portable choice is
   `openssl rand -hex 32`; do not use spaces or semicolons.
4. Start the services:

   ```sh
   cd deploy/compose
   docker compose pull
   docker compose up -d --wait --wait-timeout 180
   ```

5. Run `docker compose ps` and confirm PostgreSQL, API, and Web are healthy.
6. Open `http://localhost:8080` and initialize the first system administrator.

The same example is available in a Docker Hub-ready format at
[`docker-hub-overview.md`](./docker-hub-overview.md).

## Secrets and persistent data

- `compose/.env` is intentionally ignored by Git. Keep it only on the deployment host.
- `postgres-data` stores all application data. Back it up before an upgrade.
- `data-protection-keys` encrypts and validates authentication cookies. Keep it with
  the database backup; deleting it signs out every user and invalidates existing
  protected data.
- `attachments` stores Issue, Test Case, and Test Run files. Keep it with the
  database backup so attachment metadata and file content stay consistent.
- `KHAIKANG_REQUIRE_HTTPS` must remain `false` only for localhost or other trusted
  HTTP testing. Set it to `true` only after users reach KhaiKang through HTTPS;
  secure session, refresh, and CSRF cookies will then be required by the browser.
- The API applies EF Core migrations only because Compose sets
  `Database__ApplyMigrations=true`. Set it to `false` when operating migrations
  through a separate controlled process.
- For internet-facing deployment, use the included Caddy HTTPS override below.
  Do not expose PostgreSQL or the API service directly.

## Readiness and volume permissions

Compose waits for PostgreSQL first, then for the API migrations and
`/health/live`, and finally for the Web proxy to reach `/api/v1/system/info`.
Use `docker compose up -d --wait --wait-timeout 180` in deployment automation;
the command exits non-zero when the stack does not become ready.

The short-lived `storage-init` service mounts only the data-protection and
attachment volumes, sets their owner to the API image's non-root `APP_UID`, and
then exits. An `Exited (0)` state for this service is expected. The API itself
continues to run as the non-root application user. Deployment hosts do not need
manual `chown` commands for new or existing Compose-managed volumes.

Inspect readiness or initialization failures with:

```sh
docker compose ps
docker compose logs storage-init
docker compose logs api
```

## MVP smoke test

[`Test-MvpSmoke.ps1`](./Test-MvpSmoke.ps1) exercises the externally visible MVP
flow against a fresh disposable database:

- initial setup and administrator login;
- two Projects linked to one Workspace;
- Project Issue, Suite, Tag, Case, Plan, and Run creation;
- Issue, Case, and Run attachment upload/download with SHA-256 comparison;
- Test Run snapshot stability after the source Case changes;
- step and Case result recording and Run completion;
- optional PostgreSQL, API, and Web restart followed by persistence checks.

The script intentionally refuses a database that has already been initialized.
Use a unique Compose project name and port so existing developer data is not
touched:

```powershell
cd deploy/compose
$env:KHAIKANG_HTTP_PORT = "18082"
docker compose --project-name khaikang-mvp-smoke up -d --wait --wait-timeout 180
..\Test-MvpSmoke.ps1 `
  -BaseUrl http://localhost:18082 `
  -ExpectedVersion X.Y.Z-rc.N `
  -ComposeProjectName khaikang-mvp-smoke
```

After reviewing the exact project name and retaining any required evidence,
remove only that disposable stack with:

```powershell
docker compose --project-name khaikang-mvp-smoke down --volumes
```

## HTTPS with Caddy

The default Compose file is for localhost and trusted HTTP networks. For a
public domain, point its DNS A/AAAA record to the deployment host, allow inbound
TCP ports 80 and 443, then configure HTTPS:

```sh
cd deploy/compose
cp .env.example .env
```

Set these values in `.env` before starting the stack:

```dotenv
KHAIKANG_DOMAIN=khaikang.example.com
KHAIKANG_REQUIRE_HTTPS=true
# Keep the direct web port reachable only from the local host.
KHAIKANG_HTTP_PORT=127.0.0.1:8080
```

Start the base stack together with the Caddy override:

```sh
docker compose -f docker-compose.yml -f docker-compose.https.yml pull
docker compose -f docker-compose.yml -f docker-compose.https.yml up -d --wait --wait-timeout 180
```

Caddy obtains and renews the TLS certificate automatically. Never set
`KHAIKANG_REQUIRE_HTTPS=true` while serving the application directly over HTTP:
the browser will correctly refuse to send secure authentication and CSRF cookies.

## Upgrade and rollback

Set `KHAIKANG_IMAGE_TAG` in `.env` to the required published image tag, then run:

```sh
docker compose pull
docker compose up -d --wait --wait-timeout 180
```

Use an immutable release tag such as `0.1.0-rc.2` or the Git SHA tag published
with each build for a precise rollback. Do not deploy `rc` or `latest` when a
repeatable rollback is required.

## Versioning and image tags

`VERSION` at the repository root is the product version source of truth. It
uses semantic versioning without the Git `v` prefix. The first release candidate
is therefore stored as `0.1.0-rc.2` and tagged in Git as `v0.1.0-rc.2`.

The Docker workflow publishes these tags:

- a version tag such as `0.1.0-rc.2` when a matching Git tag is pushed;
- `sha-<commit>` for an immutable source reference;
- `rc` for builds from the `rc` acceptance branch;
- `latest` for builds from the stable `main` branch and stable version tags.

The initial project uses only two long-lived branches: `rc` for acceptance and
`main` for stable releases. Maintenance branches for older minor versions can
be introduced later when external users depend on more than one release line.
A formal release does not need another branch: merge the accepted RC into
`main`, change `VERSION` to the stable value, and create the version tag there.

The Git tag and `VERSION` must match. For example:

```sh
git tag -a v0.1.0-rc.2 -m "KhaiKang v0.1.0-rc.2"
git push origin v0.1.0-rc.2
```

On Windows, inspect or build the exact local image tags with:

```powershell
.\deploy\Build-Images.ps1 -PrintOnly
.\deploy\Build-Images.ps1
```

Add `-Push` only after signing in to the intended Docker registry. The script
refuses a dirty working tree by default so that an immutable SHA tag always
identifies the image source.

## Publishing images from GitHub Actions

The publish workflow runs for `rc`, `main`, and semantic Git tags. It
verifies the backend and frontend, builds the images, then logs in to Docker Hub
and pushes the images. Pull requests do not publish Docker images.

Repository maintainers must configure these GitHub Actions secrets:

| Secret | Value |
| --- | --- |
| `DOCKERHUB_USERNAME` | Docker Hub namespace that owns the `khaikang-api` and `khaikang-web` repositories. |
| `DOCKERHUB_TOKEN` | A Docker Hub access token with read and write permission. Do not use an account password. |

Create the token in Docker Hub under **Account Settings → Personal access tokens**,
then add it in GitHub under **Repository Settings → Secrets and variables → Actions**.
The token is used only by the publish workflow and is never added to an image or
deployment `.env` file.

## Docker Hub repository information

Docker Hub repository metadata is separate from an image tag. Use these short
descriptions, each within Docker Hub's 100-character limit:

- `khaikang-api`: `KhaiKang API for the self-hosted project and manual test management platform.`
- `khaikang-web`: `KhaiKang web UI for the self-hosted project and manual test management platform.`

Paste [`docker-hub-overview.md`](./docker-hub-overview.md) into the Overview of
both Docker Hub repositories. The GitHub Actions workflow pushes images directly,
so Docker Hub does not automatically synchronize this file as an automated build
would. Keep version-specific changes in GitHub Releases and keep the Docker Hub
Overview focused on installation, tag policy, documentation, and support links.

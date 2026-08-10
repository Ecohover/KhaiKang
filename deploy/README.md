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
   docker compose up -d
   ```

5. Open `http://localhost:8080` and initialize the first system administrator.

The same example is available in a Docker Hub-ready format at
[`docker-hub-overview.md`](./docker-hub-overview.md).

## Secrets and persistent data

- `compose/.env` is intentionally ignored by Git. Keep it only on the deployment host.
- `postgres-data` stores all application data. Back it up before an upgrade.
- `data-protection-keys` encrypts and validates authentication cookies. Keep it with
  the database backup; deleting it signs out every user and invalidates existing
  protected data.
- `KHAIKANG_REQUIRE_HTTPS` must remain `false` only for localhost or other trusted
  HTTP testing. Set it to `true` only after users reach KhaiKang through HTTPS;
  secure session, refresh, and CSRF cookies will then be required by the browser.
- The API applies EF Core migrations only because Compose sets
  `Database__ApplyMigrations=true`. Set it to `false` when operating migrations
  through a separate controlled process.
- For internet-facing deployment, use the included Caddy HTTPS override below.
  Do not expose PostgreSQL or the API service directly.

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
docker compose -f docker-compose.yml -f docker-compose.https.yml up -d
```

Caddy obtains and renews the TLS certificate automatically. Never set
`KHAIKANG_REQUIRE_HTTPS=true` while serving the application directly over HTTP:
the browser will correctly refuse to send secure authentication and CSRF cookies.

## Upgrade and rollback

Set `KHAIKANG_IMAGE_TAG` in `.env` to the required published image tag, then run:

```sh
docker compose pull
docker compose up -d
```

Use an immutable release tag such as `0.1.0-rc.1` or the Git SHA tag published
with each build for a precise rollback. Do not deploy `rc` or `latest` when a
repeatable rollback is required.

## Versioning and image tags

`VERSION` at the repository root is the product version source of truth. It
uses semantic versioning without the Git `v` prefix. The first release candidate
is therefore stored as `0.1.0-rc.1` and tagged in Git as `v0.1.0-rc.1`.

The Docker workflow publishes these tags:

- a version tag such as `0.1.0-rc.1` when a matching Git tag is pushed;
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
git tag -a v0.1.0-rc.1 -m "KhaiKang v0.1.0-rc.1"
git push origin v0.1.0-rc.1
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

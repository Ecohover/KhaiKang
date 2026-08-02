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

## Secrets and persistent data

- `compose/.env` is intentionally ignored by Git. Keep it only on the deployment host.
- `postgres-data` stores all application data. Back it up before an upgrade.
- `data-protection-keys` encrypts and validates authentication cookies. Keep it with
  the database backup; deleting it signs out every user and invalidates existing
  protected data.
- The API applies EF Core migrations only because Compose sets
  `Database__ApplyMigrations=true`. Set it to `false` when operating migrations
  through a separate controlled process.
- For internet-facing deployment, put a TLS reverse proxy such as Caddy in front
  of the `web` service. Do not expose PostgreSQL or the API service directly.

## Upgrade and rollback

Set `KHAIKANG_IMAGE_TAG` in `.env` to the required published image tag, then run:

```sh
docker compose pull
docker compose up -d
```

Use the Git SHA tag published with each main build for a precise rollback.

## Publishing images from GitHub Actions

The publish workflow only runs after a commit reaches `main`. It verifies the
backend and frontend, builds the images, then logs in to Docker Hub and pushes
the images. Pull requests do not build or publish Docker images.

Repository maintainers must configure these GitHub Actions secrets:

| Secret | Value |
| --- | --- |
| `DOCKERHUB_USERNAME` | Docker Hub namespace that owns the `khaikang-api` and `khaikang-web` repositories. |
| `DOCKERHUB_TOKEN` | A Docker Hub access token with read and write permission. Do not use an account password. |

Create the token in Docker Hub under **Account Settings → Personal access tokens**,
then add it in GitHub under **Repository Settings → Secrets and variables → Actions**.
The token is used only by the publish workflow and is never added to an image or
deployment `.env` file.

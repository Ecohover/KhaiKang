# KhaiKang

KhaiKang is an open-source, self-hostable project and test management platform.
This Docker Compose example starts the web application, API, and PostgreSQL.

**Project home and source:** <https://github.com/Ecohover/KhaiKang>

This overview is shared by the two images that make up one KhaiKang deployment:

| Image | Purpose |
| --- | --- |
| `ecohover/khaikang-web` | Browser UI and reverse proxy for API requests. |
| `ecohover/khaikang-api` | ASP.NET Core API, migrations, authentication, and attachment access. |

Use the images together through Docker Compose. Do not expose the API or
PostgreSQL directly to the public internet.

## Image tags

| Tag | Meaning |
| --- | --- |
| `0.1.0` | First stable MVP release. |
| `rc` | Latest build accepted into the RC branch; it can move. |
| `sha-<commit>` | Immutable build for an exact Git commit. |
| `latest` | Latest stable release from `main`; it never points to an RC build. |

For repeatable deployment and rollback, use a version or SHA tag rather than
`rc` or `latest`.

## Quick start

Create an empty directory and add these two files.

`compose.yml`:

```yaml
name: khaikang

services:
  postgres:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 5s
      retries: 12
    restart: unless-stopped

  storage-init:
    image: ecohover/khaikang-api:${KHAIKANG_IMAGE_TAG:-0.1.0}
    user: "0:0"
    entrypoint: ["/bin/sh", "-c"]
    command:
      - |
        set -eu
        : "$${APP_UID:?The API image must define APP_UID}"
        mkdir -p /var/lib/khaikang/data-protection /var/lib/khaikang/attachments
        chown "$${APP_UID}:$${APP_UID}" \
          /var/lib/khaikang/data-protection \
          /var/lib/khaikang/attachments
        stat -c '%u:%g %n' \
          /var/lib/khaikang/data-protection \
          /var/lib/khaikang/attachments
    volumes:
      - data-protection-keys:/var/lib/khaikang/data-protection
      - attachments:/var/lib/khaikang/attachments
    restart: "no"

  api:
    image: ecohover/khaikang-api:${KHAIKANG_IMAGE_TAG:-0.1.0}
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__KhaiKang: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Database__ApplyMigrations: "true"
      DataProtection__KeysDirectory: /var/lib/khaikang/data-protection
      Attachments__Provider: local
      Attachments__LocalRoot: /var/lib/khaikang/attachments
      Attachments__MaxFileSizeBytes: 20971520
      Identity__RequireSecureCookies: ${KHAIKANG_REQUIRE_HTTPS:-false}
    depends_on:
      postgres:
        condition: service_healthy
      storage-init:
        condition: service_completed_successfully
    volumes:
      - data-protection-keys:/var/lib/khaikang/data-protection
      - attachments:/var/lib/khaikang/attachments
    healthcheck:
      test:
        - CMD
        - bash
        - -c
        - >-
          exec 3<>/dev/tcp/127.0.0.1/8080 &&
          printf 'GET /health/live HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n' >&3 &&
          IFS= read -r status <&3 &&
          [[ "$$status" == *" 200 "* ]]
      interval: 5s
      timeout: 5s
      retries: 24
      start_period: 10s
    restart: unless-stopped

  web:
    image: ecohover/khaikang-web:${KHAIKANG_IMAGE_TAG:-0.1.0}
    depends_on:
      api:
        condition: service_healthy
    ports:
      - "${KHAIKANG_HTTP_PORT:-8080}:80"
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--spider", "http://127.0.0.1/api/v1/system/info"]
      interval: 5s
      timeout: 5s
      retries: 12
      start_period: 5s
    restart: unless-stopped

volumes:
  postgres-data:
  data-protection-keys:
  attachments:
```

`.env`:

```dotenv
POSTGRES_DB=khaikang
POSTGRES_USER=khaikang
POSTGRES_PASSWORD=REPLACE_WITH_A_LONG_RANDOM_PASSWORD
KHAIKANG_IMAGE_TAG=0.1.0
KHAIKANG_HTTP_PORT=8080
# Leave false for localhost. Use true only when HTTPS is terminated in front of web.
KHAIKANG_REQUIRE_HTTPS=false
```

Start KhaiKang:

```sh
docker compose pull
docker compose up -d --wait --wait-timeout 180
```

Open `http://localhost:8080` and initialize the first system administrator.
`storage-init` exits successfully after assigning the named volumes to the API's
non-root user; no deployment-host `chown` command is required.

## Documentation and support

- Source and documentation: <https://github.com/Ecohover/KhaiKang>
- Releases: <https://github.com/Ecohover/KhaiKang/releases>
- Issues: <https://github.com/Ecohover/KhaiKang/issues>
- License: MIT

## Production notes

- Keep `.env` private. Never add passwords or tokens to an image or source repository.
- Back up the `postgres-data`, `data-protection-keys`, and `attachments` volumes together.
- Use a fixed release tag such as `0.1.0` or `sha-...` for repeatable upgrades and rollback.
- Run the repository's `deploy/Test-MvpSmoke.ps1` against a uniquely named,
  fresh Compose project before accepting an immutable release image.
- For a public domain, terminate TLS in front of the web service and set
  `KHAIKANG_REQUIRE_HTTPS=true`. The repository includes a Caddy example:
  <https://github.com/Ecohover/KhaiKang/tree/main/deploy>.

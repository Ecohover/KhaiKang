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
| `0.1.0-rc.1` | Immutable first MVP release candidate. |
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

  api:
    image: ecohover/khaikang-api:${KHAIKANG_IMAGE_TAG:-0.1.0-rc.1}
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
    volumes:
      - data-protection-keys:/var/lib/khaikang/data-protection
      - attachments:/var/lib/khaikang/attachments
    restart: unless-stopped

  web:
    image: ecohover/khaikang-web:${KHAIKANG_IMAGE_TAG:-0.1.0-rc.1}
    depends_on:
      - api
    ports:
      - "${KHAIKANG_HTTP_PORT:-8080}:80"
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
KHAIKANG_IMAGE_TAG=0.1.0-rc.1
KHAIKANG_HTTP_PORT=8080
# Leave false for localhost. Use true only when HTTPS is terminated in front of web.
KHAIKANG_REQUIRE_HTTPS=false
```

Start KhaiKang:

```sh
docker compose pull
docker compose up -d
```

Open `http://localhost:8080` and initialize the first system administrator.

## Documentation and support

- Source and documentation: <https://github.com/Ecohover/KhaiKang>
- Releases: <https://github.com/Ecohover/KhaiKang/releases>
- Issues: <https://github.com/Ecohover/KhaiKang/issues>
- License: MIT

## Production notes

- Keep `.env` private. Never add passwords or tokens to an image or source repository.
- Back up the `postgres-data`, `data-protection-keys`, and `attachments` volumes together.
- Use a fixed release tag such as `0.1.0-rc.1` or `sha-...` for repeatable upgrades and rollback.
- For a public domain, terminate TLS in front of the web service and set
  `KHAIKANG_REQUIRE_HTTPS=true`. The repository includes a Caddy example:
  <https://github.com/Ecohover/KhaiKang/tree/main/deploy>.

# KhaiKang

KhaiKang is an open-source, self-hostable project and test management platform.
This Docker Compose example starts the web application, API, and PostgreSQL.

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
    image: ecohover/khaikang-api:${KHAIKANG_IMAGE_TAG:-latest}
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__KhaiKang: Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Database__ApplyMigrations: "true"
      DataProtection__KeysDirectory: /var/lib/khaikang/data-protection
      Identity__RequireSecureCookies: ${KHAIKANG_REQUIRE_HTTPS:-false}
    depends_on:
      postgres:
        condition: service_healthy
    volumes:
      - data-protection-keys:/var/lib/khaikang/data-protection
    restart: unless-stopped

  web:
    image: ecohover/khaikang-web:${KHAIKANG_IMAGE_TAG:-latest}
    depends_on:
      - api
    ports:
      - "${KHAIKANG_HTTP_PORT:-8080}:80"
    restart: unless-stopped

volumes:
  postgres-data:
  data-protection-keys:
```

`.env`:

```dotenv
POSTGRES_DB=khaikang
POSTGRES_USER=khaikang
POSTGRES_PASSWORD=REPLACE_WITH_A_LONG_RANDOM_PASSWORD
KHAIKANG_IMAGE_TAG=latest
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

## Production notes

- Keep `.env` private. Never add passwords or tokens to an image or source repository.
- Back up the `postgres-data` and `data-protection-keys` volumes together.
- Use a fixed `sha-...` tag for repeatable upgrades and rollback.
- For a public domain, terminate TLS in front of the web service and set
  `KHAIKANG_REQUIRE_HTTPS=true`. The repository includes a Caddy example:
  <https://github.com/Ecohover/KhaiKang/tree/main/deploy>.

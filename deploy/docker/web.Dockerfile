ARG APP_VERSION=0.0.0-dev
FROM node:26-bookworm-slim AS build
ARG APP_VERSION
WORKDIR /src/frontend
ENV KHAIKANG_VERSION=$APP_VERSION

RUN corepack enable
COPY VERSION /src/VERSION
COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
COPY frontend/apps/web/package.json apps/web/package.json
COPY frontend/packages/ui/package.json packages/ui/package.json
RUN pnpm install --frozen-lockfile

COPY frontend ./
RUN pnpm --filter @khaikang/web build

FROM nginx:1.31-alpine AS runtime
ARG APP_VERSION
LABEL org.opencontainers.image.version=$APP_VERSION
COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/frontend/apps/web/dist /usr/share/nginx/html

EXPOSE 80

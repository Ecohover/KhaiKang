FROM node:22-bookworm-slim AS build
WORKDIR /src/frontend

RUN corepack enable
COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
COPY frontend/apps/web/package.json apps/web/package.json
COPY frontend/packages/ui/package.json packages/ui/package.json
RUN pnpm install --frozen-lockfile

COPY frontend ./
RUN pnpm --filter @khaikang/web build

FROM nginx:1.31-alpine AS runtime
COPY deploy/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/frontend/apps/web/dist /usr/share/nginx/html

EXPOSE 80

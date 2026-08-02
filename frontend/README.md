# KhaiKang Frontend

The frontend is a pnpm workspace:

```text
apps/web       Vue web application
packages/ui    Replaceable KhaiKang UI primitives and design tokens
```

Run commands from this directory:

```shell
pnpm install
pnpm dev
pnpm type-check
pnpm test
pnpm build
```

The canonical HTTP contract is `../contract/openapi/khaikang.v1.yaml`.
TypeScript wire types and the project-specific HTTP client are maintained with
the backend through the documented AI/OpenAPI workflow; no client generator is
required.

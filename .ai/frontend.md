# Frontend Context

## Write Interaction Contract

- Every successful create or update must call `useSaveNotice`; the single app-level `SaveNoticeHost` renders a dismissible bottom-right notice that expires after five seconds.
- Use `UiActionDialog` in addition to the notice when the user must read, preserve, or acknowledge dynamic content. Do not build page-local modal shells.
- Forms that support repeated creation must use `UiCreateActions` and implement the same create / create-and-continue behavior as Issue creation.

## i18n Boundary

- Translate only product-authored UI copy: navigation, labels, actions, statuses, guidance, validation, errors, notifications, and route titles.
- Never translate user-authored or external record content such as project names/codes, issue titles/descriptions, usernames, comments, or test case text.
- User-authored values may be interpolated into translated messages, but the values must remain unchanged.

Read this file only for changes under `frontend/`.

## Workspace Boundaries

- The frontend is a pnpm workspace under `frontend/`.
- The web application belongs in `frontend/apps/web`.
- Reusable, domain-neutral UI primitives belong in `frontend/packages/ui` only
  after the web application has a real caller.
- Do not add a full UI framework without an accepted project decision. Prefer
  small components and CSS variables that remain replaceable.

## Implementation Rules

- Use Vue 3, TypeScript strict mode, Composition API, and `<script setup>`.
- Keep TypeScript wire types and HTTP client methods aligned with
  `contract/openapi/khaikang.v1.yaml` through the documented AI workflow.
- API contract files are maintained source code. Preserve compatible custom
  authentication, CSRF, and error-handling behavior when changing them.
- Views and stores use the shared API client instead of raw fetch calls. The
  CSRF bootstrap helper is the only current exception.
- Keep page state local unless multiple routes genuinely share it.
- Components include loading, disabled, error, keyboard, focus, and responsive
  states required by their workflow.
- Use Lucide icons for recognizable commands. Keep visible labels for commands
  where an icon alone would be ambiguous.

## Relevant Commands

Run commands from `frontend/`:

```shell
pnpm install
pnpm type-check
pnpm test
pnpm build
```

When the HTTP contract changes, compare each affected operation with the
backend implementation and run type-check, tests, and build.

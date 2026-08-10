# UI Guidelines

Status: synchronized on 2026-08-09. Traditional Chinese counterpart: [UI Guideline](../zh-TW/01-overview/07-ui-guidelines.md).

## Ownership

- Design tokens: `frontend/packages/ui/src/tokens.css`
- Domain-neutral components: `frontend/packages/ui/src/`
- KhaiKang application compositions: `frontend/apps/web/src/components/`
- Feature-specific workflows: their view or feature component

API calls, authorization, domain mapping, and feature workflows must not move into the UI package.

## Controls and Forms

- Standard toolbar controls are 32 px high.
- Prefer `UiButton`, `UiInput`, `UiTextarea`, `UiSelect`, `UiPagination`, `UiFormSection`, and `UiField`.
- Required, disabled, error, focus, keyboard, and narrow-screen behavior are part of completion.
- Long-form supported fields store Markdown source. Rendered content must be sanitized; user-authored HTML is not stored as trusted HTML.

## Lists and Feedback

- Use `UiTableContainer` and `UiTable` for consistent presentation, while features retain column, sorting, drag, and permission behavior.
- Use `UiEmptyState` for empty data and the shared state banner for loading or page-level failures.
- Use `UiStatusBadge` only for presentation; editable state uses an accessible control.
- Use `UiAlert` for inline messages and `useSaveNotice` for successful writes.

## Accessibility and Responsive Behavior

- Every interactive control has visible text, a label, or `aria-label`.
- Keyboard focus is visible and disabled state is not communicated by color alone.
- Toolbars may wrap; tables must scroll horizontally or provide a narrow-screen alternative.
- Drag operations require a non-drag alternative.

## Verification

For shared UI changes, run `pnpm type-check`, `pnpm test`, and `pnpm build`, then inspect both English and Traditional Chinese rendering.

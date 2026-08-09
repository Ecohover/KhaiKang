# Documentation Guidelines

## Purpose

KhaiKang is an open-source project with English public entry points and an
active Traditional Chinese product-design workspace. This document defines how
both sets of documentation remain usable without creating two competing sources
of truth.

## Language Policy

- Source code identifiers, comments, XML documentation, log templates, and
  technical notes inside source files use English.
- Public repository operations and contributor guidance use English as the
  primary version. This includes `README.md`, `CONTRIBUTING.md`, `AGENTS.md`,
  architecture, engineering, deployment, and security guidance.
- Traditional Chinese translations may accompany public engineering documents.
- Paired MVP product specs and data-model documents under `doc/zh-TW/` and
  `doc/en/` are maintained together. Each synchronized English document states
  its synchronization date and links to the Traditional Chinese counterpart.
- Historical planning notes may remain Traditional-Chinese-only. They are not
  part of bilingual MVP completeness unless the documentation maps identify
  them as current release guidance.
- When paired documents disagree, the canonical OpenAPI contract, accepted
  behavior tests, and current implementation determine the shipped behavior;
  both language versions must then be corrected in the same change.

## Documentation Hierarchy

1. `README.md` and `README.zh-TW.md`
   - Concise project entry points, quick start, and top-level links.
2. `AGENTS.md`
   - Repository instructions for coding agents.
3. `CONTRIBUTING.md`
   - Contribution workflow, commands, commits, and pull request expectations.
4. `doc/en/`
   - Public English architecture and engineering guidance.
5. `doc/zh-TW/`
   - Current Traditional Chinese product specs, data models, planning, and
     localized engineering guidance.

Working notes that are not intended for the public repository must not be
linked from the public README.

## Writing Rules

- Keep one primary purpose per document.
- Use clear headings, short paragraphs, and repository-relative links.
- State scope, non-goals, behavior, and open questions explicitly.
- Do not duplicate rules across many files. Link to the authoritative rule.
- Update links and reading maps when files move.
- Mark drafts and transitional translations clearly.
- Never place secrets, private URLs, real credentials, or personal data in
  documentation examples.

## Functional Spec Format

Functional specs follow `doc/spec-template.md` and normally include:

1. Purpose
2. Related documents
3. Scope
4. Non-goals
5. Core model
6. Flow
7. States and rules
8. Security and audit
9. Acceptance criteria
10. Open questions

Planning status belongs in planning documents, not inside the functional rules
or data model.

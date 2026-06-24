# Documentation Guidelines

## Language Policy

- Source code comments, XML documentation, and technical notes inside the codebase must be written in English.
- English is the primary language for all project documentation.
- Traditional Chinese documentation is allowed as a localized supplement.
- Other languages may be added later by contributors as localized documentation, but they must not replace the English source of truth.

## Documentation Hierarchy

1. `README.md` and `README.zh-TW.md`
   - Entry points for the project.
   - Keep them concise and focused on install, quick start, and top-level links.

2. `doc/en/`
   - Primary documentation source.
   - Architecture notes, specs, roadmap, and contribution guidance should live here first.

3. `doc/zh-TW/`
   - Localized Traditional Chinese documentation.
   - Should mirror important public-facing docs when needed, but not become the authoritative source.

4. `doc/internal/`
   - Internal strategy, draft analysis, and working notes.
   - Keep this content concise, factual, and separated from public-facing docs.

## Writing Rules

- Use clear, direct English.
- Prefer short sections with explicit headings.
- Keep docs focused on one purpose per file.
- Avoid mixing product vision, implementation detail, and deployment instructions in the same document.
- When a document has a public and internal version, keep the same structure across languages.

## Recommended File Types

- `architecture/*.md`
- `specs/*.md`
- `roadmap/*.md`
- `contributing.md`
- `deployment/*.md`
- `security/*.md`

## Practical Rule

If a document is intended to guide implementation or review, write it in English first.
If a localized version is needed later, add it under the matching language folder and keep the structure aligned with the English version.

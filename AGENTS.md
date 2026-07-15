# Repository Guidelines for Coding Agents

## Purpose

This is the tool-neutral entry point for AI-assisted development. Keep this
file short. Detailed context is loaded on demand through `.ai/INDEX.md`.

## Context Budget

- Do not recursively read `.ai/`, `doc/`, or the entire repository.
- Read only the files needed for the current task.
- Start with `.ai/INDEX.md`, then load only the matching context document.
- Follow links to formal specifications only when the change requires them.
- Prefer current code, tests, and repository documents over chat memory.

## Source of Truth

Use sources in this order:

1. Accepted functional specifications and data models under `doc/`.
2. Architecture decisions and engineering guidelines under `doc/`.
3. Existing public contracts and tests.
4. Existing implementation.

Do not silently resolve conflicts between these sources. Report the conflict
and keep the authoritative document aligned with the accepted behavior.

## Required Conduct

- Inspect the affected area before editing and state material assumptions.
- Preserve unrelated work and keep changes focused on the requested behavior.
- Do not weaken security, validation, audit, or compatibility guarantees to
  make a change pass.
- Do not commit, push, publish, or deploy unless explicitly requested.
- Never include secrets, production data, private prompts, or company-specific
  code and configuration.
- AI-generated code follows the same review, licensing, build, and test
  standards as human-written code. The contributor remains responsible.
- Never claim a command or test passed unless it was actually run successfully.

## Task Routing

Read `.ai/INDEX.md` and select only the context matching the current task.
Detailed project rules belong in formal documentation, not duplicated AI files.

Before finishing, report:

- what changed;
- verification commands and results;
- anything not verified;
- known risks or follow-up work.

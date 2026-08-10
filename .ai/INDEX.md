# AI Context Index

This directory provides small, task-specific context for coding agents. It is
not a second source of project requirements.

Read only the matching document:

| Task scope | Context to read |
| --- | --- |
| Changes under `backend/` | `.ai/backend.md` |
| Changes under `frontend/` | `.ai/frontend.md` |
| Changes under `contract/` or public HTTP API changes | `.ai/contract.md` |
| Build, test, formatting, or completion reporting | `.ai/verification.md` |
| Release preparation, Git tags, GitHub Releases, Docker image publishing, or backup/restore drills | `.ai/release.md` |
| Continuing unfinished work from another person or AI | The referenced handoff file based on `.ai/handoff-template.md` |
| AI Agent, Prompt selection, Agent Runtime, or multi-Agent orchestration design | `doc/zh-TW/04-planning/04-ai-agent-orchestration-direction.md` |

## Loading Rules

- Do not preload every file in this directory.
- Do not recursively scan `doc/`.
- A context file may point to a formal document. Read it only when it directly
  affects the current change.
- For tasks crossing multiple scopes, load only the applicable context files.
- Repository documents and current code override chat history or AI memory.

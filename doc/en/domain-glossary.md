# Domain Glossary

Status: synchronized on 2026-08-09. Traditional Chinese counterpart: [語詞總表](../zh-TW/01-overview/04-domain-glossary.md).

| Term | Meaning |
| --- | --- |
| Account | A local human or future AI identity that can be authenticated and audited. |
| System role | A global role such as `Admin` or `User`. |
| Permission | A capability string; scope is supplied by membership rather than encoded in the permission. |
| Project | The access and workflow boundary for issues. |
| Project member | An account's active relationship with one project. |
| Project role | A fixed MVP role: Owner, Manager, Contributor, or Reviewer. |
| Issue | The primary project work item. |
| Test Workspace | The access and collaboration boundary for test assets. |
| Test Suite | A tree node that groups test cases. |
| Test Case | A reusable manual test specification with stable UUID and Workspace-scoped human-readable number. |
| Test Tag | A system-wide label associated with test cases. |
| Test Plan | A fixed, ordered selection of test cases. |
| Test Run | One manual execution snapshot created from a Test Plan. |
| Test Run Item | The snapshot and result for one case in a Test Run. |
| Attachment | File metadata stored in PostgreSQL plus bytes stored by `IFileStorage`. |
| Audit metadata | Created/updated actor, time, and optimistic `version` fields. |

Human-readable test codes use the Workspace prefix: `{PREFIX}-TC{caseNo}` for Cases, `{PREFIX}-TP{planNo}` for Plans, and `{PREFIX}-TP{planNo}-R{runNo}` for Runs. UUIDs remain the internal primary keys.

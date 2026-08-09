# Test Management Data Model

Status: synchronized with `InitialTestManagement` on 2026-08-09. Traditional Chinese counterpart: [測試案例管理資料模型](../../zh-TW/03-data-model/03-test-case-management-data-model.md).

## Implemented Tables

| Table | Key responsibility and constraints |
| --- | --- |
| `test_workspaces` | Unique name and prefix, active/inactive status, and audit metadata. |
| `test_workspace_members` | Workspace/account membership with fixed role and lifecycle; Workspace/account pair is unique. |
| `test_workspace_projects` | Unique many-to-many Workspace/Project navigation link; it grants no access. |
| `test_suites` | Workspace-scoped tree node, parent, sort order, and lifecycle. Application rules cap depth at five. |
| `test_cases` | Direct Workspace scope, Suite, stable Workspace-scoped `case_no`, Markdown-capable content, order, lifecycle, and audit metadata. `(test_workspace_id, case_no)` is unique. |
| `test_case_steps` | Ordered Case action and expected result; `(test_case_id, step_no)` is unique. |
| `test_tags` | System-wide unique tag name and lifecycle. |
| `test_case_tags` | Unique Case/Tag association. |
| `test_case_attachments` | Case attachment metadata and soft-delete state. |
| `test_plans` | Workspace-scoped stable `plan_no`, name, description, status, and audit metadata. `(test_workspace_id, plan_no)` is unique. |
| `test_plan_items` | Fixed ordered Plan/Case selection; Plan/Case pair is unique. |
| `test_runs` | Plan-scoped stable `run_no`, lifecycle, creator, execution times, summary, and audit metadata. `(test_plan_id, run_no)` is unique. |
| `test_run_items` | Ordered Case snapshot, result, actual result, actual executor, execution time, and audit metadata. |
| `test_run_item_step_results` | Ordered Step snapshot and manual result; `(test_run_item_id, step_no)` is unique. |
| `test_run_item_attachments` | Run Item evidence metadata and soft-delete state. |
| `test_number_counters` | PostgreSQL-managed scoped counters for `case`, `plan`, and `run`; `(counter_type, scope_id)` is the primary key. |

Attachment tables store metadata only. `storage_provider` and opaque `storage_key` route file bytes through `IFileStorage`. Deletion is soft deletion.

Run creation copies Case and Step content. Once a Run exists, later Case, Suite, or Plan changes cannot alter the snapshot. `in_progress` locks scope and structure but does not lock execution to one account. Completed Runs are immutable; cancelled Runs may restart with retained results.

Case, Plan, and Run creation call `next_test_number` in the same transaction as their inserts. Case and Plan counters use Workspace ID as scope; Run counters use Plan ID. PostgreSQL serializes only requests for the same counter row, allowing unrelated Workspaces and Plans to allocate numbers independently.

Repositories, environments, execution agents, automation bindings, external result records, and AI execution tables are not part of the MVP baseline.

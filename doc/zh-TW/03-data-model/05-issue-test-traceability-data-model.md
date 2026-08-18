# Issue 與測試資產追溯資料模型

狀態：產品方向已確認，進入實作（2026-08-10）。

## 目的

本文件定義 Issue 基本關聯，以及 Case、Plan、Run 與 Issue 之間的追溯資料責任。功能規則以 [Issue 與測試資產追溯規格](../02-specs/04-issue-test-traceability.md) 為準。

## 模組責任

- Project Management 擁有 `issue_relation_types` 與 `issue_relations`。
- Test Management 擁有 `test_case_requirement_links`、Plan 的測試 Issue 關聯、Run 的測試 Issue 快照與 `test_run_bug_links`。
- Project Management 透過明確的 Issue directory contract 提供 Issue 所屬 Project、類型、顯示資訊及權限判斷。
- Test Management 應用程式碼不得直接讀取 Project Management DbContext。
- Migration 順序維持 Identity、Project Management、Test Management。

## issue_relation_types

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `id` | `uuid` | 固定種子主鍵。 |
| `code` | `varchar(50)` | 唯一；第一版為 `related`、`parent_of`、`blocks`、`duplicates`、`tests`。 |
| `forward_label` | `varchar(100)` | 正向顯示文字。 |
| `reverse_label` | `varchar(100)` | 反向顯示文字。 |
| `direction_kind` | `varchar(20)` | `symmetric`、`directed` 或 `hierarchical`。 |
| `sort_order` | `integer` | 顯示順序。 |
| `is_system` | `boolean` | 第一版種子皆為 `true`。 |
| `is_active` | `boolean` | 停用後不得新增，但保留既有資料。 |
| `audit_info` | `-` | 共通建立、更新與版本資訊。 |

第一版不提供關聯類型 CRUD。

## issue_relations

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `id` | `uuid` | 主鍵。 |
| `project_id` | `uuid` | 對應 `projects.id`。 |
| `relation_type_id` | `uuid` | 對應 `issue_relation_types.id`。 |
| `source_issue_id` | `uuid` | 對應 `issues.id`。 |
| `target_issue_id` | `uuid` | 對應 `issues.id`。 |
| `is_deleted` | `boolean` | 軟刪除旗標。 |
| `deleted_at` | `timestamptz` | 軟刪除時間。 |
| `deleted_by_account_id` | `uuid` | 軟刪除操作人。 |
| `audit_info` | `-` | 共通建立、更新與版本資訊。 |

約束與索引：

- `source_issue_id <> target_issue_id`。
- 來源與目標必須都屬於 `project_id`。
- `related` 保存前依 UUID 排序，使 A–B 與 B–A 使用相同標準化方向。
- 有效的 `relation_type_id + source_issue_id + target_issue_id` 唯一。
- `parent_of` 對有效 `target_issue_id` 建立唯一限制，使子 Issue 最多一個父層。
- `parent_of` 的循環與間接循環由應用層在 transaction 中檢查。
- 建立來源、目標與 Project 查詢索引。

## test_case_requirement_links

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `id` | `uuid` | 主鍵。 |
| `test_workspace_id` | `uuid` | 對應 `test_workspaces.id`。 |
| `test_case_id` | `uuid` | 對應 `test_cases.id`。 |
| `project_id` | `uuid` | 對應 `projects.id`。 |
| `requirement_issue_id` | `uuid` | 對應 `issues.id`。 |
| `is_deleted` 與刪除欄位 | `-` | 支援軟刪除。 |
| `audit_info` | `-` | 共通稽核與版本資訊。 |

有效的 `test_case_id + requirement_issue_id` 唯一。`test_workspace_id + project_id` 必須存在有效 Workspace–Project 關聯。

## Test Plan 測試 Issue

`test_plans` 新增：

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `test_issue_project_id` | `uuid` | 可空；測試 Issue 所屬 Project。 |
| `test_issue_id` | `uuid` | 可空；一份 Plan 最多一張測試 Issue。 |

兩欄必須同時為空或同時有值。Issue 必須屬於該 Project，且 Workspace–Project 關聯有效。Plan 更新測試 Issue 時沿用 Plan 的樂觀版本控制。

## Test Run 測試 Issue 快照

`test_runs` 新增：

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `test_issue_project_id` | `uuid` | 可空；建立 Run 時從 Plan 複製。 |
| `test_issue_id` | `uuid` | 可空；建立 Run 時從 Plan 複製。 |

Run 建立後不提供更新這兩個欄位的 API。兩欄必須同時為空或同時有值。

## test_run_bug_links

| 欄位 | 型別 | 規則 |
| --- | --- | --- |
| `id` | `uuid` | 主鍵。 |
| `test_workspace_id` | `uuid` | 對應 Run 所屬 Workspace。 |
| `test_run_id` | `uuid` | 對應 `test_runs.id`。 |
| `project_id` | `uuid` | Bug 所屬 Project。 |
| `bug_issue_id` | `uuid` | 對應 `issues.id`，建立時必須為 `bug` 類型。 |
| `audit_info` | `-` | 建立人與時間；第一版不提供移除來源。 |

有效的 `bug_issue_id` 唯一，表示一張新建 Bug 第一版只有一個來源 Run；同一 Run 可有多張 Bug。

## 跨模組應用合約

Project Management 應提供獨立 Issue directory contract，至少支援：

- 依 Issue ID 與帳號取得可讀取的最小資訊。
- 確認 Issue 是否屬於指定 Project、是否為指定類型，以及呼叫者是否具備 `issue.read`、`issue.update` 或 `issue.create`。
- 批次取得呼叫者可讀取的 Issue 最小資訊。

最小資訊包含 `issue_id`、`project_id`、Project code、Issue number、標題、類型及狀態。

## 生命週期、稽核與併發

- Issue 關聯與 Case 需求關聯使用軟刪除並保留歷史。
- Plan 的測試 Issue 是目前設定；Run 保存建立時快照。
- Run–Bug 是來源證據，第一版不跟隨 Bug 或 Run 狀態刪除。
- 建立、取代與移除必須記錄實際帳號、時間及版本。
- Workspace–Project unlink 必須拒絕仍有有效 Case、Plan、Run 或 Bug 追溯的範圍。
- 關聯建立與 unlink 應鎖定相同的 `test_workspace_projects` 資料列，避免併發繞過檢查。

## 後續擴充

- Bug 的 Run Item／Step 來源證據與跨 Run 重現紀錄。
- 可自訂 Issue 關聯類型。
- 跨 Project Issue 關聯。
- 需求覆蓋率與追溯圖讀模型。

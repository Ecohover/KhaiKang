# 測試案例管理資料模型

## 目的

這份文件定義 KhaiKang 測試案例管理的資料模型方向。

本文件目前只處理第一期 MVP 的測試案例、測試計畫、手動測試執行與步驟結果。自動化測試執行仍待確認，不在本文件定義資料表與欄位。

## 相關文件

- [測試案例管理規格](../02-specs/03-test-case-management.md)
- [資料模型總覽](./01-data-model-overview.md)
- [Audit Info 結構](./99-audit-metadata-fields.md)
- [語詞總表](../01-overview/04-domain-glossary.md)

## 設計原則

- 所有 Entity 主鍵使用 UUID。
- 資料表、欄位、constraint 與 index 使用 `snake_case`。
- 所有主檔採用統一的 `audit_info` 語意。
- Test Plan 使用手動編排的 `test_plan_items` 固定案例範圍，不保存 Suite 或 Tag 的動態篩選條件。
- Test Run 與 Test Run Item 保存執行當下的案例與步驟快照，避免後續修改 Test Case 影響歷史結果。
- Test Run 本身即為 MVP 測試報告，不另建立 `test_reports`。

## 核心資料表

### 測試工作區

- `test_workspaces`
- `test_workspace_projects`
- `test_workspace_members`

### 測試案例目錄

- `test_suites`
- `test_cases`
- `test_case_steps`
- `test_tags`
- `test_case_tags`
- `test_case_attachments`

### 測試計畫

- `test_plans`
- `test_plan_items`

### 測試執行與結果

- `test_runs`
- `test_run_items`
- `test_run_item_step_results`
- `test_run_item_attachments`

### 待確認的未來方向

自動化執行未納入目前資料模型。後續確認需求後，預期可能需要 `test_repositories`、`test_environments`、`test_execution_agents`、`test_automation_bindings` 與 `test_execution_records`，但目前不代表資料表定稿。

## 主要關聯

```text
test_workspaces
  |- test_workspace_projects -> projects
  |- test_workspace_members -> accounts
  |- test_suites (tree)
  |    `- test_cases
  |         |- test_case_steps
  |         |- test_case_tags -> test_tags
  |         `- test_case_attachments
  |
  |- test_plans
  |    `- test_plan_items -> test_cases
  |
  `- test_runs
       `- test_run_items
            |- test_run_item_step_results
            `- test_run_item_attachments
```

## 資料表責任

| 資料表 | 類型 | 說明 |
| --- | --- | --- |
| `test_workspaces` | Entity | 保存測試資產的根工作區。 |
| `test_workspace_projects` | Mapping | 保存 Test Workspace 與 Project 的多對多導覽關聯。 |
| `test_workspace_members` | Entity | 保存帳號在測試工作區中的固定角色與成員生命週期。 |
| `test_suites` | Entity | 保存測試工作區內可形成樹狀結構的測試套件。 |
| `test_cases` | Entity | 保存可重複使用的測試案例與前置準備。 |
| `test_case_steps` | Entity | 保存測試案例中可排序的步驟與每步預期結果。 |
| `test_tags` | Entity | 保存系統共用的測試標籤主資料。 |
| `test_case_tags` | Mapping | 保存測試案例與測試標籤的多對多關聯。 |
| `test_case_attachments` | Entity | 保存 Test Case 附件中繼資料與軟刪除狀態。 |
| `test_plans` | Entity | 保存一次測試工作的計畫與目的。 |
| `test_plan_items` | Entity | 保存使用者手動編排後的固定案例範圍與排序。 |
| `test_runs` | Entity | 保存一次實際測試執行與整體測試報告。 |
| `test_run_items` | Entity | 保存一次 Test Run 中每個案例的快照與結果。 |
| `test_run_item_step_results` | Entity | 保存 Test Run Item 中每個測試步驟的實際結果。 |
| `test_run_item_attachments` | Entity | 保存 Test Run Item 的執行證據附件中繼資料。 |

---

### test_workspaces

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_workspaces` |
| 說明 | 保存測試資產的根工作區。 |
| PK | `id` |
| FK | 無 |
| 備註 | 透過 `test_workspace_projects` 關聯多個 `projects`；存取權仍由 `test_workspace_members` 管理。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試工作區主鍵（`test_workspace`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `name` | 測試工作區名稱 | `varchar(200)` | Y | Y | 對人可讀的主要名稱。 |
| `prefix` | 案例編號前綴 | `varchar(10)` | Y | Y | 2–10 個英文字母或數字，且以字母開頭；建立時未指定則由系統產生。 |
| `description` | 測試工作區說明 | `text` | N | N | 補充測試資產範圍與用途。 |
| `status` | 測試工作區狀態 | `varchar(20)` | Y | N | 目前支援 `active`、`inactive`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 可建立與管理測試資產。 | 預設狀態。 |
| `inactive` | 不再用於新的測試規劃。 | 不刪除既有 Suite、Case、Plan 或 Run。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 基本資料 | `name`、`prefix`、`description` |
| 狀態資訊 | `status` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `name` 在系統範圍內唯一。
- `prefix` 正規化為大寫並在系統範圍內唯一；建立後不因 Workspace 名稱更新而改變。
- Workspace 停用不代表刪除；既有測試歷程必須可追溯。
- Project 與 Test Workspace 以多對多關聯表處理，不在 `test_workspaces` 重複保存單一 `project_id`。
- 建立 Workspace 的帳號必須在同一個 transaction 內建立為第一位 `owner` 成員。

#### Index 建議

- 建立 `idx_test_workspaces_status` 於 `status`。

#### 唯一約束建議

- 建立 unique constraint `uq_test_workspaces_name` 於 `name`。
- 建立 unique constraint `uq_test_workspaces_prefix` 於 `prefix`。

---

### test_workspace_projects

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_workspace_projects` |
| 說明 | 保存 Test Workspace 與 Project 的多對多關聯，供導覽與範圍辨識使用。 |
| PK | `id` |
| FK | `test_workspace_id -> test_workspaces.id`、`project_id -> projects.id` |
| 備註 | 不取代 Workspace 成員權限，也不建立 Issue、Test Case、Plan 或 Run 的細部追溯。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 關聯主鍵 | `uuid` | Y | Y | Entity 主鍵。 |
| `test_workspace_id` | Test Workspace 識別 | `uuid` | Y | N | 對應 `test_workspaces.id`。 |
| `project_id` | Project 識別 | `uuid` | Y | N | 對應 `projects.id`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構見 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 補充規則

- 同一個 `test_workspace_id + project_id` 不得重複。
- 關聯建立或移除不影響既有 Test Plan、Test Run 與快照。
- Workspace 資產存取仍只依 Workspace 成員與固定角色判斷；Project 成員資格不自動授與 Workspace 存取權。

#### Index 與唯一約束建議

- 建立 `idx_test_workspace_projects_project_id` 於 `project_id`。
- 建立 unique constraint `uq_test_workspace_projects_workspace_project` 於 `test_workspace_id + project_id`。

---

### test_workspace_members

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_workspace_members` |
| 說明 | 保存帳號在 Test Workspace 內的成員關係與固定角色。 |
| PK | `id` |
| FK | `test_workspace_id -> test_workspaces.id`, `account_id -> accounts.id` |
| 備註 | MVP 不建立自訂 Workspace 角色資料表。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | Workspace 成員主鍵 | `uuid` | Y | Y | Entity 主鍵。 |
| `test_workspace_id` | 測試工作區識別 | `uuid` | Y | N | 對應 `test_workspaces.id`。 |
| `account_id` | 帳號識別 | `uuid` | Y | N | 對應 `accounts.id`。 |
| `role` | 固定角色 | `varchar(20)` | Y | N | `owner`、`manager`、`tester`、`viewer`。 |
| `status` | 成員狀態 | `varchar(20)` | Y | N | `active`、`removed`。 |
| `joined_at` | 加入時間 | `timestamp with time zone` | Y | N | 首次加入 Workspace 的時間。 |
| `removed_at` | 移除時間 | `timestamp with time zone` | N | N | 有效成員為空。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 補充規則

- 同一個 `test_workspace_id + account_id` 同時間只能有一筆有效成員關係。
- 每個 Workspace 至少保留一位有效 `owner`。
- 移除成員使用 `removed` 狀態保留歷程，不直接刪除資料。
- 恢復成員時沿用原成員紀錄並更新角色、狀態與 audit metadata。
- Workspace 建立、成員加入、角色調整與移除必須寫入稽核紀錄。

#### Index 與唯一約束建議

- 建立 `idx_test_workspace_members_account_id` 於 `account_id`。
- 建立 `idx_test_workspace_members_workspace_status` 於 `test_workspace_id + status`。
- 建立 partial unique index `uq_test_workspace_members_active_member` 於
  `test_workspace_id + account_id`，條件為 `status = 'active'`。

---

### test_suites

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_suites` |
| 說明 | 保存測試工作區內可形成樹狀結構的測試套件。 |
| PK | `id` |
| FK | `test_workspace_id -> test_workspaces.id`, `parent_id -> test_suites.id` |
| 備註 | 根套件的 `parent_id` 為空；一個測試案例在 MVP 中只屬於一個測試套件。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試套件主鍵（`test_suite`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_workspace_id` | 測試工作區識別 | `uuid` | Y | N | 對應 `test_workspaces.id`。 |
| `parent_id` | 上層測試套件識別 | `uuid` | N | N | 對應同表 `test_suites.id`；為空表示根套件。 |
| `name` | 測試套件名稱 | `varchar(200)` | Y | N | 同一父層級下不可重複。 |
| `description` | 測試套件說明 | `text` | N | N | 補充功能範圍或分類用途。 |
| `sort_order` | 排序值 | `integer` | Y | N | 控制同一父層級下的顯示順序。 |
| `status` | 測試套件狀態 | `varchar(20)` | Y | N | 目前支援 `active`、`inactive`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 可建立、瀏覽與使用其下測試案例。 | 預設狀態。 |
| `inactive` | 不再用於新增或日常測試規劃。 | 不刪除既有案例、Plan 或 Run 紀錄。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_workspace_id`、`parent_id` |
| 基本資料 | `name`、`description`、`sort_order` |
| 狀態資訊 | `status` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 根測試套件的 `parent_id` 必須為空。
- 子測試套件的 `parent_id` 必須指向同一個 `test_workspace_id` 的測試套件。
- `parent_id` 不可指向自身，也不可形成樹狀循環；此規則由業務邏輯驗證。
- Suite 樹從根節點起最多五層；建立或移動時都必須驗證整個子樹不超過限制。
- 同一個 `test_workspace_id + parent_id` 範圍內，`name` 不得重複。
- 停用套件不代表刪除；既有 Test Case、Test Plan 與 Test Run 歷程必須可追溯。
- 移動套件或測試案例只影響未來目錄結構，不得改變既有 Test Plan 或 Test Run 的快照範圍。

#### Index 建議

- 建立 `idx_test_suites_test_workspace_id` 於 `test_workspace_id`。
- 建立 `idx_test_suites_parent_id` 於 `parent_id`。
- 建立 `idx_test_suites_workspace_parent_sort_order` 於 `test_workspace_id + parent_id + sort_order`。
- 建立 `idx_test_suites_workspace_status` 於 `test_workspace_id + status`。

#### 唯一約束建議

- 建立 partial unique index `uq_test_suites_root_name` 於 `test_workspace_id + name`，條件為 `parent_id IS NULL`。
- 建立 partial unique index `uq_test_suites_child_name` 於 `test_workspace_id + parent_id + name`，條件為 `parent_id IS NOT NULL`。

---

### test_cases

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_cases` |
| 說明 | 保存可重複使用的測試案例與其前置準備、整體預期結果。 |
| PK | `id` |
| FK | `test_workspace_id -> test_workspaces.id`、`test_suite_id -> test_suites.id` |
| 備註 | 直接保存 Workspace scope 以支援穩定案例編號與隔離約束；細部測試步驟另存於 `test_case_steps`。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試案例主鍵（`test_case`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_workspace_id` | 測試工作區識別 | `uuid` | Y | N | 對應 `test_workspaces.id`，建立後不可跨 Workspace 移動。 |
| `test_suite_id` | 測試套件識別 | `uuid` | Y | N | 對應 `test_suites.id`。 |
| `case_no` | Workspace 內案例流水號 | `integer` | Y | N | 與 Workspace Prefix 組成人類可讀代碼，例如 `QA-TC12`。 |
| `title` | 測試案例標題 | `varchar(200)` | Y | N | 對人可讀的主要名稱。 |
| `description` | 測試案例說明 | `text` | N | N | 補充測試目的、範圍或注意事項。 |
| `preconditions` | 前置準備 | `text` | N | N | 執行前必須完成的環境、資料或帳號準備。 |
| `overall_expected_result` | 整體預期結果 | `text` | N | N | 補充跨步驟的完成條件，不取代各步驟的預期結果。 |
| `sort_order` | 排序值 | `integer` | Y | N | 控制同一測試套件下的顯示順序。 |
| `status` | 測試案例狀態 | `varchar(20)` | Y | N | 目前支援 `active`、`inactive`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 可加入新的 Test Plan 與 Test Run。 | 預設狀態。 |
| `inactive` | 不再用於新的測試規劃。 | 不刪除既有 Plan 或 Run 中的快照。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_workspace_id`、`test_suite_id` |
| 基本資料 | `case_no`、`title`、`description`、`preconditions`、`overall_expected_result`、`sort_order` |
| 狀態資訊 | `status` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 測試案例必須隸屬於一個有效的 `test_suite_id`。
- `test_workspace_id` 必須與 `test_suite_id` 所屬 Workspace 相同；這個直接 scope 欄位用於隔離查詢與案例編號唯一約束。
- `case_no` 建立後不可修改；顯示代碼由 Workspace Prefix 與 `case_no` 組成。
- 新增 Test Case 時，業務邏輯應要求至少建立一筆 `test_case_steps`；主表不保存步驟內容。
- `overall_expected_result` 可為空；各步驟預期結果由 `test_case_steps` 保存。
- 停用案例不代表刪除；既有 Test Plan、Test Run 與快照歷程必須可追溯。
- 移動案例至另一個 Suite 只影響未來目錄結構，不得改變既有 Test Plan 或 Test Run 的快照範圍。

#### Index 建議

- 建立 `idx_test_cases_test_suite_id` 於 `test_suite_id`。
- 建立 `idx_test_cases_test_suite_status` 於 `test_suite_id + status`。
- 建立 `idx_test_cases_test_suite_sort_order` 於 `test_suite_id + sort_order`。
- 建立 unique constraint `uq_test_cases_workspace_case_no` 於 `test_workspace_id + case_no`。

#### 唯一約束建議

- MVP 不對 `title` 建立唯一約束，避免同一 Suite 中因不同資料條件或測試目的而需要相近標題時受限。

---

### test_case_steps

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_case_steps` |
| 說明 | 保存測試案例中可排序的操作步驟與每步預期結果。 |
| PK | `id` |
| FK | `test_case_id -> test_cases.id` |
| 備註 | 屬於 Test Case 的組成子實體；不重複保存 `test_workspace_id`。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試案例步驟主鍵（`test_case_step`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_case_id` | 測試案例識別 | `uuid` | Y | N | 對應 `test_cases.id`。 |
| `step_no` | 步驟編號 | `integer` | Y | N | 同一測試案例內由 1 起依序編號。 |
| `action` | 操作說明 | `text` | Y | N | 執行者需要進行的操作。 |
| `expected_result` | 預期結果 | `text` | Y | N | 完成此步驟後應觀察到的結果。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_case_id` |
| 步驟資料 | `step_no`、`action`、`expected_result` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `step_no` 必須為大於 0 的整數。
- 同一個 `test_case_id` 中，`step_no` 不得重複。
- Test Case 至少必須保留一筆有效步驟；刪除最後一筆步驟應由業務邏輯拒絕。
- 調整步驟順序時，應在同一個 transaction 內更新受影響的 `step_no`，避免中途產生重複編號。
- 修改或刪除目前步驟只影響未來測試案例內容；既有 Test Run 的步驟快照不受影響。

#### Index 建議

- 建立 `idx_test_case_steps_test_case_id` 於 `test_case_id`。
- 建立 `idx_test_case_steps_test_case_step_no` 於 `test_case_id + step_no`。

#### 唯一約束建議

- 建立 unique constraint `uq_test_case_steps_case_step_no` 於 `test_case_id + step_no`。

---

### test_tags

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_tags` |
| 說明 | 保存系統共用、可跨 Test Workspace 與 Test Suite 使用的測試標籤。 |
| PK | `id` |
| FK | 無 |
| 備註 | Tag 用於跨 Workspace 分類與搜尋，不取代 Test Suite 的功能分類；MVP 不以 Tag 批次加入 Test Plan。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試標籤主鍵（`test_tag`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `name` | 測試標籤名稱 | `varchar(50)` | Y | N | 例如 `smoke`、`regression`、`api`、`critical`。 |
| `description` | 測試標籤說明 | `text` | N | N | 補充標籤使用時機與邊界。 |
| `status` | 測試標籤狀態 | `varchar(20)` | Y | N | 目前支援 `active`、`inactive`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `active` | 可套用到 Test Case，並用於搜尋與批次加入。 | 預設狀態。 |
| `inactive` | 不再套用到新的 Test Case。 | 既有 Case Tag 關聯與歷史不受影響。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 基本資料 | `name`、`description` |
| 狀態資訊 | `status` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- Tag 名稱在系統內不分大小寫不得重複。
- Tag 停用不代表刪除；既有 Test Case 關聯與歷史必須可追溯。
- Tag 不承擔樹狀或功能目錄用途；功能分類應使用 Test Suite。

#### Index 建議

- 建立 `idx_test_tags_status` 於 `status`。

#### 唯一約束建議

- 建立 unique index `uq_test_tags_lower_name` 於 `lower(name)`。

---

### test_case_tags

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_case_tags` |
| 說明 | 保存 Test Case 與 Test Tag 的多對多關聯。 |
| PK | `id` |
| FK | `test_case_id -> test_cases.id`, `test_tag_id -> test_tags.id` |
| 備註 | Tag 為系統共用資料，可套用至任何 Test Workspace 中的 Test Case。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試案例標籤關聯主鍵 | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_case_id` | 測試案例識別 | `uuid` | Y | N | 對應 `test_cases.id`。 |
| `test_tag_id` | 測試標籤識別 | `uuid` | Y | N | 對應 `test_tags.id`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_case_id`、`test_tag_id` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 同一組 `test_case_id + test_tag_id` 不得重複。
- 移除 Tag 關聯只影響未來查找與規劃；既有 Test Plan 與 Test Run 快照不受影響。

#### Index 建議

- 建立 `idx_test_case_tags_test_case_id` 於 `test_case_id`。
- 建立 `idx_test_case_tags_test_tag_id` 於 `test_tag_id`。

#### 唯一約束建議

- 建立 unique constraint `uq_test_case_tags_case_tag` 於 `test_case_id + test_tag_id`。

---

### test_plans

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_plans` |
| 說明 | 保存在 Test Workspace 中手動編排的測試計畫。 |
| PK | `id` |
| FK | `test_workspace_id -> test_workspaces.id` |
| 備註 | Test Plan 的固定案例範圍由 `test_plan_items` 保存；不保存 Suite 或 Tag 的動態篩選條件。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試計畫主鍵（`test_plan`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_workspace_id` | 測試工作區識別 | `uuid` | Y | N | 對應 `test_workspaces.id`。 |
| `plan_no` | Workspace 內計畫流水號 | `integer` | Y | N | 與 Workspace Prefix 組成人類可讀計畫代碼。 |
| `name` | 測試計畫名稱 | `varchar(200)` | Y | N | 對人可讀的主要名稱。 |
| `description` | 測試計畫說明 | `text` | N | N | 補充測試目的、版本範圍或執行注意事項。 |
| `status` | 測試計畫狀態 | `varchar(20)` | Y | N | 目前支援 `draft`、`active`、`archived`。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `draft` | 正在編排案例範圍，尚未用於正式執行。 | 可自由調整 Item。 |
| `active` | 可建立新的 Test Run。 | 可調整 Item；只影響後續建立的 Run。 |
| `archived` | 不再建立新的 Test Run。 | 保留既有 Plan 與 Run 歷程。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_workspace_id` |
| 基本資料 | `plan_no`、`name`、`description` |
| 狀態資訊 | `status` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- Test Plan 必須屬於一個有效的 `test_workspace_id`。
- `plan_no` 在 Workspace 內唯一且建立後不可修改。
- Test Plan 至少必須有一筆 `test_plan_items` 才能切換為 `active` 或建立 Test Run。
- `draft` 與 `active` 狀態可調整 Item；修改只影響後續建立的 Test Run。
- `archived` 狀態不得新增 Test Run 或修改 Item。
- Test Plan 不保存 Test Case 內容快照；案例與步驟快照在建立 Test Run 時產生。

#### Index 建議

- 建立 `idx_test_plans_test_workspace_id` 於 `test_workspace_id`。
- 建立 `idx_test_plans_workspace_status` 於 `test_workspace_id + status`。

#### 唯一約束建議

- MVP 不對 `name` 建立唯一約束，允許同一 Workspace 為不同版本或目的建立相近名稱的 Plan。
- 建立 unique constraint `uq_test_plans_workspace_plan_no` 於 `test_workspace_id + plan_no`。

---

### test_plan_items

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_plan_items` |
| 說明 | 保存使用者手動加入 Test Plan 的固定測試案例範圍與排序。 |
| PK | `id` |
| FK | `test_plan_id -> test_plans.id`, `test_case_id -> test_cases.id` |
| 備註 | Test Case 與 Test Plan 必須屬於同一個 Test Workspace；此規則由業務邏輯驗證。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試計畫項目主鍵（`test_plan_item`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_plan_id` | 測試計畫識別 | `uuid` | Y | N | 對應 `test_plans.id`。 |
| `test_case_id` | 測試案例識別 | `uuid` | Y | N | 對應 `test_cases.id`。 |
| `sort_order` | 排序值 | `integer` | Y | N | 控制 Test Plan 中的執行順序。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_plan_id`、`test_case_id` |
| 基本資料 | `sort_order` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- 同一個 `test_plan_id + test_case_id` 不得重複。
- 建立 Item 時，業務邏輯必須確認 Test Plan 與 Test Case 屬於同一個 Test Workspace。
- `sort_order` 用於手動編排；調整順序時應在同一個 transaction 內更新受影響項目。
- 移除 Item 只影響後續建立的 Test Run；既有 Test Run 的快照不受影響。

#### Index 建議

- 建立 `idx_test_plan_items_test_plan_id` 於 `test_plan_id`。
- 建立 `idx_test_plan_items_test_case_id` 於 `test_case_id`。
- 建立 `idx_test_plan_items_plan_sort_order` 於 `test_plan_id + sort_order`。

#### 唯一約束建議

- 建立 unique constraint `uq_test_plan_items_plan_case` 於 `test_plan_id + test_case_id`。

---

### test_runs

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_runs` |
| 說明 | 保存由 Test Plan 建立的一次測試執行與其整體結果。 |
| PK | `id` |
| FK | `test_plan_id -> test_plans.id`, `started_by_account_id -> accounts.id` |
| 備註 | Test Run 即 MVP 的測試報告主體；建立時依 Test Plan Item 產生執行項目與案例快照。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試執行主鍵（`test_run`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_plan_id` | 測試計畫識別 | `uuid` | Y | N | 對應 `test_plans.id`。 |
| `run_no` | Plan 內執行流水號 | `integer` | Y | N | 與 Plan 代碼組成人類可讀 Run 代碼。 |
| `name` | 測試執行名稱 | `varchar(200)` | Y | N | 建立時可由 Plan 名稱帶入，允許使用者調整以辨識本次執行。 |
| `status` | 測試執行狀態 | `varchar(20)` | Y | N | 目前支援 `not_started`、`in_progress`、`completed`、`cancelled`。 |
| `started_by_account_id` | 建立執行者識別 | `uuid` | Y | N | 對應 `accounts.id`，記錄建立本次 Run 的帳號。 |
| `started_at` | 開始時間 | `timestamp with time zone` | N | N | 進入或重新進入 `in_progress` 時寫入本次持續執行的開始時間。 |
| `completed_at` | 完成時間 | `timestamp with time zone` | N | N | Run 完成或取消時寫入；重新開始時清空。 |
| `summary` | 執行摘要 | `text` | N | N | 記錄本次執行結論、已知風險或交接資訊。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 狀態值

| 狀態值 | 說明 | 備註 |
| --- | --- | --- |
| `not_started` | 已由 Test Plan 建立，但尚未開始執行。 | 已建立 Item 與步驟快照。 |
| `in_progress` | 至少一個執行項目正在填寫結果。 | 可持續更新執行結果。 |
| `completed` | 本次測試已結束。 | 不代表全部通過；整體結論由 Item 結果判斷。 |
| `cancelled` | 本次測試暫停或取消。 | 保留已填寫的結果與原因；可重新開始回到 `in_progress`。 |

#### 欄位分組建議

| 分組 | 欄位 |
| --- | --- |
| 身份識別 | `id` |
| 關聯欄位 | `test_plan_id`、`started_by_account_id` |
| 基本資料 | `run_no`、`name`、`summary` |
| 狀態資訊 | `status`、`started_at`、`completed_at` |
| 系統欄位 | `audit_info` |

#### 補充規則

- `id` 一律使用 UUID。
- `run_no` 在同一 Test Plan 內唯一且建立後不可修改。
- 只能由 `active` 的 Test Plan 建立新的 Test Run，且建立時 Plan 至少需有一個 Item。
- 建立 Test Run 時，必須在同一個 transaction 內依 `test_plan_items` 建立 `test_run_items`，再依每個 Test Case 的 Step 建立 `test_run_item_step_results`。
- Test Run 建立完成後，不可因 Test Plan 或 Test Case 的後續變更而增減其執行項目或修改快照。
- `completed` 狀態不得再修改執行結果。`cancelled` 狀態可重新開始回到 `in_progress`，保留既有案例與步驟結果，並清空 `completed_at`；若需新的獨立測試紀錄，應建立新的 Test Run。
- MVP 僅支援手動執行。未來自動化觸發仍沿用本表，並在第二段資料模型補上執行來源與自動化紀錄關聯。

#### Index 建議

- 建立 `idx_test_runs_test_plan_id` 於 `test_plan_id`。
- 建立 `idx_test_runs_started_by_account_id` 於 `started_by_account_id`。
- 建立 `idx_test_runs_plan_status` 於 `test_plan_id + status`。
- 建立 `idx_test_runs_completed_at` 於 `completed_at`，供近期測試報告排序。
- 建立 unique constraint `uq_test_runs_plan_run_no` 於 `test_plan_id + run_no`。

---

### test_number_counters

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_number_counters` |
| 說明 | 保存 Test Case、Test Plan 與 Test Run 的 scope 內最後配置編號。 |
| PK | `counter_type + scope_id` |
| 備註 | `case`、`plan` 使用 Workspace ID 作為 scope；`run` 使用 Plan ID 作為 scope。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `counter_type` | 計數器類型 | `varchar(20)` | Y | 複合唯一 | 僅允許 `case`、`plan`、`run`。 |
| `scope_id` | 計數範圍識別 | `uuid` | Y | 複合唯一 | Workspace ID 或 Plan ID。 |
| `last_value` | 最後配置值 | `integer` | Y | N | 必須大於零。 |

#### 配置規則

- 建立 Case、Plan 或 Run 時，必須在寫入主資料的同一個 transaction 內呼叫 `next_test_number(counter_type, scope_id)`。
- Function 使用原子的 `INSERT ... ON CONFLICT ... DO UPDATE ... RETURNING`；相同 counter row 的並行請求由 PostgreSQL 依序配置，不同 Workspace 或 Plan 不互相鎖定。
- 若主資料寫入失敗並 rollback，counter 更新也必須一併 rollback。
- Case、Plan 與 Run 主表上的 scope 唯一約束仍保留為最後一道資料完整性保護。

---

### test_run_items

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_run_items` |
| 說明 | 保存一次 Test Run 中每個 Test Case 的執行結果與案例快照。 |
| PK | `id` |
| FK | `test_run_id -> test_runs.id`, `test_case_id -> test_cases.id`, `executed_by_account_id -> accounts.id` |
| 備註 | `test_case_id` 保留原始案例追溯；案例顯示內容以本表快照為準。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 測試執行項目主鍵（`test_run_item`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_run_id` | 測試執行識別 | `uuid` | Y | N | 對應 `test_runs.id`。 |
| `test_case_id` | 原始測試案例識別 | `uuid` | Y | N | 對應 `test_cases.id`，用於追溯來源案例。 |
| `sort_order` | 排序值 | `integer` | Y | N | 依建立 Run 時的 Plan Item 順序寫入。 |
| `case_title` | 案例名稱快照 | `varchar(200)` | Y | N | 建立 Run 當下複製 `test_cases.title`。 |
| `case_description` | 案例說明快照 | `text` | N | N | 建立 Run 當下複製案例說明。 |
| `preconditions` | 前置條件快照 | `text` | N | N | 建立 Run 當下複製案例前置條件。 |
| `overall_expected_result` | 整體預期結果快照 | `text` | N | N | 建立 Run 當下複製案例整體預期結果。 |
| `result_status` | 執行結果 | `varchar(20)` | Y | N | 目前支援 `not_run`、`passed`、`failed`、`blocked`、`skipped`。 |
| `actual_result` | 實際結果 | `text` | N | N | 記錄案例層級的實際結果或失敗摘要。 |
| `executed_by_account_id` | 執行者識別 | `uuid` | N | N | 對應 `accounts.id`；尚未執行時可為空。 |
| `executed_at` | 執行時間 | `timestamp with time zone` | N | N | 首次填寫最終結果時寫入。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 結果值

| 結果值 | 說明 |
| --- | --- |
| `not_run` | 尚未執行。 |
| `passed` | 測試通過。 |
| `failed` | 測試失敗。 |
| `blocked` | 因環境、相依服務或其他阻礙而無法完成。 |
| `skipped` | 本次刻意略過，不代表通過。 |

#### 補充規則

- `id` 一律使用 UUID。
- 建立 Test Run 時，必須為每個 Test Plan Item 建立一筆 Test Run Item；同一次 Run 的 `test_case_id` 不得重複。
- 案例內容一律讀取 `case_title`、`case_description`、`preconditions`、`overall_expected_result` 快照，不回頭讀取已變動的 Test Case。
- `result_status` 初始值為 `not_run`。
- MVP 由人工填寫結果；`passed`、`failed`、`blocked`、`skipped` 皆應記錄 `executed_by_account_id` 與 `executed_at`。
- 案例有步驟時，應用層依步驟結果重新計算並保存案例結果；無步驟案例則直接保存使用者選擇的案例結果。

#### Index 建議

- 建立 `idx_test_run_items_test_run_id` 於 `test_run_id`。
- 建立 `idx_test_run_items_test_case_id` 於 `test_case_id`。
- 建立 `idx_test_run_items_run_result_status` 於 `test_run_id + result_status`。
- 建立 `idx_test_run_items_run_sort_order` 於 `test_run_id + sort_order`。

#### 唯一約束建議

- 建立 unique constraint `uq_test_run_items_run_case` 於 `test_run_id + test_case_id`。

---

### test_run_item_step_results

#### 資料表規格

| 項目 | 內容 |
| --- | --- |
| 資料表名稱 | `test_run_item_step_results` |
| 說明 | 保存一次 Test Run 中每一個案例步驟的快照與執行結果。 |
| PK | `id` |
| FK | `test_run_item_id -> test_run_items.id`, `executed_by_account_id -> accounts.id` |
| 備註 | 不直接依賴 `test_case_steps.id`，避免原始案例步驟修改或刪除後破壞歷史報告。 |

#### 欄位規格

| 名稱 | 說明 | 型別 | 必填 | 唯一 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `id` | 步驟執行結果主鍵（`test_run_item_step_result`） | `uuid` | Y | Y | Entity 主鍵，系統內部真正識別碼。 |
| `test_run_item_id` | 測試執行項目識別 | `uuid` | Y | N | 對應 `test_run_items.id`。 |
| `step_no` | 步驟序號快照 | `integer` | Y | N | 建立 Run 當下複製原始步驟序號。 |
| `action` | 操作內容快照 | `text` | Y | N | 建立 Run 當下複製原始步驟操作內容。 |
| `expected_result` | 預期結果快照 | `text` | Y | N | 建立 Run 當下複製原始步驟預期結果。 |
| `result_status` | 步驟執行結果 | `varchar(20)` | Y | N | 使用與 Test Run Item 相同的結果值。 |
| `actual_result` | 實際結果 | `text` | N | N | 記錄步驟實際觀察、錯誤訊息或阻塞原因。 |
| `executed_by_account_id` | 執行者識別 | `uuid` | N | N | 對應 `accounts.id`；尚未執行時可為空。 |
| `executed_at` | 執行時間 | `timestamp with time zone` | N | N | 填寫步驟最終結果時寫入。 |
| `audit_info` | 操作紀錄 | `-` | Y | N | 詳細結構請參考 [Audit Info 結構](./99-audit-metadata-fields.md)。 |

#### 補充規則

- `id` 一律使用 UUID。
- 建立 Test Run Item 時，必須依來源 Test Case 的步驟建立完整快照；後續不隨原始 Step 變動。
- `result_status` 初始值為 `not_run`，其值域與 `test_run_items.result_status` 相同。
- 每個 `test_run_item_id + step_no` 不得重複。
- 更新任一步驟後，應用層必須重新計算並保存案例層級結果；資料庫不使用 trigger 推導，避免把流程規則藏在 persistence layer。

#### Index 建議

- 建立 `idx_test_run_item_step_results_run_item_id` 於 `test_run_item_id`。
- 建立 `idx_test_run_item_step_results_item_result_status` 於 `test_run_item_id + result_status`。

#### 唯一約束建議

- 建立 unique constraint `uq_test_run_item_step_results_item_step_no` 於 `test_run_item_id + step_no`。

---

### test_case_attachments

保存 Test Case 附件中繼資料。主要欄位為 `id`、`test_case_id`、`uploaded_by_account_id`、`original_file_name`、`storage_provider`、`storage_key`、`content_type`、`file_size`、`file_hash`、`is_deleted`、`deleted_at` 與共通 audit 欄位。實體檔案不存入資料庫；刪除為軟刪除，下載與列表必須排除 `is_deleted = true`。建立 `idx_test_case_attachments_case_deleted` 於 `test_case_id + is_deleted`。

### test_run_item_attachments

保存 Test Run Item 的執行證據附件中繼資料，欄位語意與 `test_case_attachments` 相同，父外鍵改為 `test_run_item_id -> test_run_items.id`。只有 `in_progress` Run 可新增或移除；`completed` Run 完全唯讀。建立 `idx_test_run_item_attachments_item_deleted` 於 `test_run_item_id + is_deleted`。

## 待確認的自動化執行方向

自動化執行不屬於目前 MVP。本文件不定義 `test_repositories`、`test_environments`、`test_execution_agents`、`test_automation_bindings` 或 `test_execution_records` 的欄位。

確認自動化需求時，應先釐清：

- 自動化測試由誰觸發，以及是否仍需由 Test Plan 建立 Test Run。
- 測試程式來源、目標環境與執行服務如何設定及授權。
- 測試帳號、Token 與其他 Secret 的管理邊界。
- 自動化結果如何對應手動 Test Run Item，以及是否需要匯入外部結果格式。

## 設計順序

第一段依以下順序展開欄位與關聯：

1. `test_suites`
2. `test_cases`
3. `test_case_steps`
4. `test_tags`、`test_case_tags`
5. `test_plans`、`test_plan_items`
6. `test_runs`、`test_run_items`、`test_run_item_step_results`

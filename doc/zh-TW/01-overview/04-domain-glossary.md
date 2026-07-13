# 語詞總表

## 目的

這份文件用來統一 KhaiKang 的領域語詞。

目標是讓產品、設計、文件、開發與後續 AI 參與者在討論時使用同一套定義，避免相近詞混用。

這份文件目前只處理語意定義與中英文對照，不處理資料表、欄位名稱與 API 命名。

## 使用原則

- `系統定義名稱` 是系統內部的穩定標準詞
- `英文名稱` 是文件與技術討論的標準英文
- `中文名稱` 是繁體中文文件與介面的基準用語
- 若同一概念在不同情境容易混淆，應先回到這份文件確認定義
- 新增重要概念時，應優先補充這份文件，再延伸到 spec 或資料模型

## 語詞定義

| 系統定義名稱 | 領域 | 英文名稱 | 中文名稱 | 說明 | 備註 |
| --- | --- | --- | --- | --- | --- |
| `account` | identity | Account | 帳號 | 系統中的共用身份主體，可代表人類或 AI。 | 是所有身份的基礎概念。 |
| `human_account` | identity | Human Account | 人類帳號 | 由真人使用者操作的帳號。 | 與 AI 帳號區分。 |
| `ai_agent` | identity | AI Agent | AI 代理 | 系統中的 AI 執行身份。 | 為帳號的一種類型。 |
| `system_role` | identity | System Role | 系統角色 | 作用於整個系統層級的角色。 | 例如 `System Admin`。 |
| `project` | project | Project | 專案 | 系統中的主要工作容器。 | 第一階段不引入 Workspace。 |
| `project_member` | project | Project Member | 專案成員 | 已加入某個專案的帳號。 | 是帳號與專案的關係，不等於一般帳號本身。 |
| `project_role` | project | Project Role | 專案角色 | 只在特定專案內生效的角色。 | 與系統角色分開。 |
| `owner` | project | Owner | 專案擁有者 | 專案層級的最高權限角色。 | 只在該專案內有效。 |
| `manager` | project | Manager | 專案管理者 | 協助管理專案成員與日常專案運作的角色。 | 不等於系統管理者。 |
| `contributor` | project | Contributor | 參與者 | 主要負責建立、更新與推進工作項目的角色。 | 第一階段可調整處理人與狀態。 |
| `reviewer` | project | Reviewer | 審查者 | 協助檢視、確認與回饋工作項目的角色。 | 第一階段可協助更新 Issue。 |
| `issue` | issue | Issue | 議題 | 專案中的基本工作追蹤單位。 | 第一階段採 Kanban 式流程。 |
| `issue_type` | issue | Issue Type | 議題類型 | 用來區分不同性質的 Issue。 | 第一階段預設包含 `story`、`task`、`bug`、`spike`。 |
| `issue_status` | issue | Issue Status | 議題狀態 | 表示 Issue 目前所處的進度狀態。 | 第一階段先自由流轉。 |
| `issue_priority` | issue | Issue Priority | 議題優先級 | 表示 Issue 的處理優先程度。 | 第一階段先用系統預設值。 |
| `assignee` | issue | Assignee | 處理人 | 目前負責處理該 Issue 的帳號。 | 第一階段允許未指派。 |
| `reporter` | issue | Reporter | 建立人 | 建立該 Issue 的帳號。 | 由系統自動記錄。 |
| `status_created` | issue | Created | 已建立 | 表示 Issue 已建立，尚未進入處理中的狀態。 | 第一階段為預設初始狀態。 |
| `status_in_progress` | issue | In Progress | 處理中 | 表示 Issue 目前正在進行中。 | 第一階段可自由切入或切出。 |
| `status_verifying` | issue | Verifying | 驗證中 | 表示 Issue 正在驗證處理結果。 | 尚未視為完成。 |
| `status_completed` | issue | Completed | 已完成 | 表示 Issue 已完成。 | 第一階段的明確完成狀態。 |
| `kanban_flow` | workflow | Kanban Flow | Kanban 流程 | 第一階段專案管理採用的基本流程方式。 | 不先綁定 Sprint。 |
| `test_case` | test_management | Test Case | 測試案例 | 可重複使用的測試規格，定義要驗證的行為、前置準備、步驟與預期結果。 | 必須屬於一個測試工作區與一個測試套件。 |
| `test_workspace` | test_management | Test Workspace | 測試工作區 | 管理測試資產的高層容器。 | 不等同於 Project；MVP 作為測試資產根節點，後續再定義與 Project 的關聯。 |
| `test_step` | test_management | Test Step | 測試步驟 | Test Case 中一個可排序的操作步驟及其預期結果。 | 每個步驟可有自己的預期結果。 |
| `test_suite` | test_management | Test Suite | 測試套件 | 用於依功能或模組分類多個測試案例的樹狀邏輯集合。 | 可建立子套件；MVP 中一個測試案例只屬於一個套件。 |
| `test_tag` | test_management | Test Tag | 測試標籤 | 系統共用，標記測試案例的跨分類特性或測試目的。 | 例如 `smoke`、`regression`、`api`、`critical`；一個案例可有多個標籤。 |
| `test_plan` | test_management | Test Plan | 測試計畫 | 定義某個版本、需求或測試目的要執行的測試範圍與安排。 | 使用者手動編排案例範圍。 |
| `test_plan_item` | test_management | Test Plan Item | 測試計畫項目 | 使用者加入測試計畫的一個明確測試案例項目。 | 套件與標籤只用於查找和批次加入；建立後範圍固定。 |
| `test_run` | test_management | Test Run | 測試執行 | 依據一份測試計畫實際進行的一次測試活動與其結果報告。 | MVP 不另建 Test Report。 |
| `test_run_item` | test_management | Test Run Item | 測試執行項目 | 某次測試執行中，一個測試案例的個別執行紀錄與結果。 | 應保留當次案例內容快照。 |
| `test_result_status` | test_management | Test Result Status | 測試結果狀態 | 表示一個測試執行項目的結果。 | MVP 建議包含 `not_run`、`passed`、`failed`、`blocked`、`skipped`。 |
| `test_automation_binding` | test_management | Test Automation Binding | 自動化測試綁定 | 將一個 Test Case 與可執行的自動化測試連結。 | MVP 先支援 Playwright；綁定是可選的，不影響手動測試。 |
| `playwright_test` | test_management | Playwright Test | Playwright 自動化測試 | 透過 Playwright 執行 API 測試或瀏覽器端對端測試。 | MVP 唯一與 Test Case 綁定的自動化測試工具。 |
| `test_environment` | test_management | Test Environment | 測試環境 | 被測系統所在的目標環境。 | 例如 SIT URL、測試帳號或 API Base URL；不等於執行測試的機器。 |
| `test_execution_agent` | test_management | Test Execution Agent | 測試執行代理 | 真正執行自動化測試並回傳結果的服務、VM 或容器。 | MVP 預設為 Playwright Agent。 |
| `test_repository` | test_management | Test Repository | 測試程式庫 | 保存自動化測試程式與設定的程式庫來源。 | 執行時應指定 repository ref 或 commit。 |

## 後續補充原則

- 若新語詞只是現有概念的顯示別名，應優先沿用既有詞，不新增新的 `系統定義名稱`
- 若新語詞代表新的權限邊界、業務物件或流程節點，應新增正式定義
- 若中文名稱與英文名稱容易產生歧義，應在 `備註` 補充使用邊界

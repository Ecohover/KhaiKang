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
| `status_open` | issue | Open | 開啟 | 表示 Issue 已建立，尚未進入處理中的狀態。 | 第一階段為預設初始狀態。 |
| `status_in_progress` | issue | In Progress | 處理中 | 表示 Issue 目前正在進行中。 | 第一階段可自由切入或切出。 |
| `status_resolved` | issue | Resolved | 已處理 | 表示 Issue 已被處理，但仍可能再調整。 | 不一定等於最終完成。 |
| `status_closed` | issue | Closed | 已完成 | 表示 Issue 已完成。 | 第一階段視為完成狀態。 |
| `kanban_flow` | workflow | Kanban Flow | Kanban 流程 | 第一階段專案管理採用的基本流程方式。 | 不先綁定 Sprint。 |

## 後續補充原則

- 若新語詞只是現有概念的顯示別名，應優先沿用既有詞，不新增新的 `系統定義名稱`
- 若新語詞代表新的權限邊界、業務物件或流程節點，應新增正式定義
- 若中文名稱與英文名稱容易產生歧義，應在 `備註` 補充使用邊界

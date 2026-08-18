# Agent Integration 文件地圖與恢復交接

## 文件狀態與邊界

- 狀態：Investigation／文件收斂；不是 Product Specification、資料模型或實作授權。
- 建立日期：2026-08-17。
- 適用方向：未來經人類授權後，讓 AI 開發工作流可在 KhaiKang 記錄或使用既有 Project、Test Plan、Test Run 資訊。
- 本文件不決定 Agent Token、credential、scope、資料表、商業語意或自動化流程；未取得確認規格前，不得建立開發、QA 或部署工作。

## 恢復前先讀的權威來源

| 目的 | 權威來源 | 目前可確認的界線 |
| --- | --- | --- |
| 產品與架構方向 | [AI Agent Orchestration 設計方向](./04-ai-agent-orchestration-direction.md) | Direction，明確要求先有正式 Spec 與資料模型；未授權建立資料表或 Runner。 |
| 現行測試管理產品規格 | [測試案例管理規格](../02-specs/03-test-case-management.md) | MVP 為手動 Test Suite／Case／Plan／Run；不含 CI 匯入、Agent 觸發、Playwright 執行、結果匯入或測試憑證。 |
| 現行測試管理資料模型 | [測試案例管理資料模型](../03-data-model/03-test-case-management-data-model.md) | Plan 是固定案例範圍，Run 是快照與手動結果的歷史紀錄；自動化相關表僅列為未定義的未來方向。 |
| Project 與 AI 預留模型 | [專案管理資料模型](../03-data-model/02-project-management-data-model.md) | `ai_agents` 僅保留位置，尚無欄位；Project membership／role 是既有 scope 模型，不能自行推演成 Agent 授權模型。 |
| 身分與安全產品規格 | [使用者與登入規格](../02-specs/01-user-authentication.md) | 已定義 `human` 與 `ai_agent` 帳號類型及 audit 原則；未定義 Agent token 發放、輪替、撤銷或 scope。 |
| 身分資料模型 | [登入與使用者資料模型](../03-data-model/04-authentication-data-model.md) | 現有 audit 不保存 password、cookie、token 或完整敏感 payload；此限制應作為未來規格輸入。 |
| 公開 HTTP 契約 | [OpenAPI `khaikang.v1.yaml`](../../../contract/openapi/khaikang.v1.yaml) | 唯一公開 wire contract；目前以 `KhaiKang.Session` cookie 與 `X-XSRF-TOKEN` CSRF header 保護，未定義 Agent token security scheme 或 Agent Integration operation。 |
| API 演進規則 | [AI 與 OpenAPI 協作開發流程](../01-overview/06-ai-openapi-development-workflow.md) | 未來任何 HTTP 變更須先有功能規格與 OpenAPI，並同步後端、前端與合約測試。 |
| Test Management 實作進度 | [測試管理 MVP 實作計畫](./05-test-management-implementation-plan.md) | 此計畫描述手動 MVP 的實作順序；不構成 Agent Integration 的需求或授權。 |
| 後端重構工作狀態 | [後端可讀性重構執行追蹤](./11-backend-refactoring-execution-tracker.md) | P04 Result／Outcome 為 active principle；Tracker 的 Resume Point 是重構恢復的唯一工作狀態索引。 |

若來源衝突，依規格、資料模型、OpenAPI、測試、目前程式與 Git 實況查明差異；不得由 Agent 靜默選擇或修補。

## 已有能力與明確缺口

| 範圍 | 已有能力（盤點） | 未確認或不存在的 Agent Integration 能力 |
| --- | --- | --- |
| Project | 已有 Project、member、role 與 Project-scoped permissions 的產品與 OpenAPI 基礎。 | Agent 可否讀取、建立或變更哪一類 Project 資料，及其是否沿用 membership，均未決。 |
| Test Plan | Workspace 內可建立、更新、排序並以 `test_plan_items` 保存固定案例範圍。 | Agent 能否建立／選擇 Plan，或只讀取其範圍，未決。 |
| Test Run | 可由 active Plan 建立 Run、保存案例與步驟快照，並記錄手動執行結果。 | Agent 觸發、執行、結果匯入、覆核、失敗／阻塞語意及與手動 Run 的關係，均未決。 |
| Identity 與 API 安全 | Web 本機帳號、session cookie、CSRF 與 `ai_agent` 帳號概念已存在。 | 沒有已規格化的 Agent Token、machine-to-machine authentication、token scope、credential storage、rotation、revocation 或 runtime identity mapping。 |
| Agent orchestration | 已有 Control Plane／Runtime Provider、Execution snapshot、Approval、Audit、Budget、durable state 的方向。 | 沒有正式 entity、流程、OpenAPI、migration、前端或 runtime 實作；Direction 不是可直接實作的模型。 |

「可在 KhaiKang 記錄資訊」與「可由 Agent 寫入、執行或代表人類操作」是不同產品能力；本文件不把前者推論為後者。

## 與既有後端重構的隔離

本工作只新增本文件及文件導覽連結，不修改 `backend/`、`contract/`、migration、測試或 [重構執行追蹤](./11-backend-refactoring-execution-tracker.md)。

恢復重構時，先以 live Git 與 Tracker Resume Point 比對。2026-08-17 的唯讀盤點觀察到分支為 `ecohover/refactor/backend-clean-code`，且工作樹已有未 checkpoint 的 Project Management P04 相關修改與新增檔案；它們是既有工作，不得混入 Agent Integration 的需求、測試或 commit。Tracker 記錄的下一步仍是補完 C17-1 的 Issue、Attachment 與 Test Management reachable outcome／HTTP mapping matrix，再選擇一個 P04 vertical slice。

本文件不改變 P04、任何 C-item、MVP release 或 workflow phase 的狀態，也不宣告任何 Gate 或 checkpoint 完成。

## 需由人類先確認的產品決策

在開始正式 Product Specification 前，至少需逐項確認：

1. 首版目標是「只讀查詢／記錄連結」、可建立 Project／Plan／Run，還是包含觸發與回寫自動化結果？
2. Agent 的身份模型是既有 `ai_agent` account、獨立 machine identity，或兩者；其與人類、Project member、Workspace member 的關係是什麼？
3. 是否需要 Agent Token；若需要，誰可發放、scope 到何處、有效期、rotation、revocation、secret 儲存與 audit 的規則為何？
4. Agent 對 Project、Test Workspace、Test Plan、Test Run 的最小權限各是什麼？是否可代表人類操作，或必須是獨立 actor？
5. Agent 建立的 Run 是沿用手動 Run、獨立 Run 類型，或需要另一個 Execution／Evidence 模型？既有快照、completed immutability 與結果狀態如何適用？
6. 自動化範圍是否包含 Repository、Environment、Playwright／CI、Runner、憑證與外部結果格式；哪一項進首版？
7. 哪些操作必須等待人工 approval，哪些操作永遠不得自動執行（例如 commit、push、merge、deploy）？
8. 需要保留哪些可重現與稽核資訊：Agent／Role／Prompt version、context、tool policy、approval、revision、驗證證據、成本與停止原因？
9. 成功、失敗、blocked、取消與人工覆核在業務上各代表什麼，誰能判定或覆寫？

## 正式規格前的恢復交接

- Task：釐清「授權後的 AI 開發工作流如何使用或記錄 KhaiKang Project／Test Plan／Test Run」的產品需求。
- Phase：Product Specification 前的 Investigation／文件收斂；尚未進入 Major Phase 1。
- Source-of-Truth：本文件的文件地圖，以及上表連結的功能規格、資料模型、OpenAPI、重構 Tracker 與 live Git。
- Scope：收斂人類已確認的產品語意、安全邊界、授權、驗收條件與非目標。
- Non-Goals：不實作 token、scope、資料表、endpoint、Agent Runtime 或自動化執行；不修改或 checkpoint 既有 P04 重構。
- Produced Artifact：本文件與文件導覽連結。
- Evidence：2026-08-17 已唯讀檢閱上述文件、OpenAPI security scheme／Project／Test Management routes，以及 live Git branch／status。
- Open Decisions：前節九項均待人類決定。
- Next Role and Required Action：人類 PO 與 PM AI 先確認決策；僅在確認完整 Product Specification 後，PM AI 才建立正式 Spec 與 Handoff Package，交給 Parallel Design 的 QA AI／Development AI。此前不得派發實作。

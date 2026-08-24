# Comment、追溯與 AI 操作紀錄能力：QA Cases Handoff

## 文件狀態

- 角色：QA AI
- 來源交接：PM Parallel Design QA Cases Handoff（2026-08-20）
- 目前階段：`Parallel Design`
- 指定分支：`ecohover/feature/20260820`
- 適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`。
- Matt Pocock method：`tdd` 的 Behavior-First／public seam 方法。原因：本工作把確定的規格行為拆成可在 HTTP、PostgreSQL 與前端互動邊界驗證的案例；本階段只完成 test design，不寫測試程式。

## Product Source of Truth

唯一產品來源為 `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`。本文件的每個 QA case 均直接對應其條款；Handoff Package 與 AI Skill 整合文件僅補充 workflow 與交付格式，不用來新增、縮減或改寫產品語意。遇到該 Product Specification 未定義的行為，QA 記錄為 PM／Human PO decision，不自行假設或建立案例。

## Phase Boundary

本 artifact 僅包含由已確認 Product Specification 推導的 QA cases，及受影響 legacy regression selection。

- 本階段不實作或執行 QA automation。
- 本階段不執行 Formal QA、runtime 測試或 bug disposition。
- `Expected Red` 僅可能在 PM 授權的 `Parallel Implementation` 中由新 QA automation 標記；目前沒有 Expected Red 結果。
- 下一閘門：PM 完成 Artifact Review 並發出下一份 Handoff 後，QA 才能進入後續階段；Formal QA 仍須等 Implementation Review 完成及 PM Formal QA Run Handoff。

## QA Cases

| ID | Product Specification 條款／範圍 | 案例與預期結果 | Development-owned test seam | 未來 QA automation seam | 未來 Formal QA evidence |
| --- | --- | --- | --- | --- | --- |
| CMT-01 | §2 支援資源；§4 Comment API；§12 Backend/API | Issue、Test Case、Test Plan、Test Run 均可依授權建立／查詢 Comment；未列白名單類型與偽造 `resource_type` 均拒絕，且不洩漏資源資訊。 | Comment HTTP API 的白名單與 Problem Details。 | 四類資源頁的建立／查詢流程。 | 四類資源的請求／回應與拒絕證據。 |
| CMT-02 | §4 Comment API；§12 Backend/API | Comment 查詢沿用資源 `read`；建立要求 `comment.create`；缺少任一必要授權、跨資源邊界或偽造授權資訊均被拒絕。 | HTTP authorization 與資源授權整合。 | 不同身分在資源頁的可讀／不可建立狀態。 | 身分矩陣、回應狀態與無資料洩漏證據。 |
| CMT-03 | §3 Comment 資料模型；§4 Comment API；§9 AI 身分與權限；§12 Backend/API | 呼叫端偽造作者、時間、`accountType` 不生效；human 與 `ai_agent` 留言皆回傳伺服器依登入身分產生的作者、時間、AI／人員辨識資訊。 | 建立 Comment 的 HTTP／持久化邊界。 | 作者、角色、AI／人員標記的呈現。 | 回應與持久化資料比對。 |
| CMT-04 | §3 Comment 資料模型；§4 Comment API；§5 Comment 前端元件；§12 Backend/API、Frontend | 留言為平面序列、最新在上；分頁無重複或遺漏；前端只顯示上一頁、下一頁與目前頁次，不顯示 page size 或總筆數。 | 分頁、排序 Comment API。 | 共用 Comment 元件的簡化分頁。 | API page payload 與代表性頁面截圖。 |
| CMT-05 | §6 Markdown 與系統設定；§12 Backend/API、Frontend | 驗證全系統 Comment options 的安全預設：允許標題、清單、表格、程式碼與一般 URL；禁止原始 HTML、Script、外部嵌入；10,000 字元可接受、超限被前後端拒絕；不提供專案或單則留言覆寫。 | Markdown／長度驗證與 Comment options 套用。 | 編輯／預覽與安全呈現。 | 允許／拒絕 Markdown 樣本、長度邊界與跨資源一致性證據。 |
| CMT-06 | §3 Comment 資料模型；§4 Comment API；§12 Backend/API | 同一帳號、操作類型、冪等鍵的建立／重送回傳原結果，且不新增重複 Comment；不同組合不被錯誤合併。 | Comment 建立的冪等／併發持久化邊界。 | 重送後仍只顯示一筆留言。 | 回應 ID、資料筆數與重送紀錄。 |
| CMT-07 | §5 Comment 前端元件；§12 Frontend | 四種支援資源共用同一 Comment 元件；新增成功後回到第 1 頁並顯示新留言；提供置頂 Markdown 編輯／預覽。 | 不適用（前端共用元件為主）。 | 元件在四類資源的使用與新增後頁次。 | 代表性瀏覽器互動與元件引用證據。 |
| CMT-08 | §5 Comment 前端元件；§12 Frontend | Comment 載入顯示讀取狀態；失敗最多自動重試 3 次，仍失敗顯示指定忙碌訊息與「重新讀取」；主資源畫面維持可用。 | 不適用（前端錯誤隔離為主）。 | 可控制失敗回應下的重試、重新讀取及主畫面互動。 | 網路失敗序列與代表性頁面證據。 |
| CMT-09 | §10 軟刪除與恢復；§12 Backend/API | 資源軟刪除後 Comment／關聯保留且不另行軟刪除；恢復後重新可見；無 Comment 硬刪除能力。 | 軟刪除／恢復的 HTTP、持久化邊界。 | 資源恢復後的 Comment 可見性。 | 刪除、恢復前後 API 與資料保留證據。 |
| TRC-01 | §7 追溯關聯；§12 Backend/API | Test Case 僅能有一個原始 Test Issue，可查詢且可被多個 Test Plan 使用；關聯白名單與建立／解除冪等成立。 | Traceability HTTP／PostgreSQL 關聯邊界。 | Test Case 來源 Test Issue 的顯示與解除。 | 建立、重送、解除後的雙向關聯證據。 |
| TRC-02 | §7 `IssueParentChild`；§12 Backend/API | User Story 與 Task／Test Issue 可依既有 `parent_of` 雙向查詢；固定關聯類型不可任意建立。 | 既有 Issue relation HTTP 邊界。 | 關聯正反向顯示。 | 關聯建立與雙向查詢結果。 |
| TRC-03 | §7 `BugFoundByTestIssue`、`BugFoundInTestRun`、`BugFoundInTestRunItem`；§12 Backend/API | Bug 可建立、查詢、解除其 Test Issue、Test Run、Test Run Item 來源；全部具冪等性，且 Test Run Item 關聯不改寫 Run 歷史。 | Traceability HTTP／PostgreSQL／併發邊界。 | Bug 來源關聯的顯示與解除。 | 三種來源的雙向查詢、重送、解除與 Run 歷史比對。 |
| TRC-04 | §7 關聯解除；§8 Test Plan 與 Test Run | 解除關聯只解除該關聯，不刪除資源或歷史 Test Run 結果。 | 關聯解除持久化邊界。 | 解除後的關聯區塊更新。 | 解除前後資源與 Run 歷史比對。 |
| SNAP-01 | §8 Test Plan 與 Test Run；§12 Backend/API | Run 固定建立時的 Plan 案例／測試範圍快照；後續 Plan 新增、移除或變更案例／範圍，不改寫既有 Run。 | Run 建立與 snapshot 持久化邊界。 | Plan 變更後既有 Run 的歷史呈現。 | 建立前後 Plan／Run payload 與資料比對。 |
| AI-01 | §9 AI 身分與權限；§11 非目標；§12 Backend/API、Frontend | AI 帳號沿用 `ai_agent` 與既有角色／權限；API／前端可辨識 AI 標記；不新增 AI 專用權限、帳號管理 UI、Token 管理 UI、workflow 狀態或特殊 Comment type。 | 帳號類型與既有授權邊界。 | AI 作者／指派人標記呈現。 | AI 與 human 帳號的授權、回應與標記證據。 |

## Coverage Classification

| 覆蓋類型 | QA Cases |
| --- | --- |
| 正向行為 | CMT-01、CMT-03、CMT-04、CMT-05、CMT-06、CMT-07、CMT-09、TRC-01、TRC-02、TRC-03、TRC-04、SNAP-01、AI-01 |
| 負向／拒絕 | CMT-01、CMT-02、CMT-03、CMT-05、CMT-06、CMT-09、TRC-01、TRC-02、TRC-03、AI-01 |
| 授權／跨邊界 | CMT-01、CMT-02、CMT-03、TRC-01、TRC-02、TRC-03、AI-01 |
| 持久化／併發 | CMT-03、CMT-04、CMT-06、CMT-09、TRC-01、TRC-03、TRC-04、SNAP-01 |
| 前端互動／錯誤隔離 | CMT-04、CMT-05、CMT-07、CMT-08、CMT-09、TRC-01、TRC-02、TRC-03、TRC-04、SNAP-01、AI-01 |

## Legacy Regression Selection

這些既有測試保護本切片延伸或依賴的追溯不變量；未來的 Formal QA Run 應將其納入回歸範圍，但本階段不執行。

| Regression ID | 既有測試 | 選取原因 | 對應 QA Cases |
| --- | --- | --- | --- |
| REG-01 | `IssueTestTraceabilityEndpointsTests.TraceabilityFlow_LinksRequirementsAndSnapshotsPlanTestIssueIntoRun` | 保護既有 Case→需求、Plan→Test Issue、Run snapshot、Run→Bug 與關聯解除主路徑，避免新追溯關聯破壞既有追溯。 | TRC-02、TRC-04、SNAP-01 |
| REG-02 | `TraceabilityValidationTests.LinkRequirement_RequiresLinkedActiveProject` | 保護 Workspace–Project 有效關聯與停用 Project 的拒絕行為，為新 Test Case 來源關聯的既有授權邊界。 | CMT-02、TRC-01、TRC-03 |
| REG-03 | `TraceabilityValidationTests.PlanAndRunBug_RequireLinkedActiveProjectAndValidIssueOptions` | 保護 Plan／Run 建立 Bug 的有效 Project、Issue 選項與錯誤契約。 | TRC-03、SNAP-01 |
| REG-04 | `TestPlanRunBehaviorTests.UpdatePlan_WithStaleVersionOrEmptyActiveScope_RejectsMutation` | 保護 Plan 版本衝突及 active scope 不變量，直接關聯 Run snapshot。 | SNAP-01 |
| REG-05 | `TestPlanRunBehaviorTests.CreateRun_FromDraftPlanOrPlanWithInactiveCase_ReturnsConflict` | 保護 Run 建立時 Plan 與 Case 可執行性的既有邊界。 | SNAP-01 |
| REG-06 | `TestPlanRunBehaviorTests.Run_EnforcesVersionsCompletionAndTerminalRerunRules` | 保護 Run、Item、Step 的版本與完成規則，降低新增 Bug→Run Item 關聯破壞歷史 Run 行為的風險。 | TRC-03、SNAP-01 |

無適用 Comment legacy regression：現有程式沒有已實作的 Comment 能力或其測試；Comment 案例均為本次 Product Specification 新增範圍，不能以不相關舊測試替代。

## PM Artifact Review Handoff

- **Task / Feature**：Comment、追溯與 AI 操作紀錄能力。
- **Current Phase**：`Parallel Design`。
- **唯一 Product Source-of-Truth Path**：`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`。
- **Produced Artifact**：本 QA Cases Handoff（CMT-01 至 AI-01；REG-01 至 REG-06）。
- **Scope / Non-Goals**：只提供規格推導案例與 legacy regression selection；未實作或執行 automation、Formal QA、runtime 測試，未進行 bug disposition，未修改產品程式。
- **Open Decisions / Blockers**：Product Specification 未定義的行為，一律由 PM AI 與 Human PO 決策；本 QA artifact 不自行補充。
- **Next Role and Required Action**：PM AI 對 cases 與 regression selection 完成 Artifact Review；QA 等待下一份 Handoff。

# Comment、追溯與 AI 操作紀錄能力：QA Cases

## 文件狀態

- 角色：QA AI
- 目前階段：`Parallel Design`
- 來源交接：`doc/plans/2026-08-20-comment-traceability-ai-capability-parallel-design-handoff.md`
- 唯一產品規格：`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`
- 適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`

本文件只設計案例、風險與既有回歸選擇。尚未實作 automation、執行測試、啟動 runtime 或進行 Formal QA。

## QA Cases

| ID | 規格 | 案例與預期結果 | 未來驗證層 |
| --- | --- | --- | --- |
| CMT-01 | §2、§4 | 只有 Issue（User Story、Task、Test Issue、Bug）、Test Case、Test Plan、Test Run 可讀取或建立 Comment；未列白名單的 `resource_type` 一律拒絕且不洩漏資源資訊。 | HTTP/API、前端 |
| CMT-02 | §4 | Comment 查詢沿用資源 read 權限，建立還必須具備 `comment.create`；跨 Project／Workspace、缺少任一權限或偽造資源權限資訊均拒絕。 | HTTP/API、授權整合 |
| CMT-03 | §3、§4、§9 | human Web session 與 AI Bearer API 建立 Comment 時，作者、建立時間、`accountType`、操作來源均由伺服器登入身分決定；呼叫端提供的作者、時間或身分值不得覆寫結果。 | HTTP/API、持久化 |
| CMT-04 | §3、§4 | Comment 為平面序列，沒有 type、reply、nested branch、edit 或 delete 能力；嘗試對既有 Comment 編輯／刪除或偽造其 API 均被拒絕。 | HTTP/API、前端 |
| CMT-05 | §4、§5 | 查詢順序固定為 `created_at DESC, id DESC`；跨頁邊界無重複或遺漏，排序相同時間仍穩定。 | API、持久化／併發 |
| CMT-06 | §4、§5 | 同帳號、同操作類型、同 idempotency key 的相同建立請求重送時回傳原結果且只產生一筆；不同帳號或操作類型不得誤共用結果。 | HTTP/API、PostgreSQL |
| CMT-07 | §4 | 同一帳號、操作類型與 idempotency key 若改變目標、payload 或 version，回傳 idempotency conflict；不得建立新資料，也不得靜默回傳舊結果。 | HTTP/API、PostgreSQL |
| CMT-08 | §5 | 四種支援資源頁都使用同一 Comment 元件，置頂提供 Markdown 編輯／預覽；成功新增後回到第 1 頁並顯示新 Comment。 | 前端元件／瀏覽器 |
| CMT-09 | §5 | Comment 載入獨立於主資源畫面：載入中有圖示；失敗最多自動重試 3 次，之後顯示指定忙碌訊息及「重新讀取」；寫入失敗不背景重送，使用者以原 idempotency key 明確重試。 | 前端互動／網路故障 |
| CMT-10 | §5 | Comment 分頁只顯示上一頁、下一頁與目前頁次；不得出現 Page Size 選單或總筆數。 | 前端元件／瀏覽器 |
| CMT-11 | §6 | 全系統 Markdown 設定預設允許標題、清單、表格、程式碼與一般 URL；拒絕 raw HTML、Script、`data:`、檔案路徑、外部圖片與外部嵌入。圖片只接受已上傳附件，連結只接受 http(s) 或授權附件。 | API、前端安全呈現 |
| CMT-12 | §6 | Comment 內容剛好 10,000 Unicode 字元可接受，超限在前端與後端都拒絕；系統管理者修改的全系統設定一致套用，沒有 Project 或單 Comment 覆寫。 | 前端、HTTP/API |
| CMT-13 | §3、§10 | 資源軟刪除後 Comment 與關聯資料仍保存但一般 API／前端均不可見、不可新增或解除，且其他資源不得洩漏被刪資源名稱或 ID；恢復後原有資料重新可見。 | HTTP/API、持久化、前端 |
| TRC-01 | §7 | `IssueParentChild` 只允許 User Story 為父、Task 或 Test Issue 為子，使用既有 `parent_of`；不允許自訂關聯類型、錯誤父子類型或其他白名單外組合。 | HTTP/API、PostgreSQL |
| TRC-02 | §7 | Test Case 可無來源；若建立 `TestCaseGeneratedByTestIssue`，只可有一個原始 Test Issue，之後不可變更或解除。 | HTTP/API、PostgreSQL、併發 |
| TRC-03 | §7 | Bug 的 `BugFoundByTestIssue`、`BugFoundInTestRun` 各最多一個，建立後均不可解除；缺少來源的 Bug 仍依規格允許或限制的組合驗證。 | HTTP/API、PostgreSQL |
| TRC-04 | §7 | `BugFoundInTestRunItem` 可無來源；若有，最多一個且必須屬於同一來源 Test Run，建立後不可解除，且不改寫既有 Run 歷史。 | HTTP/API、PostgreSQL、併發 |
| TRC-05 | §7 | 一般 `IssueParentChild` 與 `tests` 關聯只有在 PM AI 已獲授權且具權限時可建立、解除或調整；解除只解除關聯、不刪除資源。不可變來源關聯不適用此規則。 | HTTP/API、授權 |
| SNAP-01 | §8 | Test Plan 不繼承其他計畫；從 Plan 移除 Test Case 只解除收納關係、不刪除 Case。 | HTTP/API、持久化 |
| SNAP-02 | §8 | 建立 Test Run 時固定當下 Test Plan 的案例與範圍；之後新增、移除或修改 Plan 不得回寫既有 Run。 | HTTP/API、PostgreSQL |
| AI-01 | §9 | 只有 `ai_agent` 帳號可啟用 AI API；human 帳號不得啟用。AI 名稱必為全域唯一 `ai-` 前綴，建立者成為唯一真人擁有者，且僅可選固定五種角色之一。 | HTTP/API、持久化、前端 |
| AI-02 | §9 | AI 帳號同時依固定角色、擁有者可讀的 Project／Test Workspace 範圍與每次操作授權判斷；跨範圍讀寫必拒絕。 | HTTP/API、授權整合 |
| AI-03 | §9 | 建立時強初始密碼只向建立者顯示一次；AI 帳號可依一般帳號流程 Web 登入。擁有者不存在、移除或停用時成為 orphan 並立即禁止 Web／API 使用，歷史仍保留。 | HTTP/API、前端、持久化 |
| AI-04 | §9 | AI API flag 預設關閉，只有擁有者或管理員可啟停；啟用後只有一組永久 token，重建立即使舊 token 失效。 | HTTP/API、持久化 |
| AI-05 | §9、§11 | token 僅能以 `Authorization: Bearer` 呼叫 API，不可登入 Web，且不使用 cookie／CSRF；缺失、已撤銷、重建後舊 token、轉移後舊 token 或 orphan 帳號的 token 均拒絕。本階段不驗證 token expiry／rotation。 | HTTP/API、安全整合 |
| AI-06 | §9 | 擁有者可查看／複製 token；管理員可啟停、撤銷、轉移但不可讀取既有 token 明文。token 不得出現在 log、Comment 或 audit 明文；建立、重建、查看／複製、撤銷與轉移都有不含明文的 audit event。 | HTTP/API、安全／稽核 |
| AI-07 | §3、§9 | 管理員轉移 AI 擁有者後，既有 token 立即失效；歷史 Comment 與資源操作仍保留操作當下的 AI 名稱、固定角色及真人負責人快照，不因轉移或改名回寫。 | HTTP/API、PostgreSQL |
| AI-08 | §3、§5、§9 | API 與前端在帳號、Comment 作者及資源操作歷程一致呈現 AI／human 標記、操作當下角色與真人負責人；主資源業務欄位不新增 AI flag。 | HTTP/API、前端 |

## 風險矩陣

| 風險 | 等級 | 失敗影響 | 必要證據 |
| --- | --- | --- | --- |
| token 外洩、錯誤 bearer/session 邊界或管理員讀到明文 | Critical | AI 身分遭冒用或機密外洩 | HTTP 拒絕案例、audit／log 無明文檢查、token 失效證據 |
| AI 固定角色／owner／Project／Workspace 授權未交集檢查 | Critical | 越權讀寫跨範圍資源 | 角色 × owner 範圍 × 資源範圍矩陣 |
| idempotency 或並發錯誤 | High | 重複 Comment／關聯或靜默錯誤結果 | 重送、衝突 key、並發持久化證據 |
| 不可變來源關聯可被解除或改寫 | High | 測試／Bug 歷史證據失真 | 建立後 mutation 拒絕、Run 歷史比對 |
| 軟刪除洩漏 Comment 或關聯 | High | 已刪資源資訊曝光 | 刪除／恢復前後 API 與前端證據 |
| Markdown sanitization 不完整 | High | XSS、外部內容或本機路徑曝光 | 允許／拒絕 payload 與安全呈現證據 |
| 排序、分頁、重試或寫入行為不符 | Medium | 使用者誤判結果或重複操作 | 瀏覽器互動、網路失敗序列與 API payload |
| Run snapshot 或 Plan 移除行為回歸 | Medium | 歷史測試範圍遭改寫 | Plan／Run 前後持久化資料比對 |

## Legacy Regression Selection

下列既有 integration tests 是受新追溯切片影響的回歸保護；僅列入未來 Formal QA Run，本階段未執行。Comment 與 AI token 為新能力，現有測試中無可替代的 legacy regression。

| ID | 既有測試 | 選取理由 | 關聯案例 |
| --- | --- | --- | --- |
| REG-01 | `IssueTestTraceabilityEndpointsTests.TraceabilityFlow_LinksRequirementsAndSnapshotsPlanTestIssueIntoRun` | 保護既有關聯、Plan Test Issue 與 Run snapshot 主流程。 | TRC-01、SNAP-02 |
| REG-02 | `TraceabilityValidationTests.LinkRequirement_RequiresLinkedActiveProject` | 保護有效 Workspace–Project 關聯與停用 Project 的拒絕。 | TRC-01、TRC-05 |
| REG-03 | `TraceabilityValidationTests.PlanAndRunBug_RequireLinkedActiveProjectAndValidIssueOptions` | 保護 Plan／Run 建立 Bug 的範圍與選項驗證。 | TRC-03、TRC-04 |
| REG-04 | `TestPlanRunBehaviorTests.UpdatePlan_WithStaleVersionOrEmptyActiveScope_RejectsMutation` | 保護 Plan 版本與 active scope 不變量。 | SNAP-01、SNAP-02 |
| REG-05 | `TestPlanRunBehaviorTests.CreateRun_FromDraftPlanOrPlanWithInactiveCase_ReturnsConflict` | 保護 Run 建立時 Plan／Case 的有效狀態。 | SNAP-02 |
| REG-06 | `TestPlanRunBehaviorTests.Run_EnforcesVersionsCompletionAndTerminalRerunRules` | 保護 Run、Item、Step 的版本與終態規則。 | TRC-04、SNAP-02 |

## PM Handoff Package

- **Task / Feature**：Comment、追溯與 AI 操作紀錄能力。
- **Current Phase**：`Parallel Design`；QA cases 已完成，未推進至 Artifact Review 或其後階段。
- **Source-of-Truth Paths**：`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`；既有對照規格為 `doc/zh-TW/02-specs/01-user-authentication.md`、`doc/zh-TW/02-specs/04-issue-test-traceability.md`、`doc/zh-TW/03-data-model/05-issue-test-traceability-data-model.md`。
- **Scope / Non-Goals**：只產出 QA case design、risk matrix 與 legacy regression selection；未修改產品程式、Migration、測試程式或既有 dirty changes，未執行 automation、Formal QA、runtime、commit、push 或 deploy。
- **Produced Artifact**：本文件；共 28 個案例（CMT-01～CMT-13、TRC-01～TRC-05、SNAP-01～SNAP-02、AI-01～AI-08）及 REG-01～REG-06。
- **Validation Evidence**：已逐條以唯一產品規格 §2～§12 比對；已以 `rg` 確認 REG-01～REG-06 的既有 integration-test 檔案與方法名稱存在。未執行 build、test 或 runtime。
- **Open Decisions / Blockers**：工作區既有未追蹤檔 `doc/plans/2026-08-20-comment-traceability-ai-capability-qa-verification-matrix.md` 將 Test Case／Bug 的不可變來源關聯寫為可解除，與唯一產品規格 §7 衝突。本 QA artifact 已依規格設計，未覆寫該他人檔案。PM 必須在 Artifact Review 決定採用／更正／移除衝突 artifact，才可將 QA cases 作為後續 automation 基準。另，產品規格要求 token 的建立、重建、查看／複製、撤銷、轉移與稽核，但未要求獨立的 token 管理 UI；是否重用既有帳號管理介面由 Development Specification 說明，不是 QA 的產品決策。
- **Next Role and Required Action**：PM AI 先審核本 QA artifact 並處理上述衝突；依流程將 Development Specification 送 Reviewer AI 進行 Artifact Review。QA AI 等待 PM 的下一份明確 Handoff，才可在 `Parallel Implementation` 實作 automation。

# Development Specification：Comment、追溯與 AI 操作紀錄能力

## 文件狀態與邊界

- Workflow Role：Development AI
- Major Phase：Parallel Design
- 依據：[正式 Parallel Design Handoff](2026-08-20-comment-traceability-ai-capability-parallel-design-handoff.md) 與已確認的[產品規格](../zh-TW/02-specs/05-comment-and-ai-recording-capability.md)。
- 適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`typescript-vue`、`http-contract-quality`；實作時遵循 Coding Style Baseline 的 Testable Core、Dependency Rule 與 Risk-Based Verification。
- 本文件不授權產品程式、OpenAPI、Migration、測試實作、commit、push、部署或階段推進。

工作樹另有同名未追蹤設計稿。為保留既有 dirty changes，本文件不覆寫該稿；本文件是本次 Development AI 交付給 PM／Reviewer 的獨立 artifact。

## 模組與資料所有權

| 能力 | Owner module | 邊界與責任 |
| --- | --- | --- |
| AI 帳號、固定角色、owner、API flag、token、token 稽核 | Identity | Identity 是 `accounts` 與全域帳號安全狀態的唯一 owner；不得由 Project／Test／Comment 模組直接寫入其資料表。 |
| Comment、Comment 操作來源快照、Comment idempotency | 新增 `KhaiKang.Modules.CommentManagement` | 擁有獨立 `comments` 與 `comment_idempotency_records` 資料、Controller／Application Service；僅藉由明確 application contracts 詢問各資源 owner 的最小 access verdict。 |
| Issue 與 `IssueParentChild` | ProjectManagement | 延伸既有固定 relation catalog；只接受規格白名單與合法 Issue 類型。 |
| Test Case／Plan／Run 與 Bug 來源追溯 | TestManagement | 擁有 Test Workspace、Case、Plan、Run、Run Item 與其 trace links；藉 explicit Issue directory contract 驗證 Issue 所屬 Project、type 與權限，不能直接讀取 ProjectManagement DbContext。 |

`CommentManagement` 不可依賴 ProjectManagement 或 TestManagement 的 Entity、DbContext、Controller 或 Infrastructure type。每個 resource owner 提供 feature-local implementation 給下列 inward-facing contract：

```text
CommentResourceAccess.Request(resourceType, resourceId, authenticatedAccount, intent)
  -> ExistsAndActive, CanRead, CanCreateComment, AuthorDisplayContext
```

`intent` 僅為 `ReadComments` 或 `CreateComment`。若帳號無資源 read 權限，verdict 不得回傳資源名稱、Project／Workspace ID、是否存在等可辨識資訊。Identity 另提供最小 `AuthenticatedActor` contract（account ID、account type、AI fixed role／owner snapshot 所需的當下資料），消費者不可取得 token。

## AI 帳號、token 與授權設計

### Identity slice

擴充 Identity 的 `Account` aggregate 或其明確關聯 entities：

- AI account 僅可由 human 建立；`account_type=ai_agent` 時要求唯一 `owner_account_id`、immutable `fixed_role`，以及符合全域唯一規則的 `ai-` username。human 不可啟用 AI API。
- `fixed_role` 為封閉 stable code：`pm_ai`、`development_ai`、`qa_ai`、`reviewer_ai`、`devops_ai`。角色／permission matrix 為 system seed data，不提供自訂或多角色堆疊。
- `ai_api_enabled` 預設 `false`。owner 與 administrator 都可啟用／停用；只有 owner 可建立、重建、查看或複製 token；administrator 可撤銷 token 與轉移帳號，但不得建立、重建、查看或複製 token。owner transfer、owner 不存在／停用／移除都必須立即拒絕該 AI account 的 Web 與 API 操作，並使 token 失效。
- 一個 AI account 僅保有一組有效永久 token。token plaintext 只可在規格允許的 create／re-create／owner view-copy flow 交給授權 caller；資料庫只保存可還原的加密密文、key identifier／version、hash 或 lookup verifier、created／revoked metadata，絕不保存或記錄明文。
- token encryption key 必須由部署環境的標準 Secret／Data Protection key source 提供，透過 validated Options 注入；不得以環境變數 helper、source code、migration seed 或 log 保存 key 或 token。驗證 Bearer token 時，先以 verifier 定位候選 token，再解密或驗證；任何 response、Problem Details、Comment、audit payload 與 structured log 都不得包含 token。
- Identity audit event 需記錄 create、enable／disable、create／re-create、view／copy、revoke、owner transfer，僅含 actor、target、time、stable event code 與 outcome。歷史動作要保存操作當時 AI 帳號名、fixed role 與 human owner snapshot，之後 rename／transfer 不回寫。

### 每次操作的 admission

所有資源操作以 current authenticated account 作為 actor；API Bearer only 導向 AI actor，不能建立 cookie session 或使用／要求 CSRF。人類 Web cookie 保持既有 CSRF 邊界。AI 操作的 allow 判斷必須同時為真：

```text
active AI account and enabled, valid non-revoked token
AND fixed-role permission allows operation
AND owner-derived Project read/membership scope allows target Project
AND (for TestManagement) owner-derived active Workspace membership allows target Workspace
AND current Project / Workspace state and link are valid
```

固定 role 不能因 owner 是 System Admin 或擁有較高人類 Project role 而提升；同理 System Admin 不得略過 Project 或 Workspace scope。owner transfer／disable 後，不可僅靠快取繼續通過 admission。為避免權限洩漏，跨 Project／Workspace 判斷一律在 resource owner 的 application service 內完成，未授權回應不披露另一邊 metadata。

### 固定角色 operation authorization

Identity 以 `fixed_role` stable code 產生不可自訂的 role-allowlist；各資源 owner 的 Application service 同時要求這個 allowlist、既有 resource permission 與上節 Project／Workspace scope intersection。Controller 只掛對應 policy metadata，不自行判定業務角色或範圍。所有五個角色在自身有效 Project／Workspace 範圍均可讀取資源及建立 `comment.create`；以下是唯一額外 mutation allowlist：

| fixed role | 可寫入 operation family | 明確禁止 |
| --- | --- | --- |
| `pm_ai` | 建立／更新 User Story、Task、Test Issue；建立／解除 `IssueParentChild` 與 `tests`；只可指派 Development AI 或 QA AI；建立交接 Comment。 | Test Case／Plan／Run／Run Item、Run 結果、Bug 與不可變 source links。 |
| `development_ai` | 僅更新自己被指派的 Development Task 之實作狀態、完成說明、Commit／驗證證據；建立 Comment。 | 建立／改寫需求、指派他人、所有測試資產、Bug、任何 trace link。 |
| `qa_ai` | 僅在被指派 Test Issue 範圍建立／更新 Test Case、Test Plan、Test Run、Run Item 結果與自己處理的 Bug；建立測試／缺陷證據 Comment。 | PM 已確認需求與所有不可變 source links。 |
| `reviewer_ai` | 僅在對應 Development Task 建立 Review Comment。 | 被審查資源內容、指派、關聯、流程狀態及任何其他 mutation。 |
| `devops_ai` | 僅建立自身範圍的診斷／處理 Comment。 | Task、指派與其他產品資源 mutation。 |

OpenAPI operation 要標註相同 authorization requirement；Identity policy mapping、resource-owner operation authorization 與 TypeScript client visibility 均使用這些 stable codes，不接受 caller supplied role。Focused tests 必須以每個 allowed operation、同角色的 denied operation、跨 Project／Workspace，以及 owner scope 比較驗證矩陣。

### Token lifecycle authorization

所有 token lifecycle operation 先要求 authenticated human actor 且為當前 active owner 或 administrator，然後套用固定 matrix；token plaintext 永不出現在 list、detail、audit 或 administrator response。

| operation | AI account human owner | administrator | response / audit |
| --- | --- | --- | --- |
| enable / disable AI API | allowed | allowed | 不含 plaintext；記錄 stable event code 與 outcome。 |
| create / re-create token | allowed | denied | 僅 owner 的成功 response 可含 plaintext；re-create 在同一 transaction 使舊 token 失效並記錄 audit。 |
| view / copy token | allowed | denied | 僅 owner 的明確 action response 可含 plaintext；每次 view/copy 都記錄 audit。 |
| revoke token | allowed | allowed | 不含 plaintext；記錄 audit。 |
| transfer owner | denied | allowed | 不含 plaintext；同一 transaction 使 token 失效、更新 owner 並記錄 audit。 |

每個 OpenAPI operation、Controller policy、Identity application command 與 audit test 均以此矩陣對齊。前端只有 owner route／control 可以呼叫會回傳 plaintext 的 command；administrator route 永遠不綁定或渲染 token plaintext。

## Comment contract、資料與 idempotency

OpenAPI-first 新增（精確 path／page response shape 須與現有 contract conventions 對齊）：

```text
GET  /api/v1/comments/{resourceType}/{resourceId}?page=...
POST /api/v1/comments/{resourceType}/{resourceId}
     Idempotency-Key: required opaque header
```

`resourceType` 為 closed enum：`issue`、`test_case`、`test_plan`、`test_run`；其它資源一律拒絕。建立 request 只有 Markdown `content`；author、createdAt、accountType、role、owner、resource authorisation 不可由 caller 指定。response 回傳 server-derived author display、accountType、操作當時必要 AI metadata snapshot、createdAt、content 與 version。列表固定 `created_at DESC, id DESC`，使用 cursor 或能完整代表上述 ordering 的 page contract，避免 timestamp tie 造成重複／遺漏。

`comments` 至少包含 UUID id、resource type／id、author account ID、Markdown content、created at、version、soft-delete／audit fields；索引 `(resource_type, resource_id, created_at DESC, id DESC)` 與 `author_account_id`。資源 soft-delete 時 Comment 留存但不顯示、不允許新增；restore 後由 access contract 重新判定可見。Comment 沒有 edit、delete、reply、type API。

Markdown 的 server sanitization 與 frontend rendering 使用同一 policy：預設允許標題、清單、表格、程式碼與 HTTP(S) URL；拒絕 raw HTML、script、`data:`、檔案路徑、外部 images／embeds。只允許 KhaiKang 授權附件圖片。Content 上限為 system-wide validated option（預設 10,000 Unicode chars），前後端皆驗證；本 slice 不自行增加設定 UI／API。

每個允許的 create／解除 relation use case 要求 idempotency。採 **module-local idempotency record**：CommentManagement、ProjectManagement、TestManagement 分別擁有 `comment_idempotency_records`、`project_idempotency_records`、`test_idempotency_records`；沒有共用 infrastructure owner，也沒有跨 module DbContext 寫入。每個 `operation_code` 是 globally stable、帶 module namespace 的常數，因此同一 operation 始終由同一 module owner 處理。

每張 record 的 logical key 為 `(actor_account_id, operation_code, idempotency_key)`，資料庫 unique constraint 與 canonical request fingerprint、state、terminal outcome／response replay snapshot、target ID、createdAt／completedAt 共同保存。`state` 是封閉 code，且 fingerprint 一經 reservation 永不更新：

| state | 意義 | 同 fingerprint retry | 不同 fingerprint |
| --- | --- | --- | --- |
| `terminal_success` | mutation 與成功 response snapshot 已原子 commit。 | 回傳保存的原成功結果，不再執行 mutation。 | `idempotency-conflict`。 |
| `terminal_business_rejection` | 預期的業務或 validation rejection 已持久化，沒有 mutation 被 commit。 | 回傳保存的原 rejection Problem Details，不再驗證或執行新 mutation。 | `idempotency-conflict`。 |
| `incomplete_transient_failure` | reservation 已持久化，但未預期 transient／5xx 使本次 mutation transaction rollback，尚無 terminal outcome。 | 只允許同一 canonical request 取得 recovery execution；它可完成為任一 terminal state 或再次保持 incomplete。 | `idempotency-conflict`。 |

每個 owner Application service 以兩個刻意分離、但都只使用該 module DbContext 的 PostgreSQL transaction 處理此流程：

1. 呼叫者先通過當前 authentication 與 resource admission；在能決定 canonical fingerprint 後，以短 transaction insert `incomplete_transient_failure` reservation 並 commit。所有會影響 outcome 的 expected request／business validation 在 reservation 後由 Application service 執行，所以 validation rejection 也會消耗 key。authentication、CSRF 或缺少 idempotency key 的拒絕不建立 record。
2. 第二個 transaction lock 同一 record，重新比對 fingerprint，執行 validation 與 domain mutation，並把 record transition 成 terminal state。成功時 mutation 與 `terminal_success` snapshot 一起 commit；預期 business／validation rejection 時，無 mutation 但 `terminal_business_rejection` Problem Details snapshot 一起 commit。
3. 未預期 transient／5xx 時，第二個 transaction 完整 rollback mutation 和任何 terminal transition；第一個已 commit 的 reservation 保持 `incomplete_transient_failure` 與原 fingerprint。相同 fingerprint 的後續 retry 才可取得 recovery execution；不同 fingerprint 在所有 state 均被 conflict，絕不因 transient failure 取得新的 reservation 或替換 payload。

unique constraint 與 record lock 讓 concurrent same-key requests 被序列化：terminal state 直接 replay；已 lock 的 incomplete record 不會開始第二次 mutation，lock release 後只按相同 fingerprint recovery 或 replay。fingerprint、record、snapshot 均不保存 token、password 或其它 plaintext secret。

不可變 source links 沒有 delete／replace／soft-delete operation，因此不配置這類 operation code 或 idempotency record；一般可解除的 `IssueParentChild`／`tests` 才有 unlink operation。此設計讓每個 module 的 Interface 只暴露「以 idempotency key 執行本 module mutation」這個深 seam，將 reservation、concurrency、failure state、fingerprint 與 replay 複雜性保留在同一 owner implementation 與 transaction 內。

## 追溯 slice

- `IssueParentChild`：保留 ProjectManagement owner 與既有 `parent_of`，建立時額外驗證 parent=`story`、child=`task` 或 `test issue`；同 Project、一個 active parent、無 self／cycle。一般 parent-child 與 `tests` 可在授權範圍解除，不刪資源。
- `TestCaseGeneratedByTestIssue`：TestManagement 新增單一來源 link（Case 至多一個 Test Issue），以 Issue directory contract 驗證 Project／Workspace／Issue type／access。此為不可變歷史來源；不可用一般 replace／delete endpoint 偽裝修改。
- `BugFoundByTestIssue`、`BugFoundInTestRun`、`BugFoundInTestRunItem`：TestManagement 分別保有 fixed links；每種對 Bug 至多一筆。Run Item link 必須驗證 item 屬於該來源 Run、Run 在指定 Workspace、Bug 所屬 Project 有有效 Workspace–Project link。三者均為不可變歷史來源，建立後不提供 delete、replace、soft-delete、unlink 或其他解除途徑；Run Item link 也不可改寫 Run／Item historical snapshot，且不接受任意 relation type。
- Test Plan 不引入繼承；Run 在 creation transaction 固定當下 Plan 的 case／test scope，後續 Plan 改動不得回寫已建立 Run。

`BugFoundInTestRunItem` 對齊產品規格 §7、§12 與 QA `TRC-04`：可建立與查詢、不可解除。HTTP contract 與 frontend 不提供 delete／replace action；PostgreSQL mapping 不加入可被 mutation API 使用的 soft-delete transition；integration tests 要驗證建立後所有解除或改寫嘗試均被拒絕，且 Run 歷史不變。

## 前端 slices

在 `frontend/apps/web` 建立 feature-local typed Comment API module、`useComments` 及 `CommentPanel`，由 Issue、Test Case、Test Plan、Test Run 的 view 以 props 提供 resource type／id 與 server-derived allow state。不得 raw `fetch` 或客戶端自行計算 access。

- Editor／preview 用既有安全 Markdown boundary；輸入與送出都驗證 length。
- CommentPanel 獨立 loading／error；initial load 失敗最多自動 retry 三次，後顯示規格文字與 explicit reload。mutation failure 不背景重送，使用相同 idempotency key 由 caller 明確 retry。
- newest first；create success reload page 1 並呼叫 `useSaveNotice`；僅有 previous、next、current page 的簡化控制，沒有 page size 或 total。
- 作者資訊使用 server response 顯示 human／AI badge、歷史 fixed role／owner snapshot。主資源欄位不新增 AI flag。mutation、loading、disabled、focus、keyboard、responsive 與 error states 都是完成條件。

AI account management frontend 依固定 role／token lifecycle matrix 實作：只有 owner controls 可呼叫或顯示 create／re-create／view／copy 的 plaintext response；administrator controls 只能 enable、disable、revoke、transfer，永不渲染 token plaintext。token display／copy 要有明確 acknowledgement，禁止在 route state、clipboard toast、query string、client logging 或 persisted store 留下 token。

## Migration 與實作順序（後續 Handoff 才可執行）

1. Reviewer 通過本 artifact；token encryption/key-management 採部署技術 profile 的受控實作細節，不改變本文已確認的 authorization rules。
2. OpenAPI 先定義 Account／AI token、Comment、trace links、security／CSRF／Problem Details；對每個 operationId 映射 Controller 與 TypeScript client。
3. Identity single final migration：AI owner／role／API state／token metadata及必要 audit snapshot；ProjectManagement／TestManagement／CommentManagement 各自只產生其 owner tables 的 migration。每個 affected DbContext 在 branch merge 前合併為一個 unpublished final migration。
4. 實作 feature-local application contracts、admission checks 與 Controller adapters；任何跨 module lookup 經 contract，無 DbContext 越界。
5. 實作 frontend API、CommentPanel 與四個 resource view；最後完成 migration／contract／PostgreSQL seam tests。

## Verification plan

Implementation 的 focused tests 與 evidence 至少涵蓋：

- AI token lifecycle（disabled、revoked、re-created、owner-transfer、orphan）與 Bearer-only／cookie-CSRF separation；owner-only create/re-create/view/copy、administrator-only denied plaintext response、administrator enable/disable/revoke/transfer 與無 plaintext token 出現在 database projection、log sink、audit response 或 Comment。
- fixed-role matrix 的每個 allow／deny operation AND owner-derived Project AND Workspace scope；System Admin、單邊 scope、cross-project、inactive resource／link 均拒絕且不洩漏 metadata。
- Comment whitelist、server-derived actor／time／account type／snapshot、stable newest-first paging、Markdown／length、soft-delete restore、idempotency terminal success replay、terminal business／validation rejection replay、incomplete transient recovery與每個 state 的 fingerprint conflict。
- `IssueParentChild` type／cycle rules，Case source與三種 Bug source的 cardinality／immutability；`BugFoundInTestRunItem` 建立／查詢、所有解除或改寫被拒絕（QA `TRC-04`），以及 Run snapshot 不被 Plan 改寫。
- 真實 disposable PostgreSQL integration 用於 partial uniqueness、FK／check constraint、migration chain，以及每個 module-local idempotency record 的：durable incomplete reservation、success mutation 與 terminal success 的單一 transaction、business／validation rejection 的 terminal snapshot、injected transient rollback 後同 fingerprint recovery、每個 state payload substitution conflict、以及 concurrent same-key serialization；EF InMemory 不可作此證據。
- contract tests 覆蓋 route、method、status、required／nullable、enum、security、CSRF、Problem Details，並把每個 change operationId 映射到 C# endpoint 與 TypeScript client。
- frontend component／API tests 覆蓋 isolated load、3 次 retry、manual reload、create 回 page 1、simplified paging、AI badge、keyboard／focus，且無 page-size／total UI。

實作完成後才執行適用的 restore/build/test/format、frontend type-check/test/build 與 OpenAPI lint/bundle/compatibility checks。這些 Development evidence 不等同 Formal QA Run。

## Handoff Package

- Task / Feature：Comment、追溯與 AI 操作紀錄能力。
- Current Phase：Artifact Review — PM Disposition 的 DS-03 technical design convergence，等待 Reviewer AI recheck。
- Source-of-Truth Paths：`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`；本文件；`doc/plans/2026-08-21-comment-traceability-ai-capability-ds03-pm-disposition-handoff.md`；`doc/plans/2026-08-21-comment-traceability-ai-capability-development-specification-recheck-conclusion-reviewer-ai.md`；`doc/zh-TW/02-specs/01-user-authentication.md`；`doc/zh-TW/02-specs/04-issue-test-traceability.md`；`doc/zh-TW/03-data-model/05-issue-test-traceability-data-model.md`。
- Scope / Non-Goals：Comment、既定 trace links、AI account／token／authorization technical slices；不含 AI workflow、role switching、auto／orchestration、implementation、tests、migration、commit、push、deploy、phase transition。
- Produced Artifact：本 Development Specification 的 DS-03 PM Disposition 收斂版；沒有產品 code、Migration、test automation 或 OpenAPI diff。
- Validation Evidence：已重新比對唯一產品規格 §4、PM Disposition Handoff、Reviewer DS-03 escalation 與本 artifact；已核對 `terminal_success`、`terminal_business_rejection`、`incomplete_transient_failure` 的 fingerprint retention、transaction boundary、payload substitution prohibition 與 PostgreSQL evidence 計畫。未執行 build、test、runtime、Docker 或 deployment（Artifact Review design convergence 不授權）。
- Open Decisions / Blockers：PM 已確認 DS-03 failure semantics；本 artifact 不再把它列為 open decision。token encryption/key-management operational profile 與 Comment system setting management route 是後續依既有技術 profile／部署設定落實的 implementation detail，非本次 DS-03 recheck blocker。
- Next Role and Required Action：Reviewer AI 僅依 PM Disposition Handoff 重新確認 DS-03 的 terminal／incomplete state、fingerprint retention、payload substitution protection、module-local transaction boundary 與 PostgreSQL evidence。Reviewer 通過前，Development AI 不進入 Parallel Implementation。

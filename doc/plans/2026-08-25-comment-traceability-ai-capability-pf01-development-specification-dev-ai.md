# PF-01 Development Specification：AI Comment owner-derived scope

## 文件狀態

- Workflow Role：Development AI
- Current Phase：新標準 remediation track 的 `Artifact Review`，執行 `PF01-DS-ROLE-ADMISSION` 唯一一次 Development artifact correction
- Work Classification：Planned Change under PM／human PO confirmed remediation disposition
- Applicable Technology Profiles：`csharp-dotnet-aspnet-core`、`http-contract-quality`
- 本文件只定義 PF-01 技術設計並收斂 Reviewer 指定的 Comment create admission；沒有產品 code、OpenAPI、migration、test automation、commit、push、deploy 或 phase advancement 授權。

## 問題與設計目標

現有 `CommentService.CreateAsync` 先由 Identity 解析 AI account，再以 owner account ID 查詢 Project／Workspace access；`ListAsync` 則把 current AI account ID 直接當成 resource membership account ID。結果是 AI Bearer 可在 owner scope 建立 Comment，卻無法以相同 scope 讀回 Comment。

本 remediation 必須達成：

1. Comment list 與 create 使用同一個 current actor interpretation。
2. AI actor 的 membership scope 永遠是當下 active human owner；human actor 的 scope 維持本人。
3. ProjectManagement／TestManagement 各自保有其資源 read／comment verdict，不讓 CommentManagement 讀取其他 module 的 DbContext 或 entity。
4. AI fixed role、Project／Workspace intersection、owner transfer／disable／orphan 狀態都使用當下資料，不接受 caller supplied owner、role 或 scope。
5. missing resource、soft-deleted resource 與 unauthorized resource 維持相同不洩漏結果。

## Module boundaries 與 Dependency Rule

### Identity owns the single actor/scope seam

Identity 繼續擁有既有 cross-module application contract：

- `IResourceActorDirectory.FindActiveAsync(accountId, cancellationToken)`
- `ResourceActorSnapshot`
  - `AccountId`：current authenticated actor，供 Comment author／idempotency identity 使用。
  - `ScopeAccountId`：human 為本人；AI 為當下 active human owner，僅供 resource membership／permission scope 使用。
  - `AccountType`、`FixedRole`、`AccountName`、`OwnerAccountId`、`OwnerDisplayName`：fixed-role admission 與 immutable Comment author snapshot 的 server-derived input。

`ResourceActorDirectory` 每次由 IdentityDbContext 讀取 active actor 與 owner，不使用 route、body、query、client claims 中的 owner／role 值，也不建立跨 request cache。AI disabled、orphaned，或 owner 非 active human 時回傳 `null`。

CommentManagement 不再擁有第二套 actor resolution contract。後續實作應：

- 刪除 `ICommentActorDirectory` 與 Identity 的 `CommentActorDirectory` adapter／DI registration。
- Identity 移除對 CommentManagement 的 project reference。
- CommentManagement 以 project reference 依賴 Identity 的 public `Contracts` seam；不得依賴 Identity DbContext、entity、infrastructure 或 endpoint。
- CommentManagement 保留自己的 persisted author snapshot model；Application 層把 `ResourceActorSnapshot` 映射成 Comment domain creation 所需欄位。這是資料寫入 mapping，不是第二次 actor／scope lookup。

預期 compile-time dependency：

```text
KhaiKang.Api
  -> CommentManagement
  -> Identity

CommentManagement -> Identity.Contracts
Identity -X-> CommentManagement
ProjectManagement -> CommentManagement + Identity.Contracts
TestManagement -> CommentManagement + Identity.Contracts
```

不得把 Identity contract 移到 CommonUtils；actor、owner 與 fixed role 是 Identity domain vocabulary，不是 domain-neutral common utility。

## Effective Comment access flow

### Shared application flow

`CommentService.ListAsync` 與 `CreateAsync` 都先執行同一個 private application flow：

1. endpoint 只從 authenticated `ClaimsPrincipal` 取得 current actor account ID。
2. `IResourceActorDirectory.FindActiveAsync` 取得 fresh `ResourceActorSnapshot`；`null` 立即回傳 non-disclosing not-found outcome，不查 Comment table。
3. 將完整 actor snapshot 與 target resource type／ID 傳給對應 `ICommentResourceDirectory`。
4. resource owner 只回傳 `CommentResourceAccess { CanRead, CanCreate }`，不回傳 Project、Workspace、resource name、Comment count 或 membership metadata。
5. list 只有在 `CanRead=true` 後才查 CommentManagementDbContext；create 只有在既有 create admission verdict 允許後才進入 idempotency reservation／mutation flow。

為避免把 `ActorAccountId` 與 `ScopeAccountId` 再次混用，`ICommentResourceDirectory` 的 public boundary 應使用 intention-revealing parameter object，例如：

```text
GetAccessAsync(CommentResourceAccessQuery query, CancellationToken)

CommentResourceAccessQuery
  ResourceType
  ResourceId
  Actor: ResourceActorSnapshot
```

resource directory 必須從 `query.Actor.ScopeAccountId` 取得 membership subject；不得自行以 `Actor.AccountId` 或 caller supplied owner ID 代替。傳遞完整 established actor context 也讓 fixed role／human distinction 保持同一來源，不在下游重新組裝 ambiguous GUID parameters。

### List

- AI Bearer：以 AI account ID 記錄 current actor，但以 owner-derived `ScopeAccountId` 判斷 resource read。
- Human cookie session：`AccountId == ScopeAccountId`，沿用既有 human read permission。
- `CanCreate` 仍由相同 resource-owner verdict server-derived 回傳；frontend 不重算權限。
- Comment author snapshot 是歷史資料；owner transfer 不改寫已存在 Comment response 的 author role／owner snapshot。

### Create

- 與 list 使用同一 actor resolution 與 resource access flow，避免兩條 owner-mapping branch 再次漂移。
- `Actor.AccountId` 保持 idempotency logical key 與 Comment author ID；不得改成 owner ID，否則不同 AI accounts 可能錯誤共用 idempotency namespace或失去 AI authorship。
- `Actor.ScopeAccountId` 只用於 resource admission。
- PF-01 不改 `terminal_success`、`terminal_business_rejection`、`incomplete_transient_failure`、fingerprint 或 transaction boundary。

### Comment create admission contract and decision order

`CommentResourceAccessQuery` 是 CommentManagement 對 resource owner 的唯一 admission request。它攜帶完整且 fresh 的 `ResourceActorSnapshot`，但不攜帶 caller 可指定的 owner、role、assignment、review context 或 testing context。`CommentResourceAccess { CanRead, CanCreate }` 仍是唯一回傳；resource owner 不把判斷原因或 context metadata送回 CommentManagement。

每個 resource owner 必須依下列順序判定，禁止先做 role target allow 後略過 read scope：

1. CommentManagement 先以 current authenticated account ID 取得 fresh actor；actor不存在或不可用時直接拒絕。
2. resource owner 以 `Actor.ScopeAccountId` 重新判定目標 resource 的既有 read scope。read denied時固定回 `{ CanRead=false, CanCreate=false }`。
3. human actor在 `CanRead=true` 後只套用既有 resource-owner `comment.create` permission／Workspace writable-role規則；PF-01 不增加或移除任何 human admission條件。
4. AI actor在 `CanRead=true` 後先要求 `FixedRole` 是五個known stable codes之一；未知、null或無法解析的role固定為 `CanCreate=false`，但不撤銷已成立的 `CanRead=true`。
5. resource owner 同時要求 fixed-role Comment target allowlist與該角色的 authoritative target／context verdict；owner的人類權限、System Administrator身分或較高Workspace role都不得替代任一條件。
6. assignment與role comparison一律使用 `Actor.AccountId`（AI account），membership intersection一律使用 `Actor.ScopeAccountId`（human owner）。兩者不得互換。
7. required assignment／review／testing context不存在、inactive、不一致或無法從owner-owned seam取得時，結果固定為 `{ CanRead=true, CanCreate=false }`。POST因此沿用non-disclosing `404`且不建立idempotency reservation；GET仍可為`200`並回`canCreate=false`。

固定allowlist只接受產品規格已確認的Comment context；accepted `PF01-QA-01`選擇其中一個representative allowed target驗證每個角色，不得由test row反向擴張其他role／target組合：

| Fixed role | Resource owner | Allowed Comment target／authoritative context |
| --- | --- | --- |
| `pm_ai` | ProjectManagement | owner scope內供交接使用的active User Story、Task或Test Issue；Issue type／Test Issue context由ProjectManagement authoritative Issue／`tests` relation判定，不要求assignment。`PF01-QA-01`以User Story作representative row。 |
| `development_ai` | ProjectManagement | owner scope內、`task`且`AssigneeAccountId == Actor.AccountId`的Development Task。 |
| `qa_ai` | ProjectManagement | owner scope內、`task`、`AssigneeAccountId == Actor.AccountId`且具有active outbound `tests` relation的Test Issue；Test Issue沿用既有`task + tests`語意，不依標題或client label推論。 |
| `reviewer_ai` | ProjectManagement | owner scope內、`task`且其assignee由Identity fresh actor seam確認為active `development_ai`的Development Task。這是現行review target context；不新增Reviewer assignment或relation。 |
| `devops_ai` | ProjectManagement | owner scope內的active Task；不要求assignment，且只授與Comment create，不授與Task mutation。 |
| `qa_ai` | TestManagement | owner同時具active Workspace與linked Project scope，且Test owner能從Case immutable Test Issue source、Plan current Test Issue或Run immutable Test Issue snapshot解析出同一testing context；Project authoritative seam再確認該Test Issue目前是指派給`Actor.AccountId`的active Test Issue。 |

Test Case／Plan／Run對`pm_ai`、`development_ai`、`reviewer_ai`與`devops_ai`固定`CanCreate=false`；這不影響它們在owner-derived scope成立時的`CanRead`。Case缺少immutable Test Issue source、Plan缺少current Test Issue、Run缺少Test Issue snapshot，或任一Workspace–Project／Project assignment context無法驗證時，同樣維持readable-but-not-creatable。

## Resource-owner access verdict

### ProjectManagement

`ProjectCommentResourceDirectory` 以 `Actor.ScopeAccountId` 呼叫既有 Issue read／comment access policy，並把AI create交給Project-owned `ProjectCommentTargetAdmission` application seam。此seam只在ProjectManagement內讀取Issue、Issue type、assignee、active `tests` relations與Project membership；需要確認assignee account type／fixed role時只呼叫Identity public `IResourceActorDirectory`，不讀Identity DbContext。

- active Project membership 與 Issue read permission 決定 `CanRead`。
- human `CanCreate`維持既有`comment.create` permission。
- AI `CanCreate`依上節五角色Project target matrix決定；Project target、assignment、Test Issue `tests` relation與Reviewer Development Task context都由此owner seam authoritative判定，不由CommentManagement或client推論。
- resource missing、soft-deleted、inactive membership、cross-Project 或 denied permission 都回傳 `{ false, false }`，不回傳 Issue／Project metadata。
- owner 是 System Admin 但不是目標 Project 的有效 scoped member時，不得以人類 System Admin 權限提升 AI fixed role 或略過 Project intersection。

TestManagement需要驗證Case／Plan／Run的Test Issue context時，ProjectManagement另提供窄化的public application contract：

```text
IIssueCommentContextDirectory.IsAssignedTestIssueAsync(
  issueId,
  expectedProjectId,
  aiActorAccountId,
  ownerScopeAccountId,
  cancellationToken) -> bool
```

此named seam在ProjectManagement內一次驗證active Project、owner scoped membership／read、Issue為`task`、assignee為該AI account及active outbound `tests` relation；只回boolean且不回Issue／Project metadata。它不是現有泛用`IIssueDirectory` DTO加欄位，也不讓TestManagement或CommentManagement自行重建Project role rules。

### TestManagement

`TestCommentResourceDirectory` 以 `Actor.ScopeAccountId` 套用目標 Test Case／Plan／Run 的既有 owning-resource read policy，並把AI create交給Test-owned `TestCommentTargetAdmission` application seam：

- active Workspace membership 是必要條件。
- human `CanCreate`維持既有active Workspace owner／manager規則。
- AI只有`qa_ai`可通過Test asset Comment create target allowlist；其他四角色即使可read亦固定`CanCreate=false`。
- `qa_ai`的testing context由TestManagement自己解析：Case使用既有immutable `TestCaseSourceLink`，Plan使用current `TestIssueProjectId/TestIssueId`，Run使用creation-time immutable Test Issue snapshot；不得接受request supplied Project／Test Issue context。
- 解析到的Project必須存在active Workspace–Project link，owner必須同時具有active Workspace membership與Project scope，再由`IIssueCommentContextDirectory.IsAssignedTestIssueAsync`確認Test Issue assignment／`tests`語意。
- PF-01不替沒有上述authoritative context的resource建立或猜測Project association；缺context即`CanCreate=false`，而已成立的Workspace read仍保留`CanRead=true`。
- missing／soft-deleted resource、inactive Workspace member、invalid link、cross-Workspace 或任一必要 Project scope denied 都回傳 `{ false, false }`，不回傳 Workspace／Project／resource metadata。

Resource owner 可以在自身 Application／Infrastructure boundary 使用其 DbContext 與既有 cross-module directory contract；CommentManagement 不得跨 module 查 ProjectMembers、WorkspaceMembers、WorkspaceProjects 或 resource entities。

## Authentication、authorization 與 stale-state boundary

- `AiTokenAuthenticationHandler` 仍先驗證 active AI account、AI API enabled、active non-revoked token 與 active human owner；Bearer 不使用 cookie／CSRF。
- Comment application service 不把 authentication claims 中的 owner／fixed role 當 authoritative scope，而是依 current actor ID 重新取得 Identity-owned context。
- owner transfer 後舊 token 由既有 token lifecycle 立即失效；後續 request 為 `401`。新 owner 建立的新 token以 fresh actor context 使用新 owner scope，不能沿用前 owner membership。
- AI disable、token revoke、owner disable／remove／orphan 的 Bearer request 為 `401`；即使 application seam 被非 HTTP caller 使用，`FindActiveAsync=null` 仍 fail closed。
- human session 維持既有 cookie、CSRF 與 resource permission行為；PF-01 不把 human actor轉換成另一帳號。

## Error and metadata non-disclosure

- `GET /api/v1/comments/{resourceType}/{resourceId}` 對 unsupported、missing、soft-deleted、cross-scope、inactive membership 與 denied read 一律維持現有 `404` boundary。
- `POST` 的 resource admission denial 同樣維持 `404`；authentication failure 維持 `401`，不建立 idempotency record。
- access denial 必須發生在 Comment query 前。response／Problem Details 不包含 resource ID 之外的新 metadata、Project／Workspace ID、resource title/name、Comment count、membership reason、owner ID、fixed role diagnostics 或 trace ID。
- structured logs 不記錄 Bearer token、Comment payload 或 owner membership details；PF-01 不新增 client-visible error code來區分不存在與未授權。

## Public contract、data model、migration 與 frontend impact

### OpenAPI／TypeScript

- 無 wire-contract 變更：route、method、security、status、request、`CommentPageResponse`、`canCreate`、Comment author snapshot 與 Problem Details shape 都維持不變。
- 不修改 `contract/openapi/khaikang.v1.yaml` 或 TypeScript client／contracts。
- Implementation verification 仍需用 contract test 確認 `ListComments`／`CreateComment` 的既有 security 與 200／201／401／404／409 mapping 沒有漂移；這是 compatibility evidence，不是 contract edit。

### Persistence

- 無 entity、column、index、constraint 或 migration 變更。
- Comment author與 owner snapshot欄位維持不變；既有 Comment與 idempotency records不做 backfill。
- PF-01 不需要 PostgreSQL migration-chain evidence。若 implementation 發現必須改 schema，立即停止並升 PM，因為那超出本 Parallel Design 的 confirmed impact。

### Frontend

- 無 frontend code change。CommentPanel 繼續只使用 server response 與 `canCreate`；不從 auth store、route 或 Project／Workspace metadata自行計算 AI scope。

## Implementation slices（後續新 Handoff 才可執行）

1. **Actor seam consolidation**：調整 project references，CommentService 改用 Identity-owned `IResourceActorDirectory`，移除 duplicate Comment actor directory，並加入 dependency-direction evidence。
2. **Resource-owner create admission**：新增`CommentResourceAccessQuery`；在ProjectManagement建立`ProjectCommentTargetAdmission`與窄化的`IIssueCommentContextDirectory.IsAssignedTestIssueAsync`，在TestManagement建立`TestCommentTargetAdmission`。owner modules依accepted role／target matrix判定assignment、review與testing context，只回最小`CanRead/CanCreate` verdict。
3. **Shared Comment access context**：讓list/create共用fresh actor resolution與同一resource-owner verdict；membership只使用`Actor.ScopeAccountId`，assignment只使用`Actor.AccountId`。缺role/context時保留read verdict但fail closed create，且在idempotency reservation前拒絕。
4. **Focused authorization regression**：以HTTP Bearer／human session覆蓋accepted五角色target matrix、Reviewer readable-but-create-denied、Test asset testing-context allow／missing-context deny、scope／transfer／disable／orphan與non-disclosure；不修改OpenAPI或資料模型。

每個 slice 必須保持 buildable／reviewable；不得在未確認新 Handoff 前開始實作或 commit。

## Verification plan

### Development-owned focused tests

1. `CreateAndListComments_WithAiBearer_AdmitsAcceptedRoleTargetMatrix`：以accepted `PF01-QA-01`五rows parameterize `pm_ai` User Story、`development_ai` assigned Development Task、`qa_ai` assigned Test Issue、`reviewer_ai` corresponding Development Task、`devops_ai` Task。每row均以AI Bearer `POST 201`後`GET 200`讀回同一Comment，並確認author仍是AI account、scope來自owner。
2. `ReviewerAi_OnReadableNonDevelopmentTarget_CannotCreateComment`：Reviewer owner可read的User Story維持`GET 200`與`canCreate=false`；POST為non-disclosing `404`且Comment與idempotency record都沒有mutation。
3. `AiCommentCreate_WhenRequiredRoleContextIsMissing_FailsClosedButRemainsReadable`：parameterize Development Task未指派／指派他人、QA task沒有active `tests` relation、Reviewer target不是指派給active Development AI的Task、unknown／null fixed role；scope read成立時均`GET 200 canCreate=false`、POST `404`。
4. `QaAi_TestAssetComments_RequireAuthoritativeTestingContext`：對Case／Plan／Run parameterize；Case source、Plan current Test Issue、Run snapshot都指向同一owner-scope Project內且assigned給current QA AI的active Test Issue時可create/list。缺source/snapshot、wrong assignee、inactive `tests` relation、inactive link或Project／Workspace任一邊scope缺失時，resource仍可read者回`200 canCreate=false`，POST `404`且無mutation。
5. `ListComments_WithAiBearer_UsesOwnerDerivedProjectScope`：owner有Issue read scope時AI Bearer可list；AI account自己的membership或owner的System Administrator權限不得取代owner scoped membership。
6. `ListComments_WithAiBearer_UsesOwnerDerivedWorkspaceScope`：owner有active Workspace scope時AI Bearer可list Test Case／Plan／Run Comment；涵蓋resource正常read policy所要求的Project intersection，且list不因create role/context denied而被撤銷。
7. `ListComments_WhenOwnerLacksAnyRequiredScope_ReturnsNotFoundWithoutMetadata`：cross-Project、cross-Workspace、inactive membership、invalid link、System Admin owner但無scoped membership皆為`404`，body不含resource title/name、Comment content、Project／Workspace metadata。
8. `CommentAccess_AfterOwnerTransfer_UsesOnlyNewOwnerScopeAndContext`：old token為`401`；new owner的新token不得沿用舊owner scope；assignment與role context仍以AI actor account重新判定，補齊新owner scope後才可依matrix list/create；existing Comment author snapshot不回寫。
9. `CommentAccess_WhenAiDisabledOrOrphaned_IsRejected`：AI disabled、owner disabled／removed／orphan及revoked token皆不可用Bearer讀取或建立Comment。
10. `CommentAccess_WithHumanSession_PreservesExistingReadAndCanCreateVerdict`：human owner／read-only member延續既有`CanRead`／`CanCreate`、cookie與CSRF behavior，不套fixed-role target matrix。
11. `CommentAccess_UnsupportedOrDeletedResource_DoesNotDiscloseExistence`：unsupported、missing、soft-deleted與scope denied保持同一external boundary；role/context denied也不新增可辨識error code。

以上 authorization／HTTP behavior 使用 `WebApplicationFactory`與實際 Identity、Comment、Project、Test application adapters，不以只驗證 mock call count 的測試取代。若沒有 PostgreSQL-specific query、schema、locking或transaction變更，SQLite HTTP integration可作本 PF-01 authorization evidence；不得把它宣稱為 PostgreSQL migration／concurrency證據。

### Architecture and compatibility evidence

- backend solution restore／build／focused integration tests／relevant regression／targeted `dotnet format --verify-no-changes`。
- 檢查project references：Identity不再依賴CommentManagement；CommentManagement只透過Identity public Contracts；TestManagement只透過ProjectManagement public `IIssueCommentContextDirectory`取得boolean testing-context verdict；任何consumer均不引用owner module Infrastructure／Domain／DbContext。
- Contract compatibility check：OpenAPI artifact零diff，既有 `ListComments`／`CreateComment` operationId、security、status與C# endpoint／TypeScript client mapping不變。
- `git diff --check`與明確 staged manifest；不混入PF-02～PF-04或planning drafts。

## Open Decisions / Blockers

沒有新的產品語意決策。Human PO／PM已確認owner-derived scope、fixed-role intersection、human regression與non-disclosure behavior。

若後續 implementation 發現下列任一情況，必須停止並升 PM，不得自行擴張：

- 必須改 public HTTP contract、data model或migration才能完成PF-01。
- 必須替目前沒有Project association的Test resource新增Project語意。
- 必須改fixed-role matrix、token lifecycle、Comment create permission或idempotency semantics。

## Handoff Package

- **Task / Feature**：PF-01 AI Comment owner-derived scope remediation。
- **Current Phase**：新標準 remediation track `Artifact Review`；`PF01-DS-ROLE-ADMISSION`唯一一次Development artifact correction completed，phase未推進。
- **Source-of-Truth Paths**：`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-review-conclusion-reviewer-ai.md`；`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md` accepted `PF01-QA-01`；`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-artifact-review.md`；`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-disposition-parallel-design-handoff.md`；`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12；`doc/zh-TW/02-specs/04-issue-test-traceability.md`。
- **Scope / Non-Goals**：只補resource-owner Comment create admission contract／decision order、Project assignment／review context、Test testing context、implementation slices與Development-focused HTTP verification；未修改產品code、OpenAPI、TypeScript、schema、migration、test automation、PF-02～PF-04或兩個未追蹤planning drafts。
- **Produced Artifact or Diff**：本PF-01 Development Specification的`PF01-DS-ROLE-ADMISSION` correction與獨立persistent Reviewer recheck handoff；沒有產品diff。
- **Validation Evidence**：已比對Reviewer finding、accepted `PF01-QA-01`、產品fixed-role matrix與traceability rules；唯讀確認ProjectManagement authoritative Issue type／assignee／`tests` relation、TestManagement Case source／Plan Test Issue／Run snapshot／Workspace–Project link，以及Identity fresh actor seam。這些既有資料可支援named owner application seams，無須新增產品語意、wire contract或schema。Artifact correction未執行build、test、runtime或Formal QA。
- **Open Decisions / Blockers**：目前無blocker；`PF01-DS-ROLE-ADMISSION`已使用唯一直接修正。若Reviewer recheck仍認為同family不完整，必須升PM，不得第二次直接修正。
- **Next Role and Required Action**：Reviewer AI只recheck `PF01-DS-ROLE-ADMISSION` correction，確認contract／decision order、owner-owned context seams、readable-but-not-creatable與accepted role matrix verification coverage。Development AI等待recheck結果，不自行實作、commit、push、deploy或推進phase。

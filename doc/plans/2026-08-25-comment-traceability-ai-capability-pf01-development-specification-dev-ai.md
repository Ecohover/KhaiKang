# PF-01 Development Specification：AI Comment owner-derived scope

## 文件狀態

- Workflow Role：Development AI
- Current Phase：新標準 remediation track 的 `Parallel Design`
- Work Classification：Planned Change under PM／human PO confirmed remediation disposition
- Applicable Technology Profiles：`csharp-dotnet-aspnet-core`、`http-contract-quality`
- 本文件只定義 PF-01 技術設計；沒有產品 code、OpenAPI、migration、test automation、commit、push、deploy 或 phase advancement 授權。

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

## Resource-owner access verdict

### ProjectManagement

`ProjectCommentResourceDirectory` 以 `Actor.ScopeAccountId` 呼叫既有 Issue read／comment access policy：

- active Project membership 與 Issue read permission 決定 `CanRead`。
- 現有 `comment.create`／fixed-role policy 決定 `CanCreate`；PF-01 不重新定義 fixed-role matrix。
- resource missing、soft-deleted、inactive membership、cross-Project 或 denied permission 都回傳 `{ false, false }`，不回傳 Issue／Project metadata。
- owner 是 System Admin 但不是目標 Project 的有效 scoped member時，不得以人類 System Admin 權限提升 AI fixed role 或略過 Project intersection。

### TestManagement

`TestCommentResourceDirectory` 以 `Actor.ScopeAccountId` 套用目標 Test Case／Plan／Run 的既有 owning-resource read policy：

- active Workspace membership 是必要條件。
- 當該 resource 的正常 read policy已有 Project dimension、Workspace–Project link 或 Project readability 要求時，Project 與 Workspace verdict 必須同時成立。
- PF-01 不替 project-neutral Test resource 新增 Project association，也不更改 Test asset business model；只確保既有 resource policy 使用 owner-derived scope，而非 AI account ID。
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
2. **Shared Comment access context**：新增 `CommentResourceAccessQuery`，讓 list/create 共用 fresh actor resolution；Project／Test directories只使用 `Actor.ScopeAccountId` 取得最小 verdict。
3. **Focused authorization regression**：以 HTTP Bearer／human session驗證 allow、deny、transfer、disable、orphan與 non-disclosure；不修改 OpenAPI或資料模型。

每個 slice 必須保持 buildable／reviewable；不得在未確認新 Handoff 前開始實作或 commit。

## Verification plan

### Development-owned focused tests

1. `ListComments_WithAiBearer_UsesOwnerDerivedProjectScope`：owner有 Issue read scope時，AI Bearer可 list；同一 token create後可 list回該 Comment。
2. `ListComments_WithAiBearer_UsesOwnerDerivedWorkspaceScope`：owner有 active Workspace scope時，AI Bearer可 list Test Case、Plan、Run Comment；涵蓋 resource正常read policy所要求的 Project intersection。
3. `ListComments_WhenOwnerLacksAnyRequiredScope_ReturnsNotFoundWithoutMetadata`：cross-Project、cross-Workspace、inactive membership、invalid link、System Admin owner但無 scoped membership皆為 `404`，body不含 resource title/name、Comment content、Project／Workspace metadata。
4. `CommentAccess_AfterOwnerTransfer_UsesOnlyNewOwnerScope`：old token為 `401`；new owner的新 token不得沿用舊 owner scope，補齊新 owner scope後才可 list/create；existing Comment author snapshot仍顯示操作當下 owner。
5. `CommentAccess_WhenAiDisabledOrOrphaned_IsRejected`：AI disabled、owner disabled／removed／orphan及 revoked token皆不可用 Bearer讀取 Comment。
6. `CommentAccess_WithHumanSession_PreservesExistingReadAndCanCreateVerdict`：human owner／read-only member延續既有 `CanRead`／`CanCreate` 與 cookie behavior。
7. `CommentAccess_UnsupportedOrDeletedResource_DoesNotDiscloseExistence`：unsupported、missing、soft-deleted與denied保持同一 external boundary。

以上 authorization／HTTP behavior 使用 `WebApplicationFactory`與實際 Identity、Comment、Project、Test application adapters，不以只驗證 mock call count 的測試取代。若沒有 PostgreSQL-specific query、schema、locking或transaction變更，SQLite HTTP integration可作本 PF-01 authorization evidence；不得把它宣稱為 PostgreSQL migration／concurrency證據。

### Architecture and compatibility evidence

- backend solution restore／build／focused integration tests／relevant regression／targeted `dotnet format --verify-no-changes`。
- 檢查 project references：Identity不再依賴 CommentManagement；CommentManagement只透過Identity public Contracts，不引用Identity Infrastructure／Domain／DbContext。
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
- **Current Phase**：新標準 remediation track `Parallel Design`；Development Specification completed，尚未進入 Artifact Review。
- **Source-of-Truth Paths**：`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-disposition-parallel-design-handoff.md`；`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12；`doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-conclusion-reviewer-ai.md` PF-01；`doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`；`doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md`。
- **Scope / Non-Goals**：只設計single actor/scope seam、Comment list/create共同access flow、resource-owner最小verdict、non-disclosure、impact與verification；未修改產品code、OpenAPI、TypeScript、migration、test automation、PF-02～PF-04或兩個未追蹤planning drafts。
- **Produced Artifact or Diff**：本PF-01 Development Specification與內嵌Handoff Package；沒有其他authorized diff。
- **Validation Evidence**：已比對PM disposition、產品規格、Reviewer PF-01 evidence、原Development Specification與QA cases；已唯讀檢查`CommentService`、`ICommentActorDirectory`、`ICommentResourceDirectory`、Identity `IResourceActorDirectory`／`ResourceActorDirectory`、Project／Test Comment resource directories、Bearer authentication handler、DI與project references。設計確認既有Identity actor seam已由ProjectManagement／TestManagement使用，可取代Comment duplicate seam且不形成circular project reference。Parallel Design未執行build、test、runtime或Formal QA。
- **Open Decisions / Blockers**：目前無blocker；超出已確認impact的contract／schema／Test resource Project語意／fixed-role變更必須回PM。
- **Next Role and Required Action**：PM AI等待並整合QA AI的PF-01 cases，對兩份artifact執行Artifact Review；之後才可發出Reviewer AI Development Specification review handoff。Reviewer本phase `Not Involved`。Development AI等待新的明確Handoff，不自行實作、commit、push、deploy或推進phase。

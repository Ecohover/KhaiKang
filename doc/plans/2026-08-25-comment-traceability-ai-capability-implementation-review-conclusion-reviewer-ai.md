# Implementation Review Conclusion：Comment、追溯與 AI 操作紀錄能力

## Review Result

**Returned with findings.** 本次獨立審查固定範圍 `baa65ea...d89a1ee`；未執行 Formal QA 或任何產品變更。Comment UI、AI management UI、idempotency terminal state、immutable traceability source、token plaintext response boundary 與已列 OpenAPI/TypeScript seam 的整體方向符合 scope，但下列 Problem Families 尚未關閉。

## Standards

### PF-02 — Per-DbContext final migration discipline

- **Severity**: P1
- **Evidence**: ProjectManagement 同時新增 `20260824035811_AddIssueRelationIdempotency.cs` 與 `20260825014341_AddAiOperationSnapshots.cs`；TestManagement 同時新增 `20260824042310_AddImmutableTraceabilitySources.cs` 與 `20260825014841_AddAiOperationSnapshots.cs`。Implementation Handoff 未記錄已發布 migration、分階段資料搬移或其他例外。
- **Rule**: `.ai/backend.md` 的 Migration Discipline 要求 branch merge 前每個 affected DbContext 合併為一個 unpublished final migration。
- **One direct correction**: Development AI 對 ProjectManagement、TestManagement 各保留一個 final unpublished migration，重新產生 snapshot，並以 clean PostgreSQL migration-chain evidence 證明可套用。若相同 family 在修正後仍存在，升 PM AI。

### PF-03 — Required success notice omitted for token create/re-create

- **Severity**: P2
- **Evidence**: [AiAccountManagementView.vue](../../frontend/apps/web/src/views/AiAccountManagementView.vue) 的 `discloseToken` 成功後只開啟 `UiActionDialog`，未呼叫 `useSaveNotice`；其他 create/update mutation 已呼叫 notice。
- **Rule**: `.ai/frontend.md` 要求每次成功 create/update 都呼叫 `useSaveNotice`；需要 acknowledgement 時另使用 `UiActionDialog`。
- **One direct correction**: token create/re-create 成功時也顯示適當的 Save Notice，保留 dialog 作一次性憑證 acknowledgement。若相同 family 在修正後仍存在，升 PM AI。

### Non-blocking standards observation

`TraceabilitySourceService` 的新 public application methods 帶 5–6 個 positional arguments，是 Coding Style Baseline `Small Parameter List`／`Distinct Parameter Types` 的 Data Clumps warning。此輪不單獨阻擋；後續碰觸此 seam 時應改用 intention-revealing command/context object。

## Spec

### PF-01 — AI Comment read does not use owner-derived scope

- **Severity**: P1
- **Evidence**: [CommentService.cs](../../backend/src/modules/KhaiKang.Modules.CommentManagement/Application/CommentService.cs) 的 `CreateAsync` 會把 AI actor 映射為 owner scope（lines 62–80），但 `ListAsync` 以 AI account ID 直接呼叫 resource directory（lines 22–30）。[ProjectCommentResourceDirectory.cs](../../backend/src/modules/KhaiKang.Modules.ProjectManagement/Application/Issues/ProjectCommentResourceDirectory.cs) 與 [TestCommentResourceDirectory.cs](../../backend/src/modules/KhaiKang.Modules.TestManagement/Application/TestCommentResourceDirectory.cs) 都以該 ID 查 Project／Workspace membership。因此 AI 可建立 Comment 卻無法依 owner scope 讀回 Issue、Case、Plan 或 Run Comment；現有 `AiResourceAdmissionTests` 的 Comment history read 是以 human client 完成，未覆蓋 AI Bearer list。
- **Specification**: 產品規格 §9 要求 AI 繼承 owner 可讀的 Project／Workspace scope，並於每次操作套用 fixed-role 與 scope；§5 要求支援資源頁可顯示 Comment。
- **Escalation, not a direct correction**: 這是先前 DS-01「fixed-role／owner-derived scope」Problem Family 在實作中的再出現。先前已用盡一次直接 correction，依 Remediation Cycle Limit 升 `PM AI`，不得再由 Reviewer AI 直接要求 Development AI 第二次修正。PM 必須決定後續 remediation route；最低需要同一 actor/scope seam 用於 AI Comment list 與 Bearer allow/deny、owner-transfer scope regression evidence。

### PF-04 — Permanent recoverable token lacks mandatory persistent key source

- **Severity**: P1
- **Evidence**: [Program.cs](../../backend/src/KhaiKang.Api/Program.cs) 只在 optional `DataProtection:KeysDirectory` 有值時才持久化 Data Protection keys；Identity options 沒有在非 Testing 環境強制／驗證 persistent key source。部署若省略此設定，重啟或多 replica 後可能無法安全 reveal 既有永久 token。
- **Specification**: 產品規格 §9 允許 owner 日後查看／複製 token，且 Development Specification 要求 deployment Secret／Data Protection key source 和 validated options。
- **One direct correction**: Development AI 需讓非 Testing 環境的 persistent protected key source 成為必填且 startup-validated 的 deployment boundary，記錄非祕密設定方式，並補 restart/key-ring continuity evidence；不得把 key/token 明文放入 source、migration、log 或 response 以外的位置。若相同 family 在修正後仍存在，升 PM AI。

### Spec items independently checked as aligned

- token plaintext 僅由 owner human-session endpoint 取得；administrator controls 不取得 plaintext。
- Bearer request 會在 antiforgery middleware 前移除 antiforgery metadata，cookie mutation 仍保有 CSRF。
- Comment actor、Project／Test operation history 已保存 AI account、fixed role、owner snapshot。
- `CommentPanel` 已掛載於 Issue、Test Case、Test Plan、Test Run；AI account/token management route 與 typed client 已存在。
- 未發現此 range 的產品 scope creep。

## Validation Evidence and Limits

- Reviewer 已讀取 Implementation Handoff、產品規格、Development Specification、QA Cases、`.ai` contexts、Canonical backend/frontend/contract standards，並以 `git diff baa65ea...d89a1ee`、commit log 與 `git diff --check` 唯讀檢查；diff check 無輸出。
- Handoff 所聲明 build、focused backend tests、frontend type-check/test/build 是 Development evidence；Reviewer 未重跑，亦未將其視為 Formal QA。
- 工作樹未追蹤的 `doc/zh-TW/04-planning/13-...prep-draft.md` 與 `14-...integration-guide.md` 不在本 review scope，未變更。

## Handoff Package

- **Task / Feature**: Comment、Issue/Test 追溯與 AI 操作紀錄能力。
- **Current Phase**: `Implementation Review`。
- **Source-of-Truth Paths**: `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`; `doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md`; `doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-handoff.md`.
- **Scope / Non-Goals**: `baa65ea...d89a1ee` independent implementation review only; no Formal QA, deployment, push, product decision, or phase advancement.
- **Produced Artifact or Diff**: 本 persistent Implementation Review conclusion；沒有產品 code、test、migration、OpenAPI 或 deployment diff。
- **Validation Evidence**: 見上節；Review code/diff evidence completed, executable evidence not rerun by Reviewer.
- **Open Decisions / Blockers**: PF-01 is escalated to PM AI. PF-02, PF-03, PF-04 each require their one authorized direct correction before Implementation Review can pass.
- **Next Role and Required Action**: `PM AI` 先處理 PF-01 escalation 並明確 route remediation；Development AI 僅在 PM 指定範圍內修正 PF-02、PF-03、PF-04，附 focused evidence 後交 Reviewer recheck。相同 Problem Family 再出現時一律升 PM AI。Reviewer AI 不發出 Formal QA handoff，亦不推進 phase。

# PF-01 QA Parallel Implementation Handoff

## Handoff Metadata

- From Role：QA AI
- To Role：PM AI
- Current Phase：`Parallel Implementation`
- Task / Feature：PF-01 AI Comment owner-derived scope與固定角色 target admission QA automation
- QA test commit：`c797ca5` (`test: cover PF-01 AI comment admission`)
- Persistent artifact commit：`b19a20d` (`docs: hand off PF-01 QA implementation`)
- Implementation Review correction：`fc817de` (`test: align PF-01 QA class layout`)；只將human HTTP／CSRF／Bearer case移回matching public class file，assertion與coverage不變
- Formal QA：未執行；本文件不構成驗收、Reviewer approval或phase推進

## Source of Truth

1. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12。
2. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`，accepted `PF01-QA-01`～`PF01-QA-16`與`PF01-REG-01`～`PF01-REG-09` selection。
3. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-parallel-implementation-handoff.md`。
4. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-development-specification-dev-ai.md`與Reviewer recheck conclusion。
5. `AGENTS.md`、`.ai/INDEX.md`、`.ai/backend.md`、`.ai/contract.md`、`.ai/verification.md`。

## Produced QA Artifacts

| Path | QA ownership / coverage |
| --- | --- |
| `backend/tests/integration/KhaiKang.Api.IntegrationTests/Identity/Pf01AiCommentScopeTests.cs` | Public HTTP regression：五角色allowed matrix、representative Reviewer deny、required assignment/testing context fail-closed、Case／Plan／Run authoritative context、denied create不保留idempotency key、owner transfer/new-owner fresh scope、Project／Workspace intersection、System Administrator non-bypass、AI API/account disable、owner disable、`401`／non-disclosing `404`與metadata suppression；亦包含human cookie read/create、missing-CSRF、AI Bearer without CSRF與human outsider regression。原`c797ca5`將human case放在`Pf01HumanCsrfRegressionTests.cs`中的partial class；`fc817de`依PF-06移入本matching file並刪除舊檔，test method與assertions不變。 |
| `backend/tests/integration/KhaiKang.Api.IntegrationTests/Identity/AiResourceAdmissionTests.cs` | 將既有Reviewer Comment legacy fixture收斂至accepted target：Task需指派給active `development_ai`；產品斷言不變。 |

## Automated Case Mapping

- `PF01-QA-01`：五種fixed role各自使用accepted target與必要assignment／`tests` relation；另驗證Reviewer對User Story可讀但`canCreate=false`、POST `404`。
- `PF01-QA-02`、`PF01-QA-06`：owner缺Project scope與System Administrator owner均不得bypass；list/create維持non-disclosing boundary。
- `PF01-QA-03`～`PF01-QA-05`、`PF01-QA-07`：QA Test Case／Plan／Run authoritative context與Project AND Workspace intersection；沿用token移除Project membership後立即重算並拒絕。
- `PF01-QA-08`～`PF01-QA-12`：transfer撤銷舊token、新owner新token依fresh scope判定、AI API disable、AI account disable與owner disable立即拒絕。
- `PF01-QA-13`～`PF01-QA-14`：human cookie allow/deny、valid/missing CSRF與Bearer no-CSRF boundary。
- `PF01-QA-15`～`PF01-QA-16`：unauthorized existing與missing resource均為`404`，response不得包含Comment content、Issue/Project identifier；Test scope的單邊intersection以相同non-disclosing status拒絕。

## Validation Evidence

所有final commands均在repository root執行，使用隔離artifact path，避免既有`testhost`鎖住一般`bin/Debug`；未終止其他角色process。

1. Restore / build：

   ```powershell
   dotnet restore backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --configfile backend\NuGet.config --artifacts-path .local\qa\pf01-parallel-implementation-20260825\compile4\artifacts --disable-build-servers
   dotnet build backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-parallel-implementation-20260825\compile4\artifacts
   ```

   Final result：exit `0`；build succeeded，`0` warnings、`0` errors。最初拆分human-CSRF test時曾因缺少兩個`using`產生QA test compile error；補正後以上final build通過，未形成產品blocker。

2. PF-01 QA HTTP regression：

   ```powershell
   dotnet test backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-build --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-parallel-implementation-20260825\compile4\artifacts --filter "FullyQualifiedName~Pf01AiCommentScopeTests&FullyQualifiedName!~CommentAccess_WithHumanSession_PreservesReadCreateAndCsrfBoundaries" --logger "trx;LogFileName=pf01-qa-green.trx" --results-directory .local\qa\pf01-parallel-implementation-20260825\qa-green
   dotnet test backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-build --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-parallel-implementation-20260825\compile4\artifacts --filter "FullyQualifiedName~CommentAccess_WithHumanSession_PreservesReadCreateAndCsrfBoundaries" --logger "trx;LogFileName=pf01-human-csrf-red.trx" --results-directory .local\qa\pf01-parallel-implementation-20260825\qa-human-csrf-red
   ```

   Final result：兩個commands均exit `0`；前者`7/7` passed，後者`1/1` passed，合計`8/8` passed。TRX：`.local/qa/pf01-parallel-implementation-20260825/qa-green/pf01-qa-green.trx`、`.local/qa/pf01-parallel-implementation-20260825/qa-human-csrf-red/pf01-human-csrf-red.trx`。

3. Selected legacy regression：以accepted `PF01-REG-01`～`PF01-REG-09`九個method組成filter，使用同一compile4 artifact執行。

   ```powershell
   dotnet test backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-build --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-parallel-implementation-20260825\compile4\artifacts --filter "FullyQualifiedName~AccessComments_WithoutResourceAccess_DoesNotDiscloseTheResource|FullyQualifiedName~CreateIssueComment_WithCommentPermission_DoesNotRequireIssueUpdatePermission|FullyQualifiedName~ListTestComments_ReturnsServerDerivedCreateAccessForWritableAndReadOnlyMembers|FullyQualifiedName~Owner_CanCreateAiAccountAndUseBearerTokenWithoutCsrf|FullyQualifiedName~TransferOwner_RevokesTokenAndAdministratorCannotReadPlaintext|FullyQualifiedName~TestAssets_AllowQaAiDenyOtherRolesAndReevaluateOwnerWorkspaceScope|FullyQualifiedName~ProjectMutations_IntersectFixedRoleWithOwnerScopeAndOwnAssignment|FullyQualifiedName~ReviewerAi_CanCommentWithinOwnerScopeAndSnapshotSurvivesOwnerTransfer|FullyQualifiedName~WorkspaceProjectFlow_EnforcesMembershipVisibilityAndOptimisticConcurrency" --logger "trx;LogFileName=pf01-legacy-final.trx" --results-directory .local\qa\pf01-parallel-implementation-20260825\legacy-final
   ```

   Final result：exit `0`；`9/9` passed，`0` failed／skipped，12秒。TRX：`.local/qa/pf01-parallel-implementation-20260825/legacy-final/pf01-legacy-final.trx`。

4. Targeted format：

   ```powershell
   dotnet format backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-restore --verify-no-changes --include backend\tests\integration\KhaiKang.Api.IntegrationTests\Identity\Pf01AiCommentScopeTests.cs backend\tests\integration\KhaiKang.Api.IntegrationTests\Identity\Pf01HumanCsrfRegressionTests.cs backend\tests\integration\KhaiKang.Api.IntegrationTests\Identity\AiResourceAdmissionTests.cs --verbosity minimal
   ```

   Result：exit `0`。

5. Contract compatibility：`git diff --exit-code -- contract/openapi/khaikang.v1.yaml` exit `0`；existing `/api/v1/comments/{resourceType}/{resourceId}` operation mapping仍為`ListComments`與`CreateComment`。

## Expected Red / Earlier In-flight Evidence

- Current final QA snapshot：沒有Expected Red。
- Development in-flight期間曾觀察human cookie缺CSRF由expected `400`實際回`500`：`.local/qa/pf01-parallel-implementation-20260825/qa-current-3/pf01-qa-current-3.trx`為`7/8` passed。compile4重新build後同一case為`1/1` passed；因此不提報為Formal QA bug，但保留為Implementation Review可查驗的歷史證據。
- 更早的role-matrix Expected Red（Reviewer denied target被放行）記錄於`.local/qa/pf01-parallel-implementation-20260825/role-matrix-red-isolated/role-matrix-red-2.trx`；final suite已轉綠。

## Residual / Not Independently Verified

- `PF01-QA-12`的owner `removed`／physically missing子狀態沒有可用且獲授權的public lifecycle seam可建立；本slice驗證owner disabled後existing bearer立即`401`。未以DB/private helper偽造狀態。
- Project–Workspace link inactive與Workspace membership移除後的Comment retry未各自建立獨立transition test；本slice驗證missing Workspace、missing Project與Project membership removal，`PF01-REG-09`另保護link/membership visibility與concurrency baseline。
- Human wrong-CSRF token未另列一個method；missing-CSRF negative與valid-CSRF positive均已執行。
- 本驗證使用Development尚未提交的shared in-flight product diff快照；Reviewer Implementation Review前必須由PM併同Development commit/evidence重跑或確認，不得只以本QA commit在`49183a9`基線宣告feature green。

## Working Tree / Commit Isolation

- QA test commit staged manifest僅有上述三個integration test files；未使用`git add -A`。
- Development-owned product files、Development-owned `Pf01CommentAdmissionTests.cs`與`CommentIdempotencyPostgreSqlTests.cs`均未stage／未commit。
- 既有untracked planning drafts `doc/zh-TW/04-planning/13-comment-traceability-ai-capability-prep-draft.md`、`14-ai-skill-khaikang-integration-guide.md`未觸碰。
- 未push、未deploy、未執行Formal QA。

## Blockers and Next Role Action

- Product semantic / public contract blocker：無。
- Evidence dependency：final green依賴Development in-flight implementation snapshot；PM應等待Development Handoff及其focused commit SHA，再統一發出Reviewer Implementation Review Handoff。
- QA下一步：停在Parallel Implementation boundary，回交PM；不自行送Reviewer、不執行Formal QA、不指導Development remediation。

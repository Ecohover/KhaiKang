# Formal QA Run Evidence：PF-01 AI Comment owner-derived scope

## Run Summary

- Workflow Role：QA AI
- Current Phase：`Formal QA Run`
- Run ID：`PF01-FORMAL-QA-20260826-RUN-01`
- Run date / timezone：2026-08-26／Asia/Taipei
- Result：**PASS within the authorized automated scope**
- Bug Reports：無；本次沒有觀察到test failure
- Formal boundary：本結果不是release、deployment或human acceptance宣告；Disposition仍由PM整合證據後交human PO決定

## Source-of-Truth Paths

1. `doc/plans/2026-08-26-comment-traceability-ai-capability-pf01-formal-qa-run-handoff.md` (`587cf05`)。
2. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md` (`PF01-QA-01`～`PF01-QA-16`、`PF01-REG-01`～`PF01-REG-09`)。
3. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12。
4. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-implementation-review-conclusion-reviewer-ai.md` (`96cb2fa`)。
5. `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-implementation-review-recheck-conclusion-reviewer-ai.md` (`6ce7bf9`)。
6. `AGENTS.md`、`.ai/INDEX.md`、`.ai/verification.md`。

## Tested Snapshot and Environment

- Repository：`C:\00_Work\70_SideProject\KhaiKang`
- Branch：`ecohover/feature/20260820`
- Exact HEAD：`587cf05b993557249aa90d9417bc98f1a3399486`
- Formal feature range：`49183a9..587cf05`（11 commits；從PF-01 Parallel Implementation Handoff後到Formal QA Run Handoff）
- Reviewed implementation/test commits：Development `bd497dc`；QA automation `c797ca5`；QA layout correction `fc817de`
- .NET SDK：`10.0.400`
- OS：`Microsoft Windows NT 10.0.26200.0`
- Build/test artifacts：`.local/qa/pf01-formal-qa-run-20260826/run-01/`
- Initial working tree：只有兩份既存untracked planning drafts；未修改、未stage，未納入測試artifact或本次commit

## Commands and Results

### Restore and Build

```powershell
dotnet restore backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --configfile backend\NuGet.config --artifacts-path .local\qa\pf01-formal-qa-run-20260826\run-01\artifacts --disable-build-servers
dotnet build backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-formal-qa-run-20260826\run-01\artifacts
```

- Restore：exit `0`。
- Build：exit `0`；0 warnings、0 errors；35.65秒。

### PF01-QA-01～PF01-QA-16

Approved cases由8個public HTTP integration test methods聚合執行；case-to-method mapping沿用accepted QA cases與persistent QA Parallel Implementation Handoff。

```powershell
dotnet test backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-build --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-formal-qa-run-20260826\run-01\artifacts --filter "FullyQualifiedName~Pf01AiCommentScopeTests" --logger "trx;LogFileName=pf01-formal-qa-cases.trx" --results-directory .local\qa\pf01-formal-qa-run-20260826\run-01\qa-cases
```

- Exit code：`0`
- Result：8 passed、0 failed、0 skipped；24秒
- TRX：`.local/qa/pf01-formal-qa-run-20260826/run-01/qa-cases/pf01-formal-qa-cases.trx`
- SHA-256：`8E170A09CE2327F07A8DDFE59D481879E44B5B48B3FD472827C29CB184167424`
- Covered observable seams：五角色allow matrix、Reviewer readable-but-create-denied、required role/testing context、Case／Plan／Run、Project／Workspace intersection、System Administrator non-bypass、owner transfer/new-owner fresh scope、AI API/account/owner disable、human cookie/CSRF、Bearer no-CSRF、metadata non-disclosure、denied create不消耗idempotency key。

Executed methods：

1. `CreateAndListComments_WithAiBearer_AdmitsAcceptedRoleTargetMatrix`
2. `CreateComment_WhenRequiredRoleContextIsMissing_FailsClosedButRemainsReadable`
3. `QaAi_TestAssetComments_RequireAuthoritativeTestingContext`
4. `CommentAccess_AfterOwnerTransfer_UsesOnlyNewOwnerScopeAndDoesNotDiscloseMetadata`
5. `CommentAccess_WhenAiApiOrAccountIsDisabled_RejectsExistingBearer`
6. `CommentAccess_WithHumanSession_PreservesReadCreateAndCsrfBoundaries`
7. `CommentAccess_WhenSystemAdministratorOwnerLacksProjectScope_DoesNotBypassMembership`
8. `QaAi_TestAssetComments_RequireOwnerProjectAndWorkspaceIntersection`

### PF01-REG-01～PF01-REG-09

```powershell
dotnet test backend\tests\integration\KhaiKang.Api.IntegrationTests\KhaiKang.Api.IntegrationTests.csproj --no-build --no-restore --disable-build-servers -m:1 --artifacts-path .local\qa\pf01-formal-qa-run-20260826\run-01\artifacts --filter "FullyQualifiedName~AccessComments_WithoutResourceAccess_DoesNotDiscloseTheResource|FullyQualifiedName~CreateIssueComment_WithCommentPermission_DoesNotRequireIssueUpdatePermission|FullyQualifiedName~ListTestComments_ReturnsServerDerivedCreateAccessForWritableAndReadOnlyMembers|FullyQualifiedName~Owner_CanCreateAiAccountAndUseBearerTokenWithoutCsrf|FullyQualifiedName~TransferOwner_RevokesTokenAndAdministratorCannotReadPlaintext|FullyQualifiedName~TestAssets_AllowQaAiDenyOtherRolesAndReevaluateOwnerWorkspaceScope|FullyQualifiedName~ProjectMutations_IntersectFixedRoleWithOwnerScopeAndOwnAssignment|FullyQualifiedName~ReviewerAi_CanCommentWithinOwnerScopeAndSnapshotSurvivesOwnerTransfer|FullyQualifiedName~WorkspaceProjectFlow_EnforcesMembershipVisibilityAndOptimisticConcurrency" --logger "trx;LogFileName=pf01-formal-qa-legacy.trx" --results-directory .local\qa\pf01-formal-qa-run-20260826\run-01\legacy
```

- Exit code：`0`
- Result：9 passed、0 failed、0 skipped；11秒
- TRX：`.local/qa/pf01-formal-qa-run-20260826/run-01/legacy/pf01-formal-qa-legacy.trx`
- SHA-256：`8FE45BDB9BBBC242DCB498AC7B718652F347198B350EDC692615EE682BDE4964`

Executed methods：

1. `AccessComments_WithoutResourceAccess_DoesNotDiscloseTheResource`
2. `CreateIssueComment_WithCommentPermission_DoesNotRequireIssueUpdatePermission`
3. `ListTestComments_ReturnsServerDerivedCreateAccessForWritableAndReadOnlyMembers`
4. `Owner_CanCreateAiAccountAndUseBearerTokenWithoutCsrf`
5. `TransferOwner_RevokesTokenAndAdministratorCannotReadPlaintext`
6. `TestAssets_AllowQaAiDenyOtherRolesAndReevaluateOwnerWorkspaceScope`
7. `ProjectMutations_IntersectFixedRoleWithOwnerScopeAndOwnAssignment`
8. `ReviewerAi_CanCommentWithinOwnerScopeAndSnapshotSurvivesOwnerTransfer`
9. `WorkspaceProjectFlow_EnforcesMembershipVisibilityAndOptimisticConcurrency`

## Failures and Bug Reports

- Test failures：無。
- Environment／runner failures：無。
- Bug Report：未建立；沒有Formal QA failure可供提報。
- 未執行任何修正、修正後重驗或root-cause判定。

## Known Coverage Limits

依Formal QA Run Handoff原樣保留，下列未自動化範圍不是本次觀察到的Bug：

- physically removed／missing owner lifecycle state；現有automation驗證owner disabled。
- 每一個獨立Workspace membership／Project–Workspace link transition；現有automation與selected regression涵蓋missing Workspace、missing Project、Project membership removal及link/membership baseline。
- wrong-CSRF distinct from missing-CSRF；現有automation驗證valid CSRF positive與missing-CSRF negative。

未使用private helper或DB workaround擴張上述範圍。

## Handoff Package to PM

- Task / Feature：PF-01 AI Comment owner-derived Project／Workspace scope與固定角色 Comment admission。
- Current Phase：`Formal QA Run` complete；等待PM Disposition checkpoint。
- Scope / Non-Goals：只執行PF01-QA-01～16與PF01-REG-01～09；未修改產品或automation、未push/deploy、未宣告release/acceptance。
- Produced Artifact：本persistent QA Run evidence；local TRX paths與hash如上；無Bug Report。
- Validation Evidence：restore/build passed；authorized QA suite 8/8 passed；selected legacy 9/9 passed。
- Open Decisions / Blockers：沒有test或environment blocker；known coverage limits保留。
- Next Role and Required Action：PM AI整合本Run、Implementation Review與既有scope evidence，交human PO決定Disposition。QA不自行推進或執行其他測試。

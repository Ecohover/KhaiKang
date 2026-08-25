# PF-01 AI Comment owner-derived scope：QA Cases

## 文件狀態

- Workflow Role：QA AI
- Current Phase：新標準 remediation track 的 `Parallel Design`
- 來源交接：`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-disposition-parallel-design-handoff.md`
- 產品 Source of Truth：`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12
- 適用 Technology Profiles：`csharp-dotnet-aspnet-core`、`http-contract-quality`

本文件只設計 PF-01 cases、風險與 legacy regression selection。尚未實作 automation、執行 test、啟動 runtime 或進行 Formal QA，也不推進 workflow phase。

## 驗收判準

1. AI Bearer 的 Comment list 與 create 必須使用同一個 current AI actor／effective owner scope interpretation；不得把 AI account ID 當成 Project 或 Workspace membership identity。
2. 有效 AI 操作必須同時滿足：AI account 與 token 有效、fixed role 允許 Comment operation、owner 對目標 Project 有 scope，以及 TestManagement 資源所需的 owner Workspace scope 與有效 Project–Workspace link。
3. owner 是 System Administrator 不能略過 fixed role、Project scope 或 Workspace scope。
4. 有效 actor 但無 resource scope 時，`ListComments`／`CreateComment` 依既有 OpenAPI 回 `404`；回應不得讓 caller 分辨「存在但未授權」與「不存在」。失效 token／actor 則由既有 authentication boundary 拒絕。
5. owner transfer、AI account／AI API disable、owner disabled／removed／missing 必須在下一次操作立即重新判定，不得因快取或先前成功結果繼續讀取或建立 Comment。
6. human session 繼續以 human 自身的 resource scope 判定，維持 cookie／CSRF 邊界；PF-01 不得改寫 human authorization。

## PF-01 QA Cases

### PF01-QA-01 Role-aware target matrix

| Fixed role | Allowed Comment target | 必要前置條件 |
| --- | --- | --- |
| PM AI | owner scope 內供交接使用的 User Story | owner 是目標 Project 的有效 scoped member；不要求 assignment。 |
| Development AI | 自己被指派的 Development Task | Task 已指派給 current Development AI account；owner 是目標 Project 的有效 scoped member。 |
| QA AI | 自己被指派的 Test Issue | Test Issue 已指派給 current QA AI account；owner 是目標 Project 的有效 scoped member。 |
| Reviewer AI | 對應被審查的 Development Task | 目標是既有 review flow 對應的 Development Task；owner 是目標 Project 的有效 scoped member。不新增 assignment／relation 語意。 |
| DevOps AI | owner scope 內用於診斷／處理紀錄的 Task | owner 是目標 Project 的有效 scoped member；只建立 Comment，不指派或異動 Task。 |

Representative denied target：Reviewer AI 對 owner scope 內、但不是對應 Development Task 的 User Story。既有 read scope 仍允許 list，但 fixed-role target verdict 必須禁止 create。

| ID | 規格／交接 | 前置與操作 | 預期結果 | 未來驗證層／風險 |
| --- | --- | --- | --- | --- |
| PF01-QA-01 | §4、§9；AI Bearer role-aware allow／deny | 以本案例上方 matrix parameterize 五種 fixed role；建立 active AI account、enabled API與有效 token，滿足各 target的 assignment／review-context前置後，以同一 Bearer create再list。另以 Reviewer AI操作 representative denied target。 | 每個 allowed row均 `201` create、`200` list且可讀回同一 Comment；create與list使用相同 owner-derived Project scope，server-derived actor仍是AI account。Representative denied row的list仍為 `200`且`canCreate=false`，create為non-disclosing `404`且不產生Comment；不得推論其他role／target組合。 | HTTP integration；Critical |
| PF01-QA-02 | §4、§9；Project deny | AI account、token 與 fixed role 均有效，但 owner 不是目標 Issue 所屬 Project member；分別 list 與 create。 | 兩者均 `404`，沒有 Comment mutation；AI account 自己是否具有同名／舊 membership 不得影響 verdict。 | HTTP authorization integration；Critical |
| PF01-QA-03 | §2、§4、§9；Project／Workspace intersection allow | owner 同時具有 linked Project 與 Test Workspace scope，Project、Workspace 與 link 均 active。以 AI Bearer 對 Test Case、Test Plan、Test Run 各 create 後 list。 | 三種資源均可 create／list，list 可讀回新 Comment；所有 verdict 使用同一 owner Project AND Workspace intersection。 | HTTP integration，Case／Plan／Run parameterized；Critical |
| PF01-QA-04 | §4、§9；missing Workspace | owner 有 linked Project scope但沒有目標 Test Workspace scope；對 Case、Plan、Run list／create。 | 全部 `404`，沒有 mutation；Project scope 不得單獨放行 TestManagement Comment。 | HTTP authorization integration；Critical |
| PF01-QA-05 | §4、§9；missing Project | owner 有目標 Test Workspace scope但不是 linked Project member；對 Case、Plan、Run list／create。 | 全部 `404`，沒有 mutation；Workspace scope 不得單獨放行。 | HTTP authorization integration；Critical |
| PF01-QA-06 | §9；administrator no bypass | owner 是 System Administrator，但缺少目標 Project scope，或只具有 Test resource intersection 的單邊 scope。使用有效 AI Bearer list／create。 | 與 PF01-QA-02／04／05 相同拒絕；human owner 的高權限不得提升 AI fixed role 或略過 scope intersection。 | HTTP authorization integration；Critical |
| PF01-QA-07 | §9；scope re-evaluation | 先以完整 owner scope成功 list／create，再移除 owner 的 Project membership、Workspace membership或停用 Project–Workspace link，沿用同一有效 token重試。 | 下一次操作立即 `404`；不得由 admission cache、先前成功或 AI account ID 繼續通過，也不得回傳先前 Comment。 | HTTP integration／state transition；Critical |
| PF01-QA-08 | §9；owner transfer token invalidation | AI Bearer 先成功 list；administrator 將 AI account transfer 給另一 active human，沿用 transfer 前 token再次 list／create。 | 舊 token 立即失效並由 authentication boundary 拒絕；沒有 Comment response或 mutation，不得退回舊 owner scope判定。 | Identity＋Comment integration；Critical |
| PF01-QA-09 | §9；new owner intersection | transfer 後由新 owner 依既有 lifecycle取得新有效 token；分別準備新 owner具有完整 scope與缺少任一 scope的情境，再 list／create。 | 完整 intersection 可依 fixed role allow；缺少 Project或Workspace scope時 `404`。不得沿用舊 owner membership或 authorization cache。 | Identity＋resource authorization integration；Critical |
| PF01-QA-10 | §9；AI API disable | 有效 Bearer先成功 list；owner或administrator停用 AI API flag後，沿用原 token list／create。 | token立即不可用並由 authentication boundary拒絕；沒有 metadata或 mutation。重新啟用不得使已依 lifecycle失效的 token自行復活。 | Identity＋Comment integration；Critical |
| PF01-QA-11 | §9；AI account disable | 有效 Bearer先成功 list；停用 AI account後沿用原 token list／create。 | 下一次操作立即拒絕，不能讀取或建立 Comment；不得因 scope cache繼續通過。 | Identity＋Comment integration；Critical |
| PF01-QA-12 | §9；owner orphan | 對 owner disabled、removed與missing三種 orphan狀態，沿用原 AI token list／create；若產品支援 AI Web login，也驗證 Web session。 | AI account立即禁止 API／Web使用，歷史資料保留；任何 Comment endpoint均不得洩漏資源或留言 metadata。 | Identity state integration；Critical |
| PF01-QA-13 | §4、§9；human read regression | 以 human cookie session驗證：自身具有 Project／Workspace read scope時 list成功；缺少 scope時 list `404`。同時存在同一 human擁有的 AI account，但不使用 Bearer。 | human只依自身既有 membership／resource read規則判定；不得被轉換成 AI actor、fixed role或另一 owner scope。 | Existing human HTTP integration；High |
| PF01-QA-14 | §4、§9；cookie／Bearer boundary | human cookie對 create分別帶有效 CSRF與缺少／錯誤 CSRF；AI Bearer在完整 owner scope下不帶 cookie／CSRF create並 list。 | human mutation維持既有 CSRF要求，缺少／錯誤 CSRF不產生 Comment；AI Bearer不要求 CSRF且成功。Bearer不得建立 human cookie session。 | Security／contract integration；High |
| PF01-QA-15 | §4、§9；Project metadata leakage | 對同一有效 AI actor，比較「存在但 owner無 Project scope」與「隨機不存在 Issue ID」的 list／create拒絕回應；預先在私有資源放置可辨識 Comment。 | 均為 `404`且 public Problem Details shape不可區分；body／headers不得包含 resource ID／name、Project ID／name、Comment ID／content／author／count或 membership狀態。私有 Comment不可被回傳。 | HTTP contract／security；Critical |
| PF01-QA-16 | §4、§9；Workspace metadata leakage | 對 Test Case、Plan、Run比較「只缺 Project」、「只缺 Workspace」、「link inactive」及「不存在 resource」的 list／create拒絕。 | 均為 `404`且不可由 status、stable public fields或 payload判斷缺少哪一邊 scope；不得包含 Project、Workspace、link、resource或Comment metadata。 | HTTP contract／security；Critical |

## 風險矩陣

| 風險 | 等級 | 失敗影響 | 對應案例／必要證據 |
| --- | --- | --- | --- |
| AI account ID被誤當 membership identity，或 list／create採不同 scope seam | Critical | AI無法讀回合法 Comment，或越權讀取其他 Project／Workspace | PF01-QA-01～05；同一 actor create→list與雙向單邊 scope矩陣 |
| System Administrator或 fixed role繞過 owner intersection | Critical | 跨 Project／Workspace資料外洩 | PF01-QA-06；administrator owner與一般 owner的相同 deny evidence |
| transfer、disable、orphan或 membership變動後仍命中 stale admission cache | Critical | 已失效 AI持續讀寫 | PF01-QA-07～12；狀態變更前後沿用同 token的立即拒絕 evidence |
| deny response洩漏存在性、資源或Comment metadata | Critical | 可枚舉私有資源、留言與組織結構 | PF01-QA-15～16；authorized-existing／unauthorized-existing／nonexistent response差異檢查 |
| PF-01共用 seam改壞 human cookie authorization或 CSRF | High | 人類使用者被錯拒或 mutation安全邊界弱化 | PF01-QA-13～14；human allow／deny與 CSRF positive／negative evidence |

## Legacy Regression Selection

下列既有 tests 在未來收到 Formal QA Handoff後才執行；本階段只確認名稱與選取理由，不宣告它們目前 pass。

| ID | 既有 test | 選取理由 | 關聯案例 |
| --- | --- | --- | --- |
| PF01-REG-01 | `CommentEndpointsTests.AccessComments_WithoutResourceAccess_DoesNotDiscloseTheResource` | 保護既有 human unauthorized `404`與 resource non-disclosure。 | PF01-QA-13、15、16 |
| PF01-REG-02 | `CommentEndpointsTests.CreateIssueComment_WithCommentPermission_DoesNotRequireIssueUpdatePermission` | 保護 human `comment.create`與 Issue update permission分離。 | PF01-QA-13、14 |
| PF01-REG-03 | `CommentEndpointsTests.ListTestComments_ReturnsServerDerivedCreateAccessForWritableAndReadOnlyMembers` | 保護 Test resource read與 server-derived `canCreate`。 | PF01-QA-03～05、13 |
| PF01-REG-04 | `AiAccountTokenEndpointsTests.Owner_CanCreateAiAccountAndUseBearerTokenWithoutCsrf` | 保護 Bearer-only、無 CSRF與 AI token基線。 | PF01-QA-01、14 |
| PF01-REG-05 | `AiAccountTokenEndpointsTests.TransferOwner_RevokesTokenAndAdministratorCannotReadPlaintext` | 保護 owner transfer立即撤銷舊 token及安全邊界。 | PF01-QA-08～09 |
| PF01-REG-06 | `AiResourceAdmissionTests.TestAssets_AllowQaAiDenyOtherRolesAndReevaluateOwnerWorkspaceScope` | 保護 Test asset的 owner Workspace scope重新判定。 | PF01-QA-03～07 |
| PF01-REG-07 | `AiResourceAdmissionTests.ProjectMutations_IntersectFixedRoleWithOwnerScopeAndOwnAssignment` | 保護 Project owner scope與 fixed role intersection。 | PF01-QA-01、02、06、07 |
| PF01-REG-08 | `AiResourceAdmissionTests.ReviewerAi_CanCommentWithinOwnerScopeAndSnapshotSurvivesOwnerTransfer` | 保護 AI Comment create、fixed role／owner snapshot與 transfer後歷史不回寫；現況 list由 human client完成，因此不能替代新的 AI Bearer list cases。 | PF01-QA-01、08、09 |
| PF01-REG-09 | `TestWorkspaceProjectEndpointsTests.WorkspaceProjectFlow_EnforcesMembershipVisibilityAndOptimisticConcurrency` | 保護 Workspace membership visibility及 Project link基線。 | PF01-QA-03～07、16 |

## Future Automation Boundary

- 以 public HTTP seams驗證 `ListComments`與`CreateComment`，不對 private actor/scope helper綁定 assertions。
- Project情境至少覆蓋 Issue；TestManagement情境以 Test Case、Test Plan、Test Run parameterized matrix覆蓋。
- scope／lifecycle transition需在同一case中保存變更前成功與變更後拒絕 evidence，才能發現 stale cache。
- unauthorized-existing與不存在資源需比較 status、公開 Problem Details fields、headers與body敏感值；不以人類訊息文字作 branching assertion。
- 若 automation需要直接建立 removed／missing owner狀態，只能使用既有 integration fixture或正式 application seam；不得用 production migration或產品 API創造未定義行為。

## PM Handoff Package

- **Task / Feature**：PF-01 AI Comment owner-derived Project／Workspace scope remediation。
- **Current Phase**：新標準 remediation track 的 `Parallel Design`；QA artifact已完成，未推進至 Artifact Review、Parallel Implementation或 Formal QA。
- **Source-of-Truth Paths**：`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-disposition-parallel-design-handoff.md`；`doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-pm-artifact-review.md`；`doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md` §4、§5、§9、§12；`doc/plans/2026-08-25-comment-traceability-ai-capability-implementation-review-conclusion-reviewer-ai.md` PF-01；`doc/plans/2026-08-25-comment-traceability-ai-capability-remediation-intake.md`；`doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`；`contract/openapi/khaikang.v1.yaml` 的 `ListComments`／`CreateComment`。
- **Scope / Non-Goals**：只產出 PF01-QA-01～16、risk matrix與PF01-REG-01～09 legacy selection；不改產品規格、fixed-role matrix、token lifecycle、Comment whitelist或idempotency；未實作 automation、執行 Formal QA、修改產品 code／migration／OpenAPI、commit、push、deploy或推進 phase。
- **Produced Artifact**：本文件 `doc/plans/2026-08-25-comment-traceability-ai-capability-pf01-qa-cases.md`。
- **Validation Evidence**：逐條比對 PM confirmed behavior、產品規格 §4／§9、Development Specification actor／scope admission，以及 OpenAPI `ListComments`／`CreateComment` 的 security與non-disclosing `401`／`404` boundary；PF01-QA-01已依 `PF01-QA-ROLE-TARGET` 唯一直接修正，改為五角色allowed target／assignment matrix及一個Reviewer denied target，PF01-QA-02～16未變更；以 `rg`確認九個 legacy test方法目前存在。本階段未執行任何 test、build、runtime或Formal QA。
- **Open Decisions / Blockers**：產品語意無新增 blocker。現有 OpenAPI對 unauthorized／unavailable resource已固定 `404`與non-disclosure，但尚未固定專用 Problem Details `type`／`code`；未來 cases應先斷言 status、公開shape與不可區分性，不自行發明新code。專案目前未找到獨立 Project Workflow Record；本次依正式 PM handoff明列的role、phase與profiles執行，PM應另行確認此治理文件缺口，但它不改變本PF-01產品案例。`PF01-QA-ROLE-TARGET` 已使用本次唯一直接修正；若 PM recheck仍判定同family有問題，QA停止修正並升PM。
- **Next Role and Required Action**：PM AI只需recheck `PF01-QA-ROLE-TARGET` correction並決定PF-01 Parallel Design checkpoint；QA AI等待下一份明確Handoff，不自行進入Parallel Implementation、實作automation或執行Formal QA。

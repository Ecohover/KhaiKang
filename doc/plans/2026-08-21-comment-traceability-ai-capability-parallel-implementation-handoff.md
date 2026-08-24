# PM Checkpoint 與正式 Handoff：Comment、追溯與 AI 操作紀錄能力

## PM Checkpoint

- Workflow Role：PM AI
- Checkpoint：Parallel Design complete；授權進入 Parallel Implementation
- PO authorization：已確認產品語意、DS-03 failure semantics，並指示依建議繼續。
- 此 Handoff 授權實作與依下列 commit strategy 建立 focused local commits；不授權自行改寫產品語意、略過 Reviewer／Formal QA、push、部署或宣告驗收完成。

## 已閉合的設計證據

| 範圍 | 證據 | 結果 |
| --- | --- | --- |
| 產品語意與驗收案例 | `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`、`doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md` | 已由 PO 確認；QA artifact 完成案例語意檢查，非 Formal QA。 |
| DS-01：固定角色與 Run Item 追溯不可變性 | `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`、`doc/plans/2026-08-21-comment-traceability-ai-capability-development-specification-recheck-conclusion-reviewer-ai.md` | CLOSED。 |
| DS-02：token lifecycle 權限矩陣與 plaintext 邊界 | 同上兩份 artifact | CLOSED。 |
| DS-03：idempotency failure semantics | `doc/plans/2026-08-21-comment-traceability-ai-capability-ds03-pm-disposition-handoff.md`、`doc/plans/2026-08-21-comment-traceability-ai-capability-ds03-reviewer-recheck-conclusion.md` | CLOSED。 |

## Task / Feature

實作 Comment、Issue/Test 追溯與 AI 操作紀錄能力。外部 AI Skill 的 workflow、Grill、Handoff、orchestration 與自動化不屬於本 feature；KhaiKang 僅提供受控 API 與記錄能力。

## Source of Truth（必讀順序）

1. `AGENTS.md` 與 `.ai/INDEX.md`。
2. `doc/zh-TW/02-specs/05-comment-and-ai-recording-capability.md`。
3. `doc/plans/2026-08-20-comment-traceability-ai-capability-development-specification-dev-ai.md`。
4. `doc/plans/2026-08-20-comment-traceability-ai-capability-qa-cases.md`。
5. `doc/plans/2026-08-21-comment-traceability-ai-capability-ds03-pm-disposition-handoff.md`。
6. 現有 Identity、Issue/Test traceability 規格與資料模型；衝突或未決語意一律回 PM／human PO，不得自行推定。

## Authorized Implementation Scope

- Identity：AI account type、固定角色與唯一 human owner、API enablement、單一永久 bearer token 的安全生命週期、稽核與歷史 snapshot；嚴禁 token plaintext 落入 log、audit、資料投影、URL、前端持久狀態或 admin response。
- CommentManagement：只為 Issue、Test Case、Test Plan、Test Run 建立／列出 Comment，依資源 owner access verdict 授權、Markdown 安全邊界、soft-delete visibility 與穩定 newest-first paging。
- ProjectManagement／TestManagement：規格指定的可變 links 與不可變來源 links；Run Item historical source 不得 delete、replace、soft-delete 或 unlink。
- Idempotency：各 module 自有 record 與 DbContext。reservation 的 key 與 canonical fingerprint 一經建立永不替換；terminal success、terminal business rejection 與 incomplete transient failure 必須符合 DS-03 disposition。
- Frontend：typed API、CommentPanel、四種資源 view、AI badge／歷史 snapshot，及 owner/admin token management UI 的最小權限邊界。

## Required Engineering Evidence

- 所有新增／變更 HTTP operation 先以現有 OpenAPI-first 慣例定義並同步 C# endpoint、TypeScript client、Problem Details、security／CSRF。
- 每個 module 的 migration 僅由該 module owner DbContext 產生；不可跨 module DbContext 或直接讀取他 module entity。
- 使用真實 disposable PostgreSQL 驗證 unique／FK／check、transaction、lock、concurrency、terminal rejection replay、transient rollback and recovery 與 payload-substitution conflict；EF InMemory 不可作為此類證據。
- 執行與變更風險相稱的 restore/build/test/format、frontend type-check/test/build、OpenAPI lint/bundle/compatibility；回報實際命令與結果。
- 明確區分：Development verification、Reviewer evidence、Formal QA Run、手動 acceptance；任何一者不得替代另一者。

## Non-Goals / Guardrails

- 不實作外部 AI Skill、AI workflow、自動化交接或多 AI 協作編排。
- 不加入未獲 PO 確認的 token retention／cleanup 政策、角色切換、多 token、機器 identity 或額外 AI account semantics。
- 不以 System Admin 或 owner 的人類高權限繞過 AI fixed-role 與 Project/Workspace intersection。
- 不因本 Handoff 自行 push、deploy、進行 Formal QA 或宣告 release；依既有 workflow 分別交接。

## Commit Strategy（PO confirmed）

- 本 feature 完成時**預估**約為 5–8 個 focused commits；這不是硬性上限或下限。commit 依可編譯、可驗證的 vertical slice、風險與實際 diff 狀況劃分，不以檔案數量、暫存內容方便性或湊足數量切分。
- 每個 commit 只包含其 slice 所需的 code、migration、contract、tests 與必要的文件更新；不得使用 `git add -A`、混入無關 dirty changes，或提交未完成的 half-wiring。
- 預期切分順序：Comment core／idempotency；AI Identity/token；fixed-role admission 與 operation snapshots；traceability immutable links；frontend Comment/AI UI；cross-boundary verification 與必要修正。實際切分可依可查驗 diff、相依性與驗證結果調整；若明顯偏離預估，於 Development Handoff 說明理由即可。
- PO 已授權在本 Handoff 範圍內，於 slice 已完整且有相稱 verification evidence 時建立 focused local commit；不得預先提交 half-wiring。push 仍需另行授權。

## Handoff Recipients and Next Action

| Role | Required action |
| --- | --- |
| Development AI | 依本 Handoff 實作；每個完成且已驗證的 vertical slice 可建立 focused local commit，產出 focused test／build／contract evidence 與 Development Handoff 給 Reviewer AI。 |
| Reviewer AI | 僅在收到 Development Handoff 與可查驗 diff/evidence 後審查規格與 standards；若發現問題，遵循單一直接修正／PM escalation 規則。 |
| QA AI | 可依既有 CMT/TRC/SNAP/AI cases 並行實作 QA automation；僅在 PM 發出 Formal QA Run Handoff 且 Implementation Review 完成後執行 Formal QA。不能將設計案例或 Expected Red 當作測試結果。 |
| PM AI | 接收 remediation、維護產品語意與 gate；任何新產品決策回 human PO。 |

## Exit Criteria

本 Handoff 的完成條件是可審查的 Development implementation evidence，不是產品完成。後續必須依序完成 Reviewer、Formal QA、PM checkpoint 與 human acceptance／release 授權。

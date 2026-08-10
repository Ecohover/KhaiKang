# 發布自動化與交付演練計畫

狀態：2026-08-10 已確認實作方向；自動化與隔離交付演練尚未完成。英文同步文件：
[Release Automation and Delivery Drill Plan](../../en/planning/release-automation.md)。

## 目的

定義一條可重複使用的流程，把已驗收的修改發布成 Git tag、GitHub Release
與不可變的 API／Web Docker images。版本與發布決定保留人工關卡，驗證、打包、
發布及證據整理則交由自動化完成。

本文件是發布流程實作的 source of truth；文件本身不代表授權 Agent 或腳本執行
真實發布。

## 預期成果

- 開發者準備 RC 或正式版本時，不需要手動修改散落在多份文件的版本字串。
- Pull Request 只執行驗證，不發布 image。
- `rc` 或 `main` 的已驗收 commit 可以透過一個受保護的命令發布。
- 每次發布可追溯來源 commit、Git tag、GitHub Actions run、image tag 與 digest。
- 已發布 image 可在全新 PostgreSQL／附件 volumes 啟動，並完成備份與隔離還原。

## 不處理範圍

- 自動合併 Pull Request。
- 每次 feature branch push 都發布 image。
- 自動部署到正式主機。
- 歷史安全性維護分支。
- Object Storage、image signing、正式弱點政策或額外 SBOM 政策。

## 分支與版本模型

| 來源 | 用途 | 允許版本 | 發布 tag |
| --- | --- | --- | --- |
| `ecohover/feature/*`、`ecohover/chore/*` | 隔離開發 | 準備發布前沿用基準版本 | 不發布 |
| `rc` | 已驗收的 RC 整合 | prerelease 或 stable candidate | `rc`、`sha-<commit>` |
| `main` | 正式版本 | stable semantic version | `latest`、`sha-<commit>` |
| Git tag `vX.Y.Z-rc.N` | 不可變 RC | 完全相同的 prerelease | `X.Y.Z-rc.N`、`sha-<commit>` |
| Git tag `vX.Y.Z` | 不可變正式版 | 完全相同的 stable version | `X.Y.Z`、`latest`、`sha-<commit>` |

`VERSION` 是目前產品版本唯一的 source of truth。歷史 release note 保留原本版本；
一般文件範例應使用 placeholder 或 `rc`，只有描述不可變歷史版本時才固定版號。

## 必要人工關卡

專案負責人只需要人工決定或明確授權：

1. 選擇下一個 semantic version。
2. 審閱並完成 Release Note。
3. 審閱並合併到 `rc`；正式版則再由 `rc` 合併到 `main`。
4. 必要檢查通過後，明確執行或授權發布命令。

正常流程不需要人工執行 Docker build／tag／push、不需要手動建立 Git tag，也不需要
人工計算 image digest。

## 規劃中的自動化

### `deploy/Prepare-Release.ps1`

準備命令負責：

- 要求乾淨且符合目標的 feature／chore branch；
- 驗證 semantic version 與 release channel；
- 更新 `VERSION`；
- 由 template 建立 `doc/releases/v<version>.md`，且不得覆寫既有文件；
- 顯示目標分支與後續命令；
- 絕不自行 commit、push、merge、tag 或發布。

### Pull Request 驗證

以 `rc` 或 `main` 為目標的 PR 必須執行：

- 後端 restore、Release build、unit／integration tests；
- 前端 frozen install、type check、tests 與 production build；
- `VERSION` 變動時的 semantic version 與 release-note 一致性檢查；
- 不需要發布 secrets 的 workflow 與文件檢查。

PR 驗證不得登入 Docker Hub，也不得發布 image。

### 既有 Docker 發布 workflow

Docker workflow 繼續作為遠端打包器：

- push 到 `rc` 發布 `rc` 與 `sha-<commit>`；
- push 到 `main` 發布 `latest` 與 `sha-<commit>`；
- 精確 Git tag 發布相同的不可變 semantic-version tag；
- API 與 Web image 推送前，後端與前端驗證都必須通過。

### `deploy/Publish-Release.ps1`

受保護的發布命令負責：

- prerelease 只能在 `rc` 執行，stable 只能在 `main` 執行；
- 要求乾淨 worktree，且 `HEAD` 必須等於對應 remote branch；
- 要求精確 `VERSION`、相符 release note，且本機與遠端都不存在同名 Git tag；
- 要求相同 commit 的 branch Docker workflow 已成功；
- 確認前顯示版本、分支、commit 與預計發布的 image tags；
- 確認後才建立並推送 annotated Git tag；
- 等待 tag workflow，失敗時停止；
- 建立對應 GitHub prerelease 或 stable release；
- 核對 API／Web 不可變 tags 並記錄 digests；
- 絕不移動既有 tag，也不覆寫既有 GitHub Release。

必須提供 validation-only 模式，只執行所有前置檢查，不改變任何遠端狀態。

## RC 發布流程

1. 在 `ecohover/feature/*` 或 `ecohover/chore/*` 開發並完成驗證。
2. 執行 `Prepare-Release.ps1 -Version X.Y.Z-rc.N`。
3. 完成 Release Note、commit、push，並建立到 `rc` 的 PR。
4. 審閱 PR 並等待驗證。
5. 合併到 `rc`，等待 `rc` image workflow。
6. 完成必要驗收或交付演練。
7. 執行 `Publish-Release.ps1`，建立不可變 tag 與 prerelease。

## 正式版發布流程

1. 透過 `rc` 上的受審變更準備 stable `X.Y.Z` 版本。
2. 完成 RC 驗收，將 `rc` 合併到 `main`。
3. 等待 `main` workflow 從完全相同的 commit 發布 `latest`。
4. 在 `main` 執行 `Publish-Release.ps1`，建立 `vX.Y.Z`、不可變 image tags 與
   stable GitHub Release。

## 隔離交付與還原演練

第一版實作必須在不影響現有開發資料的情況下演練：

1. 使用唯一 Compose project name，以及全新的 PostgreSQL、附件與 data protection volumes。
2. 使用已發布的不可變 RC images，不使用本機 source build。
3. 驗證 health、首次設定／登入、Project／Issue、Workspace／Case／Plan／Run，
   以及附件上傳／下載。
4. 重啟所有應用容器，確認資料庫與附件仍存在。
5. 將 PostgreSQL 與附件 volume 備份為同一組 release set，manifest 記錄版本、
   時間與來源 image tags。
6. 還原到第二組唯一命名的資源。
7. 確認還原後的資料、Test Run snapshots、附件原始檔名與下載內容。
8. 檢查解析後的名稱，再只移除明確命名的 disposable drill resources。

## 驗收條件

- RC 準備不需要手動修改散落的目前版號。
- PR 驗證不需要發布 secrets，且不可能發布 image。
- validation-only 模式可列出所有預計遠端變更，但不執行變更。
- 經明確授權的 RC 發布會由相同 commit 建立一個不可變 Git tag、一個 GitHub
  prerelease，以及精確 API／Web image tags。
- 重複發布既有版本時必須失敗，且不能改動既有版本。
- 全新 image 部署、重啟持久化、備份與隔離還原皆成功並留下證據。
- 實作內容與實際演練結果同步更新中英文文件。

## 實作順序

1. 移除非歷史文件中固定的目前版本範例。
2. 新增 Release Note template 與準備檢查。
3. 新增發布前置檢查與 validation-only 模式。
4. 將 PR 驗證與 Docker 發布責任分開。
5. 新增備份／還原腳本與 manifest 格式。
6. 執行隔離 RC2 演練並記錄證據。
7. 只有在專案負責人明確要求時，才執行真實發布。

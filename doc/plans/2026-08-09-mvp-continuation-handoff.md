# MVP 跨電腦開發交接

## Objective / 目標

在另一台電腦接續 `ecohover/feature/20260802` 分支的 MVP 開發與測試。開發環境只使用 Docker 啟動 PostgreSQL，API 與前端維持本機開發模式。

## Accepted Decisions / 已確認決策

- MVP 核心功能已完成並可在本機操作與展示；後續先處理實際測試發現的問題。
- Test Run 進入 `in_progress` 後鎖定快照範圍與結構，但不限制特定執行者；具權限的 Workspace 成員可共同執行。
- 附件先採本機檔案儲存，不在本次 MVP 導入 S3、MinIO、病毒掃描或預覽處理。
- 完整桌面／手機人工驗收、CI／Jenkins、Playwright、自動結果匯入與 AI 測試不在本次 MVP 範圍。
- Case、Plan、Run 與 Project Issue 的對外編號已改由 PostgreSQL function 與 counter table 在建立交易中原子配置。
- 目前三個 baseline migration 尚未正式發布，因此直接整合最終 schema，不另外保留開發期間的增量 migration。

## Current State / 目前狀態

- Repository：`https://github.com/Ecohover/KhaiKang.git`
- Branch：`ecohover/feature/20260802`
- 本交接建立前的基準 commit：`6c27361 fix: allocate scoped numbers atomically`
- MVP 狀態文件：[中文](../zh-TW/04-planning/06-mvp-release-closure-plan.md)／[English](../en/planning/mvp-release-closure-plan.md)
- API 預設網址：`http://localhost:5220`
- 前端預設網址：`http://localhost:5173`
- 附件設定：local provider、相對路徑 `data/attachments`、單檔上限 20 MiB。
- `.run/` 是本機執行紀錄，不需搬移或提交。

### 在另一台電腦取得程式碼

第一次 clone：

```shell
git clone https://github.com/Ecohover/KhaiKang.git
cd KhaiKang
git switch --track origin/ecohover/feature/20260802
git pull --ff-only
```

若本機已經有 repository 與分支：

```shell
git switch ecohover/feature/20260802
git pull --ff-only
```

### 必要環境

- .NET 10 SDK
- Node.js 22
- pnpm 10
- Docker Desktop；只用於 PostgreSQL

### 啟動 PostgreSQL

新環境建議使用全新的資料庫，以確保 baseline migrations 完整建立 schema：

```shell
docker run --name khaikang-postgres -e POSTGRES_DB=khaikang -e POSTGRES_USER=khaikang -e POSTGRES_PASSWORD=<strong-local-password> -p 5432:5432 -v khaikang-postgres-data:/var/lib/postgresql/data -d postgres:17-alpine
```

若容器已建立但停止：

```shell
docker start khaikang-postgres
```

### 還原工具、設定連線並套用 migrations

在 repository 根目錄執行：

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
dotnet tool restore
dotnet user-secrets set --project backend/src/KhaiKang.Api "ConnectionStrings:KhaiKang" "Host=localhost;Port=5432;Database=khaikang;Username=khaikang;Password=<strong-local-password>"
dotnet ef database update --context KhaiKang.Modules.Identity.Infrastructure.IdentityDbContext --project backend/src/modules/KhaiKang.Modules.Identity --startup-project backend/src/KhaiKang.Api
dotnet ef database update --context KhaiKang.Modules.ProjectManagement.Infrastructure.ProjectManagementDbContext --project backend/src/modules/KhaiKang.Modules.ProjectManagement --startup-project backend/src/KhaiKang.Api
dotnet ef database update --context KhaiKang.Modules.TestManagement.Infrastructure.TestManagementDbContext --project backend/src/modules/KhaiKang.Modules.TestManagement --startup-project backend/src/KhaiKang.Api
```

### 本機啟動 API 與前端

API 終端機：

```shell
dotnet run --project backend/src/KhaiKang.Api/KhaiKang.Api.csproj
```

前端終端機：

```shell
cd frontend
pnpm install
pnpm dev
```

啟動後先確認 `http://localhost:5220/health/live`，再開啟 `http://localhost:5173`。

### 搬移既有測試資料時的注意事項

- Git push 不包含 PostgreSQL 資料、.NET user secrets 或 `data/attachments` 內的附件實體檔案。
- 不需要既有測試資料時，直接建立全新資料庫最簡單也最安全。
- 需要保留既有資料時，請另外使用 `pg_dump`／`pg_restore` 搬移資料庫，並確認來源資料庫已包含最新的 counter tables 與 functions。
- 需要保留既有附件時，還要另外複製 API 執行目錄所解析到的 `data/attachments`；只搬資料庫會留下附件 metadata，但下載時找不到實體檔案。
- 三個 baseline migration IDs 曾在尚未發布期間原地更新。若目標資料庫曾套用 commit `6c27361` 以前相同 ID 的舊 migration，EF 不會重跑它們；應優先重建測試資料庫，或手動補齊 counter tables、seed values 與 functions。

## Verification / 已完成驗證

- 全新隔離 PostgreSQL 資料庫可依序套用 Identity、Project Management、Test Management 三個 baseline migrations。
- 全部 36 張 public tables 與兩個編號配置 functions 均已建立。
- Issue、Case、Plan、Run 各自以 8 個平行請求配置編號，結果皆為不重複的 `1` 至 `8`。
- 編號配置與建立資料共用交易，rollback 不會消耗 counter。
- Backend release build 通過，0 warnings、0 errors。
- Backend integration tests 8/8、unit tests 13/13 通過。
- 本次修改的 C# 檔案通過 scoped `dotnet format --verify-no-changes`。
- 中英文文件的本機 Markdown links 檢查為 50 份文件、0 broken links。
- API 與前端曾在本機啟動並回傳 HTTP 200。
- Repository 全域 format 檢查仍會受到既有大量 CRLF line-ending findings 影響；目前只確認本次變更範圍。

## Remaining Work / 後續工作

MVP 核心沒有已知阻擋項目。接手後建議依序：

1. 在另一台電腦使用全新 PostgreSQL database 完成一次 migrations 與啟動 smoke test。
2. 依實際操作結果修正 MVP 問題，特別留意權限、跨 Workspace／Project 範圍與附件讀寫。
3. 決定是否需要搬移目前測試資料與附件；若不需要，維持乾淨測試資料庫即可。
4. MVP 穩定後，再排入完整桌面／手機驗收、CI、Playwright、自動匯入、AI、物件儲存與正式環境升級／備份演練。

## Context To Load / 建議先讀文件

- [Repository 指引](../../AGENTS.md)
- [中文 README](../../README.zh-TW.md)
- [English README](../../README.md)
- [中文 MVP 收尾計畫](../zh-TW/04-planning/06-mvp-release-closure-plan.md)
- [English MVP closure plan](../en/planning/mvp-release-closure-plan.md)
- [舊功能修補計畫（已結案／歷史文件）](../zh-TW/04-planning/06-current-feature-remediation-plan.md)

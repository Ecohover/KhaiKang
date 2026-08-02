# KhaiKang

KhaiKang 是一套為個人開發者及中小型軟體團隊設計的開源、可自架式開發與測試管理平台。

它將需求、開發規劃、程式實作、測試管理、品質條件與 CI/CD 回饋串成完整流程，同時繼續使用 Jenkins、GitHub Actions、Playwright 等專業外部工具負責實際執行。

KhaiKang 以 Kanban 持續流動為優先，同時保留 Scrum 相容性。AI 可以在每個流程階段提供協助，各專案也能決定是否需要人工檢核後才能繼續。

> AI 負責提案，需要時由人員核准，自動化工具負責執行，KhaiKang 負責追蹤。

## 產品方向

KhaiKang 的定位是現有工程工具上層的管理與協調中樞，不取代原始碼平台、CI Server、部署引擎或測試框架。

目標交付流程如下：

1. 紀錄需求。
2. AI 分析需求及相關 Git Repository。
3. 提出技術方案、開發計畫、驗收條件與測試案例。
4. 功能程式與測試程式並行開發。
5. 視專案需要執行單元、整合、API、端對端及人工測試。
6. 將同一份 Build Artifact 逐步晉級至專案定義的環境。
7. 每個階段依可設定的品質條件及選擇性人工檢核判斷是否繼續。
8. 產生需求層級的品質報告並核准正式部署。

MVP 階段採用線性、模板化的環境流程；底層資料模型保留未來修改能力，但第一版不實作完整的視覺化流程設計器。

詳細內容請參閱[產品願景與 MVP 工作流程](./doc/zh-TW/01-overview/04-product-vision-and-mvp-workflow.md)。

## 快速開始

前置需求：[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)、
PostgreSQL、Node.js 22 與 pnpm 10。

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
dotnet tool restore
dotnet user-secrets set --project backend/src/KhaiKang.Api "ConnectionStrings:KhaiKang" "Host=localhost;Port=5432;Database=khaikang;Username=khaikang;Password=<你的本機密碼>"
dotnet ef database update --project backend/src/modules/KhaiKang.Modules.Identity --startup-project backend/src/KhaiKang.Api
dotnet run --project backend/src/KhaiKang.Api/KhaiKang.Api.csproj
```

開發設定預設使用 `http://localhost:5220`：

- 健康檢查：`GET /health/live`
- 系統資訊：`GET /api/v1/system/info`
- Canonical OpenAPI 合約：`GET /openapi/v1.yaml`

後端驗證指令：

```shell
dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1
dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1
```

另開終端機啟動前端：

```shell
cd frontend
pnpm install
pnpm dev
```

Frontend workspace 指令請參考 [frontend/README.md](./frontend/README.md)。

## 文件

- [English README](./README.md)
- [產品願景與 MVP 工作流程](./doc/zh-TW/01-overview/04-product-vision-and-mvp-workflow.md)
- [英文產品願景與 MVP 工作流程](./doc/en/product-vision-and-mvp-workflow.md)
- [貢獻指南](./CONTRIBUTING.md)
- [Coding Agent 規範](./AGENTS.md)
- [文件規範](./doc/documentation-guidelines.md)
- [開發規範](./doc/zh-TW/01-overview/03-development-guidelines.md)
- [AI 與 OpenAPI 協作開發流程](./doc/zh-TW/01-overview/06-ai-openapi-development-workflow.md)
- [Canonical OpenAPI 合約](./contract/openapi/khaikang.v1.yaml)
- [英文架構總覽](./doc/en/architecture/overview.md)
- [繁體中文文件導覽](./doc/zh-TW/01-overview/01-documentation-map.md)
- [繁體中文架構總覽](./doc/zh-TW/01-overview/02-architecture-overview.md)
- [目前階段規劃](./doc/zh-TW/04-planning/01-phase-overview.md)

## 授權

本專案採用 [Apache License 2.0](./LICENSE) 授權。

Copyright 2026 KhaiKang Contributors.

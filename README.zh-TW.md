# KhaiKang

KhaiKang 是一套開源、可自行部署的開發與測試管理平台，適合個人開發者與中小型軟體團隊。

它串連需求、交付規劃、實作、測試管理、品質門檻與 CI/CD 回饋；同時持續使用
GitHub Actions、Jenkins、Playwright 等專門工具，而非取代它們。

> AI 提供建議；需要時由人員核准；自動化負責執行；KhaiKang 負責追蹤。

## 快速開始

### 透過 Docker 執行（建議先評估使用）

Docker Hub 已提供 Web 與 API image；Compose 會一併啟動 PostgreSQL。只需要安裝
Docker Engine 與 Docker Compose v2。

```shell
git clone https://github.com/Ecohover/KhaiKang.git
cd KhaiKang/deploy/compose
cp .env.example .env
# 編輯 .env，將 POSTGRES_PASSWORD 改為足夠長的隨機密碼。
docker compose pull
docker compose up -d
```

開啟 `http://localhost:8080`，建立第一個系統管理員帳號。

升級、備份、HTTPS 與正式環境的做法請見[ Docker 部署說明](./deploy/README.md)。正式環境
建議使用固定的 `sha-...` image tag，而不是 `latest`。

### 從原始碼執行

需要 [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)、PostgreSQL、
Node.js 22 與 pnpm 10。

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile backend/NuGet.config
dotnet tool restore
dotnet user-secrets set --project backend/src/KhaiKang.Api "ConnectionStrings:KhaiKang" "Host=localhost;Port=5432;Database=khaikang;Username=khaikang;Password=<your-local-password>"
dotnet ef database update --context KhaiKang.Modules.Identity.Infrastructure.IdentityDbContext --project backend/src/modules/KhaiKang.Modules.Identity --startup-project backend/src/KhaiKang.Api
dotnet ef database update --context KhaiKang.Modules.ProjectManagement.Infrastructure.ProjectManagementDbContext --project backend/src/modules/KhaiKang.Modules.ProjectManagement --startup-project backend/src/KhaiKang.Api
dotnet ef database update --context KhaiKang.Modules.TestManagement.Infrastructure.TestManagementDbContext --project backend/src/modules/KhaiKang.Modules.TestManagement --startup-project backend/src/KhaiKang.Api
dotnet run --project backend/src/KhaiKang.Api/KhaiKang.Api.csproj
```

開發環境 API 預設使用 `http://localhost:5220`。前端請在另一個終端機執行：

```shell
cd frontend
pnpm install
pnpm dev
```

## 文件與參與方式

- [English README](./README.md)
- [Docker 部署說明](./deploy/README.md)
- [安全性政策](./SECURITY.md)
- [貢獻指南](./CONTRIBUTING.md)
- [社群行為準則](./CODE_OF_CONDUCT.md)
- [變更紀錄](./CHANGELOG.md)
- [產品願景與 MVP 工作流程](./doc/zh-TW/01-overview/04-product-vision-and-mvp-workflow.md)
- [文件地圖](./doc/zh-TW/01-overview/01-documentation-map.md)
- [English Documentation Map](./doc/en/documentation-map.md)

## 授權

本專案採用 [Apache License 2.0](./LICENSE) 授權。

Copyright 2026 KhaiKang Contributors.

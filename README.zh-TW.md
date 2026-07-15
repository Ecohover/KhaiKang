# KhaiKang

KhaiKang 是一套開源、可自架的工程團隊工作流平台。

## 快速開始

前置需求：[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)。

```shell
dotnet restore backend/KhaiKang.Backend.slnx --configfile NuGet.config
dotnet run --project backend/src/KhaiKang.Api/KhaiKang.Api.csproj
```

開發設定預設使用 `http://localhost:5220`：

- 健康檢查：`GET /health/live`
- 系統資訊：`GET /api/v1/system/info`

後端驗證指令：

```shell
dotnet build backend/KhaiKang.Backend.slnx --no-restore --disable-build-servers -m:1
dotnet test backend/KhaiKang.Backend.slnx --no-build --disable-build-servers -m:1
```

Frontend 與 Docker Compose workspace 尚未建立。

## 文件

- [English README](./README.md)
- [貢獻指南](./CONTRIBUTING.md)
- [Coding Agent 規範](./AGENTS.md)
- [文件規範](./doc/documentation-guidelines.md)
- [開發規範](./doc/zh-TW/01-overview/03-development-guidelines.md)
- [英文架構總覽](./doc/en/architecture/overview.md)
- [繁體中文文件導覽](./doc/zh-TW/01-overview/01-documentation-map.md)
- [繁體中文架構總覽](./doc/zh-TW/01-overview/02-architecture-overview.md)
- [目前階段規劃](./doc/zh-TW/04-planning/01-phase-overview.md)

## 授權

本專案採用 Apache License 2.0 授權。

Copyright 2026 KhaiKang Contributors.

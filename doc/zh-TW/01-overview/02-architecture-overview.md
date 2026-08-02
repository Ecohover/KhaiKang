# 架構總覽

## 產品方向

KhaiKang 採用 modular monolith 的方式設計。
目標是在維持地端部署簡單的前提下，仍保有清楚的模組邊界與長期可維護性。

## 技術棧

- 前端：Vue 3 + TypeScript
- 後端：C# + ASP.NET Core on .NET 10
- 資料庫：PostgreSQL
- 快取：Redis
- 訊息佇列：RabbitMQ
- 部署：Docker Compose

## 檔案結構

```text
doc/
  zh-TW/
    01-overview/
    02-specs/
    03-data-model/
    04-planning/
deploy/
  docker/
  compose/
  scripts/
backend/
  KhaiKang.Backend.slnx
  src/
    common/
    modules/
    KhaiKang.Api/
    KhaiKang.Worker/
  tests/
    integration/
frontend/
contract/
  openapi/
tests/                 # 有跨元件 E2E 測試時才建立
```

- `doc/zh-TW/01-overview/`：文件導覽、架構說明、語詞總表與通用規範
- `doc/zh-TW/02-specs/`：功能規格文件
- `doc/zh-TW/03-data-model/`：資料模型與資料欄位說明
- `doc/zh-TW/04-planning/`：階段規劃與待處理清單
- `deploy/`：Docker Compose、腳本、環境範例
- `backend/`：.NET backend workspace、Solution、source 與後端測試
- `backend/src/common/`：穩定且 domain-neutral 的後端共用元件
- `backend/src/modules/`：依功能組織的 modular-monolith project
- `backend/tests/`：後端 unit 與 integration test
- `frontend/`：Vue 3 前端應用
- `contract/openapi/`：前後端共同的 canonical HTTP wire contract；C# 與 TypeScript 實作留在各自 workspace
- root `tests/`：需要跨前後端或部署層 E2E 測試時才建立

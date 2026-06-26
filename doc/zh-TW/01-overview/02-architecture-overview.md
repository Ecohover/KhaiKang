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
src/
  contracts/
  backend/
  frontend/
  worker/
```

- `doc/zh-TW/01-overview/`：文件導覽、架構說明、語詞總表與通用規範
- `doc/zh-TW/02-specs/`：功能規格文件
- `doc/zh-TW/03-data-model/`：資料模型與資料欄位說明
- `doc/zh-TW/04-planning/`：階段規劃與待處理清單
- `deploy/`：Docker Compose、腳本、環境範例
- `src/contracts/`：API、DTO、enum、error、event 契約
- `src/backend/`：ASP.NET Core 後端應用與各模組
- `src/frontend/`：Vue 3 前端應用
- `src/worker/`：背景任務、queue consumer、非同步處理

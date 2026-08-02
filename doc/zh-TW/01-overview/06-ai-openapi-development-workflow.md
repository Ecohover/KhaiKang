# AI 與 OpenAPI 協作開發流程

## 決策

KhaiKang 採用 **OpenAPI-first、AI-implemented、compiler-verified** 的開發方式。

- `contract/openapi/khaikang.v1.yaml` 是公開 HTTP wire contract 的唯一來源。
- C# DTO、HTTP endpoint、TypeScript DTO 與 HTTP client 是正式維護的原始碼。
- 專案不要求 NSwag、OpenAPI Generator 或其他 code generator。
- AI 依 OpenAPI 與既有程式更新前後端，但 AI 的判斷不能取代編譯、測試與 review。

這項決策讓特殊的 authentication、CSRF、錯誤處理、模組邊界與前端互動可以直接保留在程式碼中，不需要修改 generator template，也不會在重新產生程式碼時被覆蓋。

## Source of truth

同一功能的資訊依下列責任分工：

1. 功能規格定義 use case、商業規則、安全與驗收條件。
2. OpenAPI 定義 route、operation ID、HTTP method、request、response、status、nullability、enum、安全需求與範例。
3. C# 與 TypeScript 實作 OpenAPI 合約及各自的架構需求。
4. Migration 定義資料庫 schema 演進，不從 OpenAPI 推導 entity 或資料表。

若規格、OpenAPI 與程式衝突，不得由 AI 靜默選擇其中一份；應先指出差異並修正具權威性的來源。

## 每個 API 變更的流程

1. 與 AI 收斂需求、錯誤情境與驗收條件。
2. 先更新功能規格與 `contract/openapi/khaikang.v1.yaml`。
3. Review 受影響的 operation ID、schema 與相容性。
4. AI 讀取現有實作後，同步修改：
   - 所屬後端模組的 C# request／response DTO；
   - endpoint 或 controller；
   - application、domain、persistence 與 migration（需要時）；
   - 前端 TypeScript wire types 與 feature HTTP client；
   - UI、store 與測試。
5. AI 必須保留不違反新合約的既有客製行為，不得把整份檔案視為可覆蓋的 generated output。
6. 執行後端 build/test、前端 type-check/test/build 與必要的 HTTP contract integration test。
7. 在完成報告列出 OpenAPI operation 與前後端實作的對照結果。

## 程式碼位置

```text
contract/openapi/                 Canonical HTTP wire contract
backend/src/modules/<Feature>/
  Contracts/                      Feature-owned C# HTTP DTOs
  Endpoints/                      HTTP adapters
frontend/apps/web/src/api/
  contracts.ts                    TypeScript wire types
  client.ts                       Project-specific HTTP client
```

Host 專屬 DTO 可以留在 host 內；功能 DTO 應由所屬模組管理。`contract/` 不放 C# 或 TypeScript implementation project。

## AI 同步檢查

每次公開 API 修改至少檢查：

- route、method 與 operation ID；
- request／response required、nullable、format 與 enum；
- 成功與錯誤 status code；
- Problem Details `type` 與必要的 `code`；
- authentication、authorization、cookie 與 CSRF；
- C# DTO 與 JSON serialization；
- TypeScript DTO 與 HTTP client method；
- backend integration test 與 frontend type-check。

## 何時重新評估 generator

只有 API 數量、外部 SDK、跨 repository consumer 或多人並行修改已使手動同步成為可量測問題時，才重新評估 code generator。導入前必須先證明它能保留專案客製行為，並記錄版本、授權、升級與 generated code review 策略。

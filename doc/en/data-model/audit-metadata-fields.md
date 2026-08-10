# Audit Metadata Fields

Status: synchronized on 2026-08-09. Traditional Chinese counterpart: [Audit Info 結構](../../zh-TW/03-data-model/99-audit-metadata-fields.md).

Mutable MVP entities normally expose:

| Field | Type | Meaning |
| --- | --- | --- |
| `created_at` | `timestamp with time zone` | UTC creation time. |
| `created_by_account_id` | `uuid`, nullable where bootstrap/system writes require it | Creating account. |
| `updated_at` | `timestamp with time zone` | UTC last-update time. |
| `updated_by_account_id` | `uuid`, nullable | Last updating account. |
| `version` | `integer` | Optimistic concurrency version incremented by accepted writes. |

Dedicated audit-event tables record actor, actor type, event type, target, outcome, and occurrence time. Audit data is append-oriented and must not contain credentials, authentication cookies, file bytes, or sensitive request bodies.

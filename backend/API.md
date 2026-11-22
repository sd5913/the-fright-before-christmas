# TFBC Score API

Base URL: `https://metapool.cloud/tfbc-api` — no login or API key required.

## Submit a score

**POST `/v1/scores`** when the game ends. Set `Content-Type: application/json`.

```json
{
  "runId": "8dcfd251-7127-4bd8-9279-18c879a8c6e7",
  "playerName": "Billy",
  "score": 1250
}
```

All three fields are required; no additional fields are accepted.

| Field | Rules |
| --- | --- |
| `runId` | UUID; use UUID v4. Generate once per game run and reuse it for retries. |
| `playerName` | String, 1–24 Unicode characters after trimming. Chinese and emoji supported; no control characters. |
| `score` | JSON integer, −2147483648 to 2147483647. Zero and negative values allowed. |

Success: **201** for a new result, **200** for an identical retry.

```json
{
  "score": {
    "runId": "8dcfd251-7127-4bd8-9279-18c879a8c6e7",
    "playerName": "Billy",
    "score": 1250,
    "submittedAt": "2026-11-11T00:00:00.000Z"
  },
  "duplicate": false
}
```

An identical retry returns `duplicate: true` without creating another record. After a timeout or temporary failure, retry the same payload and run ID. A different name or score with an existing run ID returns **409**.

## Read scores

**GET `/v1/scores?limit=10`**. No request body. `limit` defaults to 10; allowed range: 1–100.

```json
{
  "scores": [
    {
      "rank": 1,
      "runId": "8dcfd251-7127-4bd8-9279-18c879a8c6e7",
      "playerName": "Billy",
      "score": 1250,
      "submittedAt": "2026-11-11T00:00:00.000Z"
    }
  ],
  "limit": 10
}
```

Ranks are per game run, ordered by score descending, then submission time ascending, then run ID. The same player name may appear more than once. An empty board returns `scores: []`. `submittedAt` is assigned by the server.

## Errors

Format: `{"error":{"code":"INVALID_SCORE","message":"..."}}`.

| HTTP | Meaning / action |
| --- | --- |
| 400 | Invalid fields or JSON; correct the request. |
| 403 | Website origin not allowed; request a CORS configuration update. |
| 409 | Run ID already used with different contents. |
| 413 / 415 | Keep JSON under 8 KiB and use `application/json`. |
| 429 | Rate limited; wait the seconds in `Retry-After` before retrying. |
| 500 / 503 | Temporary server failure; retry later with the same payload. |

Per-IP limits: **20 submissions and 300 reads per minute**.

Allowed browser origins: `https://tfbc.darketomaly.com`, `https://metapool.cloud`, `http://localhost:4321`, `http://127.0.0.1:4321`. Additional preview origins must be configured before browser use.

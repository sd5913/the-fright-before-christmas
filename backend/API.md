# TFBC score API — v1 contract

Status: **deployed**. Public health, empty-board reads, POST validation, CORS preflight and real browser cross-origin requests were verified on 2026-11-11. Game integration (#9) and replacing the frontend demo scores (#13) are still pending. No Unity files or Unity calling code are included.

## Addresses

| Environment | API base URL |
| --- | --- |
| Production | `https://metapool.cloud/tfbc-api` |
| Local full Docker stack | `http://127.0.0.1:3107/tfbc-api` |

Append the paths below to the base URL. Keep `/tfbc-api` in the path. There is no additional `/api` segment.

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/health` | Check API and database readiness |
| POST | `/v1/scores` | Submit the final score for one game run |
| GET | `/v1/scores?limit=10` | Read the highest scoring game runs |

Production submission target: `https://metapool.cloud/tfbc-api/v1/scores`.

## Submit a score

Call once the game ends and the final player name and score are available. Use HTTPS in production and `Content-Type: application/json`. Send a JSON object:

```json
{
  "runId": "8dcfd251-7127-4bd8-9279-18c879a8c6e7",
  "playerName": "Billy",
  "score": 1250
}
```

| Field | Required | Type and rules |
| --- | --- | --- |
| `runId` | Yes | A UUID (version 1–8, standard variant; UUID v4 recommended). Generate once per game run, use the same value for every retry, and generate a new value for the next run. UUID letter case is normalized. |
| `playerName` | Yes | String, 1–24 Unicode code points after trimming outer whitespace and NFC normalization. Chinese characters and emoji are allowed. Control characters and invalid Unicode surrogates are rejected. |
| `score` | Yes | JSON integer from `-2147483648` to `2147483647`, matching a signed 32-bit game score. Zero and negative scores are valid. Strings, floating point values, booleans and null are rejected. |

No other fields are accepted. The server records `submittedAt`; clients must not submit timestamps or a rank. These limits are a first-version contract; they do not assert a gameplay-specific maximum score.

First successful submission: HTTP **201**.

```json
{
  "score": {
    "runId": "8dcfd251-7127-4bd8-9279-18c879a8c6e7",
    "playerName": "Billy",
    "score": 1250,
    "submittedAt": "2026-11-10T08:00:00.000Z"
  },
  "duplicate": false
}
```

Retrying the same run with the same normalized name and score returns HTTP **200**, the same stored record and `duplicate: true`. It does not insert another row or change the original timestamp. Both 200 and 201 mean the result is stored successfully.

Reusing a run ID with a different name or score returns HTTP **409** (`RUN_ID_CONFLICT`); the existing result is kept. Concurrent identical requests are also deduplicated by the database.

## Read the leaderboard

`GET /v1/scores?limit=10` requires no body. `limit` defaults to 10 and must be an integer from 1 to 100, supplied once.

```json
{
  "scores": [
    {
      "rank": 1,
      "runId": "8dcfd251-7127-4bd8-9279-18c879a8c6e7",
      "playerName": "Billy",
      "score": 1250,
      "submittedAt": "2026-11-10T08:00:00.000Z"
    }
  ],
  "limit": 10
}
```

- Records represent **game runs**, not one personal best per name. The same name may appear more than once; there is no login or unique player identity.
- Order: score descending, server submission time ascending, then run ID ascending. `rank` is the displayed position, starting at 1; ties still have successive positions.
- A new database returns HTTP 200 with `scores: []`. Production is not seeded with the frontend's demo scores.
- Responses use `Cache-Control: no-store`. The webpage must request fresh data on entry or refresh; the API does not send push notifications or emit Unity events.
- Display `playerName` as text, never as HTML.

## Errors and retries

Errors have the same JSON shape:

```json
{
  "error": {
    "code": "INVALID_SCORE",
    "message": "score must be an integer between -2147483648 and 2147483647."
  }
}
```

| HTTP | Error code | Client action |
| --- | --- | --- |
| 400 | `INVALID_BODY`, `INVALID_RUN_ID`, `INVALID_PLAYER_NAME`, `INVALID_SCORE`, `INVALID_LIMIT`, `UNKNOWN_FIELD`, `INVALID_JSON` | Correct the request; do not retry the same invalid request. |
| 403 | `ORIGIN_NOT_ALLOWED` | Verify the website origin is configured. A browser may expose only a CORS/network error for a denied origin. |
| 404 | `NOT_FOUND` | Check the full URL, including `/tfbc-api`. |
| 405 | `METHOD_NOT_ALLOWED` | Use GET or POST for `/v1/scores`. |
| 409 | `RUN_ID_CONFLICT` | Do not overwrite/retry with changed contents. Inspect run ID reuse. |
| 413 | `BODY_TOO_LARGE` | Keep JSON at or below 8 KiB. Very large bodies may be rejected by Nginx before reaching the API. |
| 415 | `UNSUPPORTED_MEDIA_TYPE` | Set `Content-Type: application/json`. |
| 429 | `RATE_LIMITED` | Wait the number of seconds in `Retry-After`, then retry with the same run ID and contents. |
| 503 | `DATABASE_UNAVAILABLE` | Temporary storage failure; retry later with the same run ID and contents. |
| 500 | `INTERNAL_ERROR` | Temporary server error; allow a small number of delayed retries. |

For network timeouts, the server may already have stored the result. Retry the **same** payload with the same run ID. A practical client policy is up to three retries with increasing delays. Keep the game-over screen usable when submission fails; do not represent an unconfirmed result as saved.

Default limits per client IP per 60 seconds: 20 submissions and 300 leaderboard reads. Failed submissions also count. OPTIONS preflight is handled before these limits. Limits are held in this single API process and reset on restart; a multi-instance deployment would need a shared rate-limit store.

## Browser access

- Initial allowed origins: `https://tfbc.darketomaly.com`, `https://metapool.cloud`, `http://localhost:4321`, `http://127.0.0.1:4321`.
- Other preview domains must be added to `CORS_ORIGINS` explicitly.
- GET, HEAD, POST and OPTIONS are supported. JSON POST preflight allows `Content-Type`.
- No login, cookie, client API key or Authorization header is required. CORS controls browser access and is **not** authentication or proof that a submitted score came from a genuine game.
- An editor or server request without an Origin header can be tested directly. A successful editor request does not replace testing the WebGL build in a browser.
- Unity's static build does not automatically receive Astro environment variables. The partner needs to configure the final API URL in their own integration.

## Ready check

`GET /health` returns HTTP 200 with `{"status":"ok","database":"ok"}` only when the score table can be queried. It returns a sanitized HTTP 503 response when storage is unavailable. It does not return database credentials or connection strings.

# TFBC score backend (#12)

Independent Node.js 24 + Express + PostgreSQL service. Frontend and Unity remain in their current directories. The API is deployed at `https://metapool.cloud/tfbc-api/` through the user's BaoTa-managed Nginx. Public HTTP and browser CORS checks passed on 2026-11-11. The production board is empty; frontend #13 and game integration #9 are pending.

- [API contract for partner #9 and frontend #13](./API.md)
- [Docker / BaoTa deployment instructions](../docs/后端部署与接口交付.md)

## Full local stack

From this `backend/` directory, with Node 24 and Docker available:

```sh
npm ci
npm run setup
docker compose up -d --build --wait
```

`setup` creates an ignored `.env` with a random database password. Repeating it leaves an existing file unchanged. Compose publishes the API on **host loopback** port 3107 and keeps PostgreSQL unpublished on an internal Docker network.

The small-server configuration caps the API at 160 MiB and PostgreSQL at 128 MiB, with 0.5 CPU per service and reduced database buffers. These limits suit the current small coursework deployment; monitor before increasing traffic or running additional API replicas.

Open `http://127.0.0.1:3107/tfbc-api/health`. The score collection is `http://127.0.0.1:3107/tfbc-api/v1/scores`.

The API applies checked, transactional schema migrations before it starts listening. No seed scores are inserted. Data is kept in the `tfbc-scores_score-data` volume across API restarts and container recreation. Never remove that volume during an update.

## Node-only development

With an existing local PostgreSQL database, set `DATABASE_URL` in `.env`, set `TRUST_PROXY=0`, then run `npm run dev`. Docker's private hostname `db` only works within its Compose network. The full-stack Compose configuration does not expose its database for host Node to connect to.

## Checks

```sh
npm run check
npm test
docker compose -f compose.test.yaml up --build --abort-on-container-exit --exit-code-from tests
docker compose -f compose.test.yaml down
```

The Docker test stack has a separate `tfbc_test` database in tmpfs and does not mount the production data volume. Tests reject any `TEST_DATABASE_URL` whose database name is not `tfbc_test`. Integration tests use real PostgreSQL for concurrent deduplication, ranking, persistence across app recreation, migration re-entry and HTTP validation/CORS/rate-limit behavior.

## Operations

```sh
docker compose ps
docker compose logs --tail=100 api
docker compose up -d --build --wait
sh scripts/backup.sh
```

The backup script runs on Linux and writes a private custom-format `pg_dump` file under ignored `backups/`. Copy backups off the server as part of ongoing maintenance. Update existing databases with new numbered migration files instead of editing applied migrations or recreating tables.

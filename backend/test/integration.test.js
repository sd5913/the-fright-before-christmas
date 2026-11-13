import { after, before, beforeEach, test } from 'node:test';
import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { once } from 'node:events';
import { Pool } from 'pg';
import { createApp } from '../src/app.js';
import { readConfig } from '../src/config.js';
import { migrate } from '../src/migrate.js';
import { createScoreStore } from '../src/store.js';

// Tests truncate their dedicated database. Never accept the production URL.
const connectionString = process.env.TEST_DATABASE_URL;
if (!connectionString || new URL(connectionString).pathname !== '/tfbc_test') {
  throw new Error('Integration tests require TEST_DATABASE_URL pointing to a dedicated tfbc_test database.');
}
const pool = new Pool({ connectionString, connectionTimeoutMillis: 1000 });
const origin = 'https://tfbc.darketomaly.com';
const config = readConfig({ DATABASE_URL: connectionString, CORS_ORIGINS: origin });
const store = createScoreStore(pool);
const logger = { error() {} };
let server;
let base;

async function start(overrides = {}) {
  const app = createApp({ config: { ...config, ...overrides }, store, logger });
  server = app.listen(0, '0.0.0.0');
  await once(server, 'listening');
  base = `http://127.0.0.1:${server.address().port}/tfbc-api`;
}
async function stop() {
  server?.closeIdleConnections();
  await new Promise((resolve) => server?.close(resolve) ?? resolve());
}
async function request(path = '/v1/scores', options = {}) {
  const response = await fetch(base + path, options);
  const text = await response.text();
  return { status: response.status, headers: response.headers, body: text ? JSON.parse(text) : null };
}
function submit(entry, extra = {}) {
  return request('/v1/scores', { method: 'POST', headers: { 'Content-Type': 'application/json', Origin: origin }, body: JSON.stringify(entry), ...extra });
}
const entry = (score = 100) => ({ runId: randomUUID(), playerName: '小明🎄', score });

before(async () => { await migrate(pool); await migrate(pool); await start(); });
beforeEach(async () => { await pool.query('TRUNCATE tfbc_scores'); });
after(async () => { await stop(); await pool.end(); });

test('migration is repeatable, empty database returns an empty list, and health checks storage', async () => {
  const migrations = await pool.query('SELECT count(*)::int AS count FROM tfbc_schema_migrations');
  assert.equal(migrations.rows[0].count, 1);
  assert.deepEqual((await request()).body, { scores: [], limit: 10 });
  assert.deepEqual((await request('/health')).body, { status: 'ok', database: 'ok' });
  assert.equal((await request()).headers.get('cache-control'), 'no-store');
});

test('score persists, identical retry is idempotent and conflicting retry cannot overwrite', async () => {
  const payload = entry(-100);
  const first = await submit(payload);
  assert.equal(first.status, 201);
  assert.equal(first.body.duplicate, false);
  assert.equal(first.body.score.score, -100);
  assert.ok(!Number.isNaN(Date.parse(first.body.score.submittedAt)));
  const retry = await submit({ ...payload, runId: payload.runId.toUpperCase(), playerName: `  ${payload.playerName}  ` });
  assert.equal(retry.status, 200);
  assert.equal(retry.body.duplicate, true);
  assert.deepEqual(retry.body.score, first.body.score);
  assert.equal((await submit({ ...payload, score: 999 })).status, 409);
  assert.equal((await submit({ ...payload, playerName: 'Someone else' })).status, 409);
  const list = (await request()).body.scores;
  assert.equal(list.length, 1);
  assert.equal(list[0].score, -100);
});

test('concurrent identical submissions produce exactly one database record', async () => {
  const payload = entry(500);
  const results = await Promise.all(Array.from({ length: 8 }, () => submit(payload)));
  assert.equal(results.filter((result) => result.status === 201).length, 1);
  assert.equal(results.filter((result) => result.status === 200).length, 7);
  assert.equal((await request()).body.scores.length, 1);
});

test('ranking handles signed scores, deterministic ties, Unicode and SQL punctuation', async () => {
  const ids = ['11111111-1111-4111-8111-111111111111', '22222222-2222-4222-8222-222222222222', '33333333-3333-4333-8333-333333333333'];
  for (const id of [...ids].reverse()) await pool.query('INSERT INTO tfbc_scores (run_id, player_name, score, submitted_at) VALUES ($1, $2, $3, $4)', [id, '同分选手', 100, '2026-11-10T00:00:00Z']);
  const earlier = entry(100);
  await pool.query('INSERT INTO tfbc_scores (run_id, player_name, score, submitted_at) VALUES ($1, $2, $3, $4)', [earlier.runId, earlier.playerName, earlier.score, '2026-11-09T00:00:00Z']);
  const quoted = { ...entry(-5), playerName: "O'Reilly 🎄" };
  assert.equal((await submit(quoted)).status, 201);
  const list = (await request()).body.scores;
  assert.deepEqual(list.map((row) => row.runId), [earlier.runId, ...ids, quoted.runId]);
  assert.deepEqual(list.map((row) => row.rank), [1, 2, 3, 4, 5]);
  assert.equal(list[4].playerName, quoted.playerName);
  assert.equal((await request('/v1/scores?limit=2')).body.scores.length, 2);
  for (const query of ['limit=0', 'limit=101', 'limit=1&limit=2', 'limit=abc']) assert.equal((await request(`/v1/scores?${query}`)).status, 400);
});

test('browser CORS supports preflight and preserves headers on validation errors', async () => {
  const preflight = await request('/v1/scores', { method: 'OPTIONS', headers: { Origin: origin, 'Access-Control-Request-Method': 'POST', 'Access-Control-Request-Headers': 'Content-Type' } });
  assert.equal(preflight.status, 204);
  assert.equal(preflight.headers.get('access-control-allow-origin'), origin);
  assert.match(preflight.headers.get('access-control-allow-methods'), /POST/);
  const invalid = await submit({ ...entry(), score: '100' });
  assert.equal(invalid.status, 400);
  assert.equal(invalid.headers.get('access-control-allow-origin'), origin);
  assert.equal(invalid.body.error.code, 'INVALID_SCORE');
  const denied = await request('/v1/scores', { headers: { Origin: 'https://unlisted.example' } });
  assert.equal(denied.status, 403);
  assert.equal(denied.headers.get('access-control-allow-origin'), null);
});

test('malformed JSON, oversized input, unexpected fields and unsupported methods are reported', async () => {
  assert.equal((await submit({}, { body: '{' })).body.error.code, 'INVALID_JSON');
  assert.equal((await submit({}, { body: ' '.repeat(9000) })).status, 413);
  assert.equal((await submit({}, { headers: { 'Content-Type': 'text/plain' }, body: 'hello' })).status, 415);
  assert.equal((await submit({ ...entry(), submittedAt: '2026-01-01' })).body.error.code, 'UNKNOWN_FIELD');
  assert.equal((await request('/v1/scores', { method: 'DELETE' })).status, 405);
  assert.equal((await request('/not-a-route')).status, 404);
});

test('HTTP app restart retains stored scores', async () => {
  const payload = entry(4321);
  await submit(payload);
  await stop();
  await start();
  assert.equal((await request()).body.scores[0].runId, payload.runId);
});

test('read and write limits return structured 429 errors and Retry-After', async () => {
  await stop();
  await start({ readLimit: 2, writeLimit: 2 });
  assert.equal((await request()).status, 200);
  assert.equal((await request()).status, 200);
  const read = await request();
  assert.equal(read.status, 429);
  assert.ok(Number(read.headers.get('retry-after')) > 0);
  assert.equal((await submit(entry())).status, 201);
  assert.equal((await submit(entry())).status, 201);
  const write = await submit(entry());
  assert.equal(write.status, 429);
  assert.equal(write.headers.get('access-control-allow-origin'), origin);
  assert.equal(write.body.error.code, 'RATE_LIMITED');
  await stop();
  await start();
});

test('database connection failure returns a sanitized 503 health response', async () => {
  const offline = new Pool({ connectionString: 'postgresql://none:do-not-leak@127.0.0.1:1/offline', connectionTimeoutMillis: 100 });
  const app = createApp({ config, store: createScoreStore(offline), logger });
  const failed = app.listen(0, '0.0.0.0');
  await once(failed, 'listening');
  try {
    const response = await fetch(`http://127.0.0.1:${failed.address().port}/tfbc-api/health`, { headers: { Origin: origin } });
    assert.equal(response.status, 503);
    assert.equal(response.headers.get('access-control-allow-origin'), origin);
    const body = await response.json();
    assert.equal(body.error.code, 'DATABASE_UNAVAILABLE');
    assert.ok(!JSON.stringify(body).includes('do-not-leak'));
  } finally {
    failed.closeIdleConnections();
    await new Promise((resolve) => failed.close(resolve));
    await offline.end();
  }
});

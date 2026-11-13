import test from 'node:test';
import assert from 'node:assert/strict';
import { readConfig } from '../src/config.js';
import { parseLimit, parseSubmission } from '../src/validation.js';

const runId = '4F818AE3-BD24-4894-A234-90815AF0C87A';
const valid = { runId, playerName: '  小明🎄  ', score: 0 };

test('names are trimmed and normalized, UUID case is normalized, Unicode and negative scores work', () => {
  assert.deepEqual(parseSubmission(valid), { runId: runId.toLowerCase(), playerName: '小明🎄', score: 0 });
  assert.equal(parseSubmission({ ...valid, playerName: 'e\u0301', score: -125 }).playerName, 'é');
  for (const score of [-2147483648, -1, 2147483647]) assert.equal(parseSubmission({ ...valid, score }).score, score);
  assert.equal([...parseSubmission({ ...valid, playerName: '🎄'.repeat(24) }).playerName].length, 24);
});

test('invalid names, IDs, body types and score types are rejected', () => {
  for (const body of [null, [], {}, { ...valid, runId: 'not-a-uuid' }, { ...valid, runId: 42 },
    { ...valid, playerName: ' ' }, { ...valid, playerName: '🎄'.repeat(25) },
    { ...valid, playerName: 'a\nb' }, { ...valid, playerName: '\ud800' },
    { ...valid, score: 1.5 }, { ...valid, score: '10' }, { ...valid, score: true },
    { ...valid, score: null }, { ...valid, score: 2147483648 }, { ...valid, score: -2147483649 },
    { ...valid, submittedAt: '2026-01-01' }]) assert.throws(() => parseSubmission(body), { status: 400 });
});

test('limit is explicit, bounded and rejects repeated or structured parameters', () => {
  assert.equal(parseLimit(undefined), 10);
  assert.equal(parseLimit('1'), 1);
  assert.equal(parseLimit('100'), 100);
  for (const value of ['0', '-1', '101', '1.5', '1e1', '', '01', ['10', '20'], {}]) {
    assert.throws(() => parseLimit(value), { code: 'INVALID_LIMIT' });
  }
});

test('configuration rejects ambiguous CORS origins and unsafe proxy settings', () => {
  const environment = { DATABASE_URL: 'postgresql://user:secret@localhost/tfbc_test' };
  assert.equal(readConfig(environment).basePath, '/tfbc-api');
  assert.equal(readConfig(environment).trustProxy, false);
  assert.equal(readConfig({ ...environment, TRUST_PROXY: '1' }).trustProxy, 1);
  for (const override of [{ DATABASE_URL: '' }, { CORS_ORIGINS: '*' }, { CORS_ORIGINS: '' },
    { CORS_ORIGINS: 'https://metapool.cloud/path' }, { CORS_ORIGINS: 'https://metapool.cloud/' },
    { TRUST_PROXY: 'true' }, { API_BASE_PATH: '/tfbc-api/' }, { PORT: '0' }, { WRITE_RATE_LIMIT: '0' }]) {
    assert.throws(() => readConfig({ ...environment, ...override }));
  }
});

// Browser-level checks for #13. Fixtures are intercepted locally; they are never
// submitted to the public API. Set PLAYWRIGHT_MODULE and CHROME_PATH if needed.
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const base = process.env.HOME_URL || 'http://localhost:4321';
const apiPattern = '**/tfbc-api/v1/scores*';
const output = path.resolve('artifacts/leaderboard');
const origin = new URL(base).origin;
const fixtures = (count = 10) => Array.from({ length: count }, (_, i) => ({
  rank: i + 1,
  runId: `00000000-0000-4000-8000-${String(i + 1).padStart(12, '0')}`,
  playerName: ['Starlight', 'NightOwl', 'CosmicGuest', 'Frosty', 'Holly', 'Moonbeam', 'Comet', 'Snowcap', 'Twilight', 'Winter'][i],
  score: 59130 - i * 1000,
  submittedAt: '2026-11-11T00:00:00.000Z',
}));
const body = (scores = []) => ({ scores, limit: 10 });
const json = (route, data, status = 200) => route.fulfill({ status, contentType: 'application/json', headers: { 'access-control-allow-origin': origin, 'cache-control': 'no-store' }, body: JSON.stringify(data) });

async function main() {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ headless: true, ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : { channel: 'chrome' }) });
  const results = [];
  async function check(name, run, options = {}) {
    const context = await browser.newContext({ viewport: { width: 1536, height: 744 }, reducedMotion: 'reduce', ...options });
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', (error) => errors.push(error.message));
    try {
      const details = await run(page, context);
      assert.deepEqual(errors, [], 'Browser script errors');
      results.push({ name, passed: true, ...(details || {}) });
    } finally { await context.close(); }
  }
  const open = async (page) => {
    await page.goto(`${base}/leaderboard/`, { waitUntil: 'domcontentloaded' });
    await page.addStyleTag({ content: 'astro-dev-toolbar { display:none !important; }' });
  };
  const state = (page, value) => page.waitForSelector(`[data-live-hiscores][data-state="${value}"]`);
  try {
    await check('Homepage does not request scores or load Unity before interaction', async (page) => {
      const unexpected = [];
      page.on('request', (request) => { if (/\/tfbc-api\/|\/game\//.test(request.url())) unexpected.push(request.url()); });
      await page.goto(base, { waitUntil: 'networkidle' });
      assert.deepEqual(unexpected, []);
      assert.equal(await page.locator('a[href="/leaderboard/"]').count(), 1);
    });

    await check('Public API renders its actual response without fixture scores', async (page) => {
      const response = page.waitForResponse((r) => r.url().includes('/tfbc-api/v1/scores?limit=10'));
      await open(page);
      const actual = await (await response).json();
      await state(page, actual.scores.length ? 'ready' : 'empty');
      assert.equal(await page.locator('[data-rank]').count(), actual.scores.length);
      assert.equal(await page.getByText('Sample scores', { exact: true }).count(), 0);
      await page.screenshot({ path: path.join(output, 'public-desktop.png') });
      await page.setViewportSize({ width: 390, height: 744 });
      await page.screenshot({ path: path.join(output, 'public-mobile.png') });
      return { actualScoreCount: actual.scores.length };
    });

    await check('Loading contains no invented scores and disables duplicate refreshes', async (page, context) => {
      let release;
      let requests = 0;
      const gate = new Promise((resolve) => { release = resolve; });
      await context.route(apiPattern, async (route) => { requests++; await gate; await json(route, body(fixtures())); });
      try {
        await open(page);
        await state(page, 'loading');
        assert.equal(await page.locator('#scores-refresh').isDisabled(), true);
        assert.equal(await page.locator('[data-score]').count(), 0);
        await page.screenshot({ path: path.join(output, 'loading.png') });
      } finally { release(); }
      await state(page, 'ready');
      assert.equal(requests, 1);
      assert.equal(await page.locator('[data-rank]').count(), 10);
    });

    await check('Ten scores stay visible across desktop and mobile layouts', async (page, context) => {
      await context.route(apiPattern, (route) => json(route, body(fixtures())));
      await open(page);
      await state(page, 'ready');
      const championFont = await page.locator('[data-podium-rank="1"] [data-score-value]').evaluate((e) => parseFloat(getComputedStyle(e).fontSize));
      assert.ok(championFont >= 28, 'The champion score must retain its prominent display size');
      const layouts = [];
      for (const [width, height] of [[1920, 980], [1536, 744], [1366, 640], [1024, 576], [390, 744], [320, 640]]) {
        await page.setViewportSize({ width, height });
        const layout = await page.evaluate(() => ({
          width: innerWidth, height: innerHeight,
          overflow: document.documentElement.scrollWidth > innerWidth,
          scores: [...document.querySelectorAll('[data-score]')].map((e) => { const r = e.getBoundingClientRect(); return { top: r.top, bottom: r.bottom, left: r.left, right: r.right }; }),
        }));
        assert.equal(layout.overflow, false);
        assert.equal(layout.scores.length, 10);
        if (width >= 1000) assert.ok(layout.scores.every((r) => r.top >= 0 && r.bottom <= height), `Scores below the fold at ${width}x${height}`);
        if (width === 1536) await page.screenshot({ path: path.join(output, 'populated-desktop-fixture.png') });
        if (width === 390) await page.screenshot({ path: path.join(output, 'populated-mobile-fixture.png'), fullPage: true });
        layouts.push({ width, height, overflow: layout.overflow });
      }
      return { layouts };
    });

    await check('Empty and partial boards leave unclaimed positions without fake entries', async (page, context) => {
      let scores = [];
      await context.route(apiPattern, (route) => json(route, body(scores)));
      await open(page);
      await state(page, 'empty');
      assert.equal(await page.locator('[data-rank]').count(), 0);
      assert.equal(await page.locator('#scores-table').isHidden(), true);
      assert.match(await page.locator('#scores-state-description').innerText(), /No scores yet/);
      scores = fixtures(2);
      await page.locator('#scores-refresh').click();
      await state(page, 'ready');
      assert.equal(await page.locator('[data-rank]').count(), 2);
      assert.equal(await page.locator('[data-entry-state="vacant"]').count(), 1);
      assert.equal(await page.locator('#scores-table').isHidden(), true);
    });

    await check('Initial HTTP failure recovers through the retry button', async (page, context) => {
      let fail = true;
      await context.route(apiPattern, (route) => fail ? json(route, { error: { code: 'DATABASE_UNAVAILABLE' } }, 503) : json(route, body(fixtures(1))));
      await open(page);
      await state(page, 'error');
      assert.equal(await page.locator('[data-score]').count(), 0);
      await page.screenshot({ path: path.join(output, 'initial-error.png') });
      fail = false;
      await page.locator('#scores-retry').click();
      await state(page, 'ready');
      assert.equal(await page.locator('[data-rank]').count(), 1);
    });

    await check('Refresh failure explicitly marks the retained last successful scores', async (page, context) => {
      let fail = false;
      await context.route(apiPattern, (route) => fail ? json(route, {}, 503) : json(route, body(fixtures())));
      await open(page);
      await state(page, 'ready');
      const updated = await page.locator('#scores-updated').getAttribute('datetime');
      fail = true;
      await page.locator('#scores-refresh').click();
      await state(page, 'error');
      assert.equal(await page.locator('[data-rank]').count(), 10);
      assert.match(await page.locator('#scores-feedback').innerText(), /last loaded scores/);
      assert.equal(await page.locator('#scores-updated').getAttribute('datetime'), updated);
      assert.equal(await page.locator('#scores-refresh').isEnabled(), true);
      fail = false;
      await page.locator('#scores-refresh').focus();
      await page.keyboard.press('Enter');
      await state(page, 'ready');
    });

    await check('Network failure and malformed API responses never create fallback scores', async (page, context) => {
      let mode = 'offline';
      await context.route(apiPattern, (route) => mode === 'offline' ? route.abort('failed') : json(route, { scores: [{ rank: 1, playerName: 'Invalid', score: '999' }], limit: 10 }));
      await open(page);
      await state(page, 'error');
      mode = 'invalid';
      await page.locator('#scores-retry').click();
      await state(page, 'error');
      assert.equal(await page.locator('[data-score]').count(), 0);
      assert.equal(await page.locator('#scores-table').isHidden(), true);
    });

    await check('Slow requests time out and cannot overwrite a newer successful retry', async (page, context) => {
      await page.clock.install();
      let release;
      let calls = 0;
      const gate = new Promise((resolve) => { release = resolve; });
      await context.route(apiPattern, async (route) => {
        if (++calls === 1) { await gate; try { await json(route, body(fixtures())); } catch {} }
        else await json(route, body(fixtures(1)));
      });
      try {
        await open(page);
        await state(page, 'loading');
        await page.clock.fastForward(10001);
        await state(page, 'error');
        assert.match(await page.locator('#scores-feedback').innerText(), /longer than expected/);
        await page.locator('#scores-retry').click();
        await state(page, 'ready');
      } finally { release(); }
      await page.waitForTimeout(100);
      assert.equal(await page.locator('[data-rank]').count(), 1);
    });

    await check('Player names stay text and signed 32-bit scores fit narrow screens', async (page, context) => {
      const scores = fixtures(4);
      scores[0].playerName = '<b>Guest</b>';
      scores[1].playerName = '星'.repeat(24);
      [2147483647, 2147483646, 0, -2147483648].forEach((score, i) => { scores[i].score = score; });
      await context.route(apiPattern, (route) => json(route, body(scores)));
      await open(page);
      await state(page, 'ready');
      assert.equal(await page.locator('[data-podium-rank="1"] [data-player-name]').innerText(), '<b>Guest</b>');
      assert.equal(await page.locator('[data-player-name] b').count(), 0);
      await page.setViewportSize({ width: 320, height: 740 });
      assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
      assert.equal(await page.locator('[data-score="-2147483648"] [data-cell-score]').innerText(), '-2,147,483,648');
      const fits = await page.locator('[data-score]').evaluateAll((elements) => elements.every((e) => { const r = e.getBoundingClientRect(); const value = e.querySelector('[data-score-value],[data-cell-score]').getBoundingClientRect(); return value.left >= r.left - 1 && value.right <= r.right + 1; }));
      assert.ok(fits, 'A complete score was clipped or overlapped its cell');
      assert.equal(await page.locator('[data-live-hiscores]').evaluate((e) => e.getAnimations({ subtree: true }).length), 0);
    });

    await check('No-JavaScript users get an explanation and working navigation', async (page) => {
      await page.goto(`${base}/leaderboard/`);
      assert.match(await page.locator('#scores-state-description').innerText(), /Enable JavaScript/);
      assert.equal(await page.locator('[data-score]').count(), 0);
      await page.locator('.leaderboard-back').click();
      await page.waitForURL(`${base}/`);
    }, { javaScriptEnabled: false });

    fs.writeFileSync(path.join(output, 'verification.json'), JSON.stringify({ date: '2026-11-11', scope: 'Browser fixtures plus a read-only public API check; no production score submissions', results }, null, 2));
    console.log(JSON.stringify({ passed: results.length, results }));
  } finally { await browser.close(); }
}
main().catch((error) => { console.error(error); process.exitCode = 1; });

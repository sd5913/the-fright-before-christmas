// Run against an already running Astro server. This tests the homepage contract;
// mock iframe responses do not claim to test Unity gameplay.
// Install Playwright separately, or set PLAYWRIGHT_MODULE to an existing module.
const { chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

async function main() {
  const browser = await chromium.launch({
    headless: true,
    ...(process.env.CHROME_PATH ? { executablePath: process.env.CHROME_PATH } : { channel: 'chrome' }),
  });
  const base = process.env.HOME_URL || 'http://localhost:4321';
  const output = path.resolve('artifacts/home');
  fs.mkdirSync(output, { recursive: true });
  const results = [];
  try {
    const context = await browser.newContext({ viewport: { width: 1440, height: 1000 } });
    const page = await context.newPage();
    const errors = [];
    const gameRequests = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      if (request.url().includes('/game/')) gameRequests.push({ url: request.url(), method: request.method() });
    });
    let mode = 'normal';
    const pending = [];
    await page.route('**/game/index.html', async route => {
      if (mode === 'slow' && route.request().method() === 'HEAD') { pending.push(route); return; }
      if (mode === 'fail') { await route.fulfill({ status: 503, body: 'Unavailable' }); return; }
      await route.fulfill({ status: 200, contentType: 'text/html', body: '<!doctype html><title>QA game shell</title><p>Game-owned loading UI</p>' });
    });
    await page.goto(base, { waitUntil: 'networkidle' });
    await page.waitForTimeout(1000);
    assert.equal(gameRequests.length, 0);
    assert.equal(await page.locator('iframe').count(), 0);
    results.push('No game iframe or Unity requests before click');
    assert.deepEqual(await page.locator('[data-idle-src]').evaluateAll(images => images.map(image => image.dataset.characterState)), ['playing', 'playing']);
    assert.equal(await page.locator('[data-idle-src]').evaluateAll(images => images.every(image => image.getAttribute('src').endsWith('.gif'))), true);
    results.push('Both selected six-frame character GIFs play on the homepage');

    await page.locator('#play-button').evaluate(button => { button.click(); button.click(); });
    await page.waitForFunction(() => document.querySelector('#adventure').dataset.state === 'game');
    assert.equal(await page.locator('iframe').count(), 1);
    assert.equal(gameRequests.filter(request => request.method === 'GET').length, 1);
    assert.equal(await page.locator('#adventure').getAttribute('data-animating'), 'false');
    assert.deepEqual(await page.locator('[data-idle-src]').evaluateAll(images => images.map(image => image.dataset.characterState)), ['paused', 'paused']);
    assert.match(await page.locator('#game-status').textContent(), /loading progress/);
    results.push('Double click creates one iframe; shell load does not claim Unity ready; effects paused');

    await page.locator('#back-button').click();
    assert.equal(await page.locator('iframe').count(), 0);
    assert.equal(await page.locator('#adventure').getAttribute('data-state'), 'home');
    assert.deepEqual(await page.locator('[data-idle-src]').evaluateAll(images => images.map(image => image.dataset.characterState)), ['playing', 'playing']);
    assert.equal(await page.locator('#play-button').evaluate(button => button === document.activeElement), true);
    results.push('Back unloads iframe, restores scene and returns keyboard focus');

    assert.equal(await page.locator('#motion-toggle').count(), 0);
    assert.equal(await page.getByText('Free to play', { exact: false }).count(), 0);
    await page.evaluate(() => localStorage.setItem('tfbc-home-effects', 'off'));
    await page.reload({ waitUntil: 'networkidle' });
    assert.equal(await page.locator('#adventure').getAttribute('data-motion'), 'on');
    results.push('Promotional note and effects control removed; effects default on despite legacy saved preference');

    await page.evaluate(() => {
      Object.defineProperty(document, 'hidden', { get: () => true, configurable: true });
      document.dispatchEvent(new Event('visibilitychange'));
    });
    assert.equal(await page.locator('#adventure').getAttribute('data-animating'), 'false');
    assert.equal(await page.locator('[data-idle-src]').evaluateAll(images => images.every(image => image.getAttribute('src') === image.dataset.posterSrc)), true);
    await page.evaluate(() => { delete document.hidden; document.dispatchEvent(new Event('visibilitychange')); });
    assert.equal(await page.locator('#adventure').getAttribute('data-animating'), 'true');
    results.push('Hidden document pauses and visible document resumes effects');

    mode = 'fail';
    await page.locator('#play-button').click();
    await page.locator('#retry-button').waitFor({ state: 'visible' });
    assert.match(await page.locator('#game-status').textContent(), /could not be opened/);
    assert.equal(await page.locator('iframe').count(), 0);
    mode = 'normal';
    await page.locator('#retry-button').click();
    await page.waitForFunction(() => document.querySelector('#adventure').dataset.state === 'game');
    assert.equal(await page.locator('iframe').count(), 1);
    await page.locator('#back-button').click();
    results.push('HTTP entry failure is recoverable; retry creates one fresh iframe');

    await page.clock.install();
    mode = 'slow';
    await page.locator('#play-button').click();
    await page.clock.fastForward(500);
    await page.waitForTimeout(50);
    assert.equal(pending.length, 1);
    await page.clock.fastForward(30_100);
    await page.locator('#retry-button').waitFor({ state: 'visible' });
    assert.match(await page.locator('#game-status').textContent(), /longer than expected/);
    mode = 'normal';
    await page.locator('#retry-button').click();
    await page.waitForFunction(() => document.querySelector('#adventure').dataset.state === 'game');
    await pending[0].fulfill({ status: 200, body: '' });
    assert.equal(await page.locator('iframe').count(), 1);
    await page.locator('#back-button').click();
    results.push('30-second timeout, retry and stale response isolation verified');
    await page.close();

    const reduced = await context.newPage();
    await reduced.emulateMedia({ reducedMotion: 'reduce' });
    await reduced.goto(base, { waitUntil: 'networkidle' });
    assert.equal(await reduced.locator('#adventure').getAttribute('data-motion'), 'off');
    assert.equal(await reduced.locator('#motion-toggle').count(), 0);
    assert.equal(await reduced.locator('[data-idle-src]').evaluateAll(images => images.every(image => image.dataset.characterState === 'paused' && image.getAttribute('src') === image.dataset.posterSrc)), true);
    assert.equal(await reduced.locator('#adventure').evaluate(scene => scene.getAnimations({ subtree: true }).filter(animation => animation.playState === 'running').length), 0);
    results.push('System reduced motion still stops scene animations without a visible toggle');
    await reduced.close();

    const screenshotPage = await context.newPage();
    await screenshotPage.goto(base, { waitUntil: 'networkidle' });
    await screenshotPage.waitForTimeout(1100);
    // Hide the Astro development-only toolbar in evidence, not in application code.
    await screenshotPage.addStyleTag({ content: 'astro-dev-toolbar{display:none!important}' });
    await screenshotPage.screenshot({ path: path.join(output, 'desktop.png'), fullPage: true });
    for (const width of [320, 390, 768]) {
      await screenshotPage.setViewportSize({ width, height: 844 });
      await screenshotPage.waitForTimeout(150);
      assert.equal(await screenshotPage.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
      if (width === 390) await screenshotPage.screenshot({ path: path.join(output, 'mobile.png'), fullPage: true });
    }
    results.push('Desktop and 320/390/768px layouts have no horizontal overflow');

    const fallback = await context.newPage();
    await fallback.route('**/assets/home/logo.webp', route => route.abort());
    await fallback.goto(base, { waitUntil: 'networkidle' });
    assert.equal(await fallback.locator('.logo-fallback').isVisible(), true);
    results.push('Logo load failure displays title fallback');

    const keyboard = await context.newPage();
    await keyboard.goto(base, { waitUntil: 'networkidle' });
    await keyboard.keyboard.press('Tab');
    assert.equal(await keyboard.locator('.skip-link').evaluate(link => link === document.activeElement), true);
    await keyboard.locator('#play-button').focus();
    await keyboard.keyboard.press('Enter');
    await keyboard.waitForFunction(() => document.querySelector('#adventure').dataset.state !== 'home');
    results.push('Skip link and keyboard activation work');

    const noJsContext = await browser.newContext({ javaScriptEnabled: false });
    const noJs = await noJsContext.newPage();
    await noJs.goto(base);
    assert.equal(await noJs.locator('.no-script').isVisible(), true);
    assert.equal(await noJs.locator('iframe').count(), 0);
    results.push('Without JavaScript, explanatory fallback appears and game stays unloaded');
    await noJsContext.close();
    assert.deepEqual(errors, []);
    results.push('No homepage JavaScript errors');
    const report = { checkedAt: new Date().toISOString(), server: base, browser: 'Chrome headless', note: 'Iframe shell mocked for deterministic error/timeout tests; actual Unity smoke test reported separately.', results, gameRequests };
    fs.writeFileSync(path.join(output, 'verification.json'), JSON.stringify(report, null, 2));
    console.log(JSON.stringify({ passed: results.length, results }, null, 2));
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });

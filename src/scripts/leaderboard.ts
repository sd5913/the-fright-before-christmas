interface ScoreEntry {
  rank: number;
  runId: string;
  playerName: string;
  score: number;
  submittedAt: string;
}

class ScoreRequestError extends Error {
  constructor(message: string) { super(message); }
}

function parseScores(payload: unknown): ScoreEntry[] {
  if (!payload || typeof payload !== 'object') throw new ScoreRequestError('Scores are unavailable right now. Please try again.');
  const data = payload as { scores?: unknown; limit?: unknown };
  if (data.limit !== 10 || !Array.isArray(data.scores) || data.scores.length > 10) {
    throw new ScoreRequestError('Scores are unavailable right now. Please try again.');
  }
  const ids = new Set<string>();
  let previousScore = Infinity;
  return data.scores.map((value: unknown, index: number) => {
    if (!value || typeof value !== 'object') throw new ScoreRequestError('Scores are unavailable right now. Please try again.');
    const entry = value as Record<string, unknown>;
    if (entry.rank !== index + 1 || typeof entry.runId !== 'string' ||
      !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(entry.runId) ||
      ids.has(entry.runId.toLowerCase()) || typeof entry.playerName !== 'string' ||
      !entry.playerName.trim() || [...entry.playerName].length > 24 ||
      /[\u0000-\u001f\u007f-\u009f\ud800-\udfff]/u.test(entry.playerName) ||
      typeof entry.score !== 'number' || !Number.isInteger(entry.score) ||
      entry.score < -2147483648 || entry.score > 2147483647 || entry.score > previousScore ||
      typeof entry.submittedAt !== 'string' || !Number.isFinite(Date.parse(entry.submittedAt))) {
      throw new ScoreRequestError('Scores are unavailable right now. Please try again.');
    }
    ids.add(entry.runId.toLowerCase());
    previousScore = entry.score;
    return entry as unknown as ScoreEntry;
  });
}

function setupLeaderboard(root: HTMLElement) {
  const element = <T extends Element>(selector: string): T => {
    const found = root.querySelector<T>(selector);
    if (!found) throw new Error('The leaderboard markup is incomplete.');
    return found;
  };
  const refresh = element<HTMLButtonElement>('#scores-refresh');
  const refreshLabel = element<HTMLElement>('#scores-refresh-label');
  const retry = element<HTMLButtonElement>('#scores-retry');
  const updated = element<HTMLTimeElement>('#scores-updated');
  const feedback = element<HTMLElement>('#scores-feedback');
  const table = element<HTMLTableElement>('#scores-table');
  const rows = element<HTMLTableSectionElement>('#scores-rows');
  const template = element<HTMLTemplateElement>('#score-row-template');
  const panel = element<HTMLElement>('#scores-state-panel');
  const panelHeading = element<HTMLElement>('#scores-state-heading');
  const panelDescription = element<HTMLElement>('#scores-state-description');
  const adventure = element<HTMLAnchorElement>('#scores-adventure');
  const places = Array.from(root.querySelectorAll<HTMLElement>('[data-podium-rank]'));
  const number = new Intl.NumberFormat('en-US');
  const time = new Intl.DateTimeFormat(undefined, { hour: '2-digit', minute: '2-digit', hour12: false });
  let controller: AbortController | undefined;
  let timeout: number | undefined;
  let generation = 0;
  let hasLoaded = false;
  let lastScores: ScoreEntry[] = [];

  function message(heading: string, description: string, canRetry = false, canPlay = false) {
    panelHeading.textContent = heading;
    panelDescription.textContent = description;
    retry.hidden = !canRetry;
    adventure.hidden = !canPlay;
    panel.hidden = false;
  }

  function render(scores: ScoreEntry[]) {
    for (const [index, place] of places.entries()) {
      const entry = scores[index];
      const name = place.querySelector<HTMLElement>('[data-player-name]')!;
      const score = place.querySelector<HTMLElement>('.podium-score')!;
      const value = place.querySelector<HTMLElement>('[data-score-value]')!;
      const article = place.querySelector('article')!;
      place.dataset.entryState = entry ? 'filled' : 'vacant';
      name.textContent = entry?.playerName ?? 'Unclaimed';
      name.title = entry?.playerName ?? '';
      value.textContent = entry ? number.format(entry.score) : '—';
      score.classList.toggle('is-long', value.textContent.length > 8);
      if (entry) {
        place.dataset.rank = String(entry.rank);
        score.dataset.score = String(entry.score);
        article.setAttribute('aria-label', `Rank ${entry.rank}: ${entry.playerName}, ${number.format(entry.score)} points`);
      } else {
        delete place.dataset.rank;
        delete score.dataset.score;
        article.setAttribute('aria-label', `Place ${index + 1} is unclaimed`);
      }
    }
    const fragment = document.createDocumentFragment();
    for (const entry of scores.slice(3)) {
      const row = template.content.firstElementChild!.cloneNode(true) as HTMLTableRowElement;
      row.dataset.rank = String(entry.rank);
      row.querySelector('[data-cell-rank]')!.textContent = String(entry.rank).padStart(2, '0');
      const name = row.querySelector<HTMLElement>('.hiscores-player-name')!;
      name.textContent = entry.playerName;
      name.title = entry.playerName;
      row.querySelector('.hiscores-avatar')!.textContent = [...entry.playerName][0] ?? '';
      row.querySelector('[data-cell-score]')!.textContent = number.format(entry.score);
      row.querySelector<HTMLElement>('.hiscores-score')!.dataset.score = String(entry.score);
      fragment.append(row);
    }
    rows.replaceChildren(fragment);
    table.hidden = scores.length < 4;
    panel.hidden = scores.length >= 4;
    if (!scores.length) message('The first legend could be you.', 'No scores yet. Finish an adventure to claim a place.', false, true);
    else if (scores.length < 4) message('More legends to come.', 'Every adventure is another chance to make the list.', false, true);
  }

  async function load() {
    const token = ++generation;
    controller?.abort();
    window.clearTimeout(timeout);
    controller = new AbortController();
    const request = controller;
    let timedOut = false;
    root.dataset.state = 'loading';
    root.setAttribute('aria-busy', 'true');
    refresh.disabled = true;
    retry.disabled = true;
    refreshLabel.textContent = hasLoaded ? 'Refreshing…' : 'Loading…';
    feedback.dataset.tone = 'normal';
    feedback.textContent = hasLoaded ? 'Refreshing the leaderboard…' : 'Loading the latest scores…';
    if (!hasLoaded) message('Gathering the legends…', 'Checking the latest scores.');
    timeout = window.setTimeout(() => { timedOut = true; request.abort(); }, 10000);
    try {
      const base = new URL((root.dataset.apiBaseUrl ?? '').replace(/\/+$/, '') + '/');
      if (!['http:', 'https:'].includes(base.protocol)) throw new ScoreRequestError('Scores are unavailable right now. Please try again.');
      const url = new URL('v1/scores', base);
      url.searchParams.set('limit', '10');
      const response = await fetch(url, { signal: request.signal, cache: 'no-store', credentials: 'omit', headers: { Accept: 'application/json' } });
      if (!response.ok) throw new ScoreRequestError(response.status === 429 ? 'Please wait a moment before refreshing again.' : 'Scores are unavailable right now. Please try again.');
      const scores = parseScores(await response.json());
      if (token !== generation) return;
      render(scores);
      lastScores = scores;
      hasLoaded = true;
      const now = new Date();
      updated.dateTime = now.toISOString();
      updated.textContent = `Updated ${time.format(now)}`;
      updated.title = now.toLocaleString();
      updated.hidden = false;
      root.dataset.state = scores.length ? 'ready' : 'empty';
      feedback.textContent = scores.length ? `${scores.length} ${scores.length === 1 ? 'score' : 'scores'} loaded. Refresh to see the latest.` : 'No scores recorded yet.';
    } catch (error) {
      if (token !== generation) return;
      root.dataset.state = 'error';
      feedback.dataset.tone = 'error';
      const reason = timedOut ? 'This is taking longer than expected. Please try again.' : error instanceof ScoreRequestError ? error.message : 'Couldn’t reach the leaderboard. Please try again.';
      feedback.textContent = hasLoaded && lastScores.length ? `${reason} Showing the last loaded scores.` : reason;
      if (!hasLoaded || lastScores.length < 4) message('Couldn’t load the latest scores.', reason, true);
    } finally {
      if (token === generation) {
        window.clearTimeout(timeout);
        root.setAttribute('aria-busy', 'false');
        refresh.disabled = false;
        retry.disabled = false;
        refreshLabel.textContent = 'Refresh';
      }
    }
  }

  refresh.addEventListener('click', () => void load());
  retry.addEventListener('click', () => void load());
  window.addEventListener('pagehide', () => {
    ++generation;
    controller?.abort();
    window.clearTimeout(timeout);
  });
  window.addEventListener('pageshow', (event) => { if (event.persisted) void load(); });
  void load();
}

const leaderboard = document.querySelector<HTMLElement>('[data-live-hiscores]');
if (leaderboard) setupLeaderboard(leaderboard);

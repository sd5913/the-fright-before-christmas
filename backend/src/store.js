export class StorageError extends Error {
  constructor(cause) { super('Score storage is unavailable.', { cause }); }
}

const fields = 'run_id AS "runId", player_name AS "playerName", score, submitted_at AS "submittedAt"';
const serialize = (row) => ({ ...row, submittedAt: row.submittedAt.toISOString() });

export function createScoreStore(pool) {
  async function query(text, values) {
    try { return await pool.query(text, values); }
    catch (cause) { throw new StorageError(cause); }
  }
  return {
    async health() { await query('SELECT 1 FROM tfbc_scores LIMIT 1'); },
    async submit(entry) {
      const inserted = await query(
        `INSERT INTO tfbc_scores (run_id, player_name, score) VALUES ($1, $2, $3)
         ON CONFLICT (run_id) DO NOTHING RETURNING ${fields}`,
        [entry.runId, entry.playerName, entry.score],
      );
      if (inserted.rows.length) return { conflict: false, duplicate: false, score: serialize(inserted.rows[0]) };
      // ON CONFLICT waits for any competing insert. The following statement
      // reads its committed result, including when different API processes race.
      const existing = await query(`SELECT ${fields} FROM tfbc_scores WHERE run_id = $1`, [entry.runId]);
      const row = existing.rows[0];
      if (!row) throw new StorageError(new Error('An existing run disappeared.'));
      if (row.playerName !== entry.playerName || row.score !== entry.score) return { conflict: true };
      return { conflict: false, duplicate: true, score: serialize(row) };
    },
    async list(limit) {
      const { rows } = await query(`SELECT ${fields} FROM tfbc_scores ORDER BY score DESC, submitted_at ASC, run_id ASC LIMIT $1`, [limit]);
      return rows.map((row, index) => ({ rank: index + 1, ...serialize(row) }));
    },
  };
}

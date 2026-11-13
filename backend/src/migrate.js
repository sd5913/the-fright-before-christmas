import { createHash } from 'node:crypto';
import { readdir, readFile } from 'node:fs/promises';

export async function migrate(pool) {
  const client = await pool.connect();
  try {
    await client.query('BEGIN');
    await client.query('SELECT pg_advisory_xact_lock(5913, 12)');
    await client.query(`CREATE TABLE IF NOT EXISTS tfbc_schema_migrations (
      version TEXT PRIMARY KEY,
      checksum CHAR(64) NOT NULL,
      applied_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
    )`);
    const { rows } = await client.query('SELECT version, checksum FROM tfbc_schema_migrations');
    const applied = new Map(rows.map((row) => [row.version, row.checksum]));
    const directory = new URL('../migrations/', import.meta.url);
    const files = (await readdir(directory)).filter((name) => /^\d+_[a-z0-9_-]+\.sql$/.test(name)).sort();
    for (const file of files) {
      const sql = await readFile(new URL(file, directory), 'utf8');
      // Git CRLF conversion must not change the migration checksum on Linux.
      const checksum = createHash('sha256').update(sql.replace(/\r\n/g, '\n')).digest('hex');
      if (applied.has(file)) {
        if (applied.get(file) !== checksum) throw new Error('An applied migration was changed. Add a new migration instead.');
        continue;
      }
      await client.query(sql);
      await client.query('INSERT INTO tfbc_schema_migrations (version, checksum) VALUES ($1, $2)', [file, checksum]);
    }
    await client.query('COMMIT');
  } catch (error) {
    await client.query('ROLLBACK');
    throw error;
  } finally {
    client.release();
  }
}

import { Pool } from 'pg';
import { readConfig } from '../src/config.js';
import { migrate } from '../src/migrate.js';

let pool;
try {
  pool = new Pool({ connectionString: readConfig().databaseUrl, connectionTimeoutMillis: 5000, statement_timeout: 5000 });
  await migrate(pool);
  console.log('TFBC database migrations applied.');
} catch (error) {
  console.error({ event: 'migration_failed', code: error.code ?? 'UNKNOWN' });
  process.exitCode = 1;
} finally {
  await pool?.end();
}

import { Pool } from 'pg';
import { createApp } from './app.js';
import { ConfigError, readConfig } from './config.js';
import { migrate } from './migrate.js';
import { createScoreStore } from './store.js';

let pool;
try {
  const config = readConfig();
  pool = new Pool({ connectionString: config.databaseUrl, max: 10, connectionTimeoutMillis: 5000, idleTimeoutMillis: 30000, statement_timeout: 5000 });
  pool.on('error', (error) => console.error({ event: 'database_pool_error', code: error.code ?? 'UNKNOWN' }));
  await migrate(pool);
  const app = createApp({ config, store: createScoreStore(pool) });
  const server = app.listen(config.port, '0.0.0.0');
  server.on('error', (error) => {
    console.error({ event: 'http_server_error', code: error.code ?? 'UNKNOWN' });
    void pool.end().finally(() => process.exit(1));
  });
  server.on('listening', () => console.log({ event: 'api_ready', port: config.port, basePath: config.basePath }));
  let stopping = false;
  async function stop() {
    if (stopping) return;
    stopping = true;
    const deadline = setTimeout(() => process.exit(1), 10000).unref();
    server.closeIdleConnections();
    await new Promise((resolve) => server.close(resolve));
    await pool.end();
    clearTimeout(deadline);
  }
  process.on('SIGTERM', () => void stop());
  process.on('SIGINT', () => void stop());
} catch (error) {
  console.error({ event: 'startup_failed', code: error.code ?? 'UNKNOWN', message: error instanceof ConfigError ? error.message : 'Database initialization failed.' });
  await pool?.end();
  process.exitCode = 1;
}

export class ConfigError extends Error {}

function integer(value, fallback, min, max, name) {
  const text = String(value ?? fallback);
  const number = Number(text);
  if (!/^\d+$/.test(text) || !Number.isSafeInteger(number) || number < min || number > max) {
    throw new ConfigError(`${name} must be an integer between ${min} and ${max}.`);
  }
  return number;
}

export function readConfig(env = process.env) {
  if (!/^postgres(?:ql)?:\/\//.test(env.DATABASE_URL ?? '')) {
    throw new ConfigError('DATABASE_URL must be a PostgreSQL connection URL.');
  }
  const basePath = env.API_BASE_PATH ?? '/tfbc-api';
  if (!/^\/[a-zA-Z0-9_-]+(?:\/[a-zA-Z0-9_-]+)*$/.test(basePath)) {
    throw new ConfigError('API_BASE_PATH must be an absolute path without a trailing slash.');
  }
  const origins = (env.CORS_ORIGINS ?? 'http://localhost:4321,http://127.0.0.1:4321')
    .split(',').map((origin) => origin.trim()).filter(Boolean);
  for (const origin of origins) {
    let url;
    try { url = new URL(origin); } catch { throw new ConfigError('CORS_ORIGINS must contain full HTTP(S) origins.'); }
    if (!['http:', 'https:'].includes(url.protocol) || url.origin !== origin) {
      throw new ConfigError('CORS_ORIGINS entries must not contain paths, wildcards, or trailing slashes.');
    }
  }
  if (!origins.length) throw new ConfigError('CORS_ORIGINS must contain at least one allowed origin.');
  return {
    databaseUrl: env.DATABASE_URL,
    port: integer(env.PORT, 3000, 1, 65535, 'PORT'),
    basePath,
    origins,
    // The deployment publishes the API on host loopback behind one Nginx proxy.
    trustProxy: integer(env.TRUST_PROXY, 0, 0, 1, 'TRUST_PROXY') || false,
    readLimit: integer(env.READ_RATE_LIMIT, 300, 1, 10000, 'READ_RATE_LIMIT'),
    writeLimit: integer(env.WRITE_RATE_LIMIT, 20, 1, 1000, 'WRITE_RATE_LIMIT'),
    rateWindowMs: integer(env.RATE_WINDOW_MS, 60000, 1000, 3600000, 'RATE_WINDOW_MS'),
  };
}

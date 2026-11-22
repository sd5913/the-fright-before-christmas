import express from 'express';
import cors from 'cors';
import { rateLimit } from 'express-rate-limit';
import { ApiError, parseLimit, parseSubmission } from './validation.js';
import { StorageError } from './store.js';

export function createApp({ config, store, logger = console }) {
  const app = express();
  app.disable('x-powered-by');
  app.disable('etag');
  app.set('trust proxy', config.trustProxy);
  app.use((req, res, next) => {
    res.set('Cache-Control', 'no-store');
    res.set('X-Content-Type-Options', 'nosniff');
    next();
  });
  app.use(cors({
    origin(origin, callback) {
      if (!origin || config.origins.includes(origin)) callback(null, true);
      else callback(new ApiError(403, 'ORIGIN_NOT_ALLOWED', 'This website origin is not allowed.'));
    },
    methods: ['GET', 'HEAD', 'POST', 'OPTIONS'],
    allowedHeaders: ['Content-Type'],
    exposedHeaders: ['Retry-After'],
    maxAge: 600,
  }));
  const limiter = (limit) => rateLimit({
    windowMs: config.rateWindowMs,
    limit,
    standardHeaders: 'draft-8',
    legacyHeaders: false,
    handler(req, res) {
      res.status(429).json({ error: { code: 'RATE_LIMITED', message: 'Too many requests. Retry after the Retry-After interval.' } });
    },
  });
  const api = express.Router();
  api.get('/health', async (req, res) => {
    await store.health();
    res.json({ status: 'ok', database: 'ok' });
  });
  api.get('/v1/scores', limiter(config.readLimit), async (req, res) => {
    const limit = parseLimit(req.query.limit);
    res.json({ scores: await store.list(limit), limit });
  });
  api.post('/v1/scores', limiter(config.writeLimit), (req, res, next) => {
    if (!req.is('application/json')) return next(new ApiError(415, 'UNSUPPORTED_MEDIA_TYPE', 'Use Content-Type: application/json.'));
    next();
  }, express.json({ limit: '8kb', strict: true }), async (req, res) => {
    const result = await store.submit(parseSubmission(req.body));
    if (result.conflict) throw new ApiError(409, 'RUN_ID_CONFLICT', 'This runId already has a different score or playerName.');
    res.status(result.duplicate ? 200 : 201).json({ score: result.score, duplicate: result.duplicate });
  });
  api.all('/v1/scores', (req, res) => {
    res.set('Allow', 'GET, HEAD, POST, OPTIONS');
    res.status(405).json({ error: { code: 'METHOD_NOT_ALLOWED', message: 'Use GET to read scores or POST to submit a score.' } });
  });
  app.use(config.basePath, api);
  app.use((req, res) => res.status(404).json({ error: { code: 'NOT_FOUND', message: 'API route not found.' } }));
  app.use((error, req, res, next) => {
    if (res.headersSent) return next(error);
    if (error.type === 'entity.parse.failed') error = new ApiError(400, 'INVALID_JSON', 'The request body is not valid JSON.');
    if (error.type === 'entity.too.large') error = new ApiError(413, 'BODY_TOO_LARGE', 'The JSON body must not exceed 8 KiB.');
    if (error.type === 'charset.unsupported') error = new ApiError(415, 'UNSUPPORTED_CHARSET', 'Use a UTF-8 JSON body.');
    if (error.type === 'encoding.unsupported') error = new ApiError(415, 'UNSUPPORTED_ENCODING', 'The request Content-Encoding is not supported.');
    if (error instanceof StorageError) {
      logger.error({ event: 'storage_error', code: error.cause?.code ?? 'UNKNOWN' });
      error = new ApiError(503, 'DATABASE_UNAVAILABLE', 'Score storage is temporarily unavailable. Please retry later.');
    }
    if (!(error instanceof ApiError)) {
      logger.error({ event: 'request_error', code: error.code ?? 'UNKNOWN' });
      error = new ApiError(500, 'INTERNAL_ERROR', 'An unexpected server error occurred.');
    }
    res.status(error.status).json({ error: { code: error.code, message: error.message } });
  });
  return app;
}

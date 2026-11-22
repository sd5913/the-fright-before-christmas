export class ApiError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

export function parseSubmission(body) {
  if (!body || typeof body !== 'object' || Array.isArray(body)) {
    throw new ApiError(400, 'INVALID_BODY', 'A JSON object is required.');
  }
  if (Object.keys(body).some((key) => !['runId', 'playerName', 'score'].includes(key))) {
    throw new ApiError(400, 'UNKNOWN_FIELD', 'Only runId, playerName, and score are accepted.');
  }
  if (typeof body.runId !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(body.runId)) {
    throw new ApiError(400, 'INVALID_RUN_ID', 'runId must be a UUID generated once per game run.');
  }
  if (typeof body.playerName !== 'string') {
    throw new ApiError(400, 'INVALID_PLAYER_NAME', 'playerName must contain 1 to 24 Unicode characters.');
  }
  const playerName = body.playerName.trim().normalize('NFC');
  const length = [...playerName].length;
  if (length < 1 || length > 24 || /[\u0000-\u001f\u007f-\u009f\ud800-\udfff]/u.test(playerName)) {
    throw new ApiError(400, 'INVALID_PLAYER_NAME', 'playerName must contain 1 to 24 Unicode characters without control characters.');
  }
  if (!Number.isInteger(body.score) || body.score < -2147483648 || body.score > 2147483647) {
    throw new ApiError(400, 'INVALID_SCORE', 'score must be an integer between -2147483648 and 2147483647.');
  }
  return { runId: body.runId.toLowerCase(), playerName, score: body.score };
}

export function parseLimit(value) {
  if (value === undefined) return 10;
  if (typeof value !== 'string' || !/^[1-9][0-9]{0,2}$/.test(value) || Number(value) > 100) {
    throw new ApiError(400, 'INVALID_LIMIT', 'limit must be an integer between 1 and 100.');
  }
  return Number(value);
}

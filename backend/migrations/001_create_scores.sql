CREATE TABLE tfbc_scores (
  run_id UUID PRIMARY KEY,
  player_name VARCHAR(24) NOT NULL CHECK (char_length(player_name) BETWEEN 1 AND 24),
  score INTEGER NOT NULL,
  submitted_at TIMESTAMPTZ NOT NULL DEFAULT clock_timestamp()
);

CREATE INDEX tfbc_scores_ranking_idx
  ON tfbc_scores (score DESC, submitted_at ASC, run_id ASC);

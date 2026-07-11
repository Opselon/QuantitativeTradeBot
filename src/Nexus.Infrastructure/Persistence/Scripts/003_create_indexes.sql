-- 003_create_indexes.sql

-- Index for high-throughput ticks querying/streaming
CREATE INDEX IF NOT EXISTS idx_market_ticks_symbol_time_desc
    ON market_ticks (symbol, time_utc DESC);

-- Index for bars querying
CREATE INDEX IF NOT EXISTS idx_market_bars_symbol_timeframe_time_desc
    ON market_bars (symbol, timeframe, time_utc DESC);

-- Index for orders query by account, status, and creation date
CREATE INDEX IF NOT EXISTS idx_orders_account_id_status_created_desc
    ON orders (account_id, status, created_at_utc DESC);

-- Index for positions query by account, status, and open date
CREATE INDEX IF NOT EXISTS idx_positions_account_id_status_opened_desc
    ON positions (account_id, status, opened_at_utc DESC);

-- Index for trades query by position
CREATE INDEX IF NOT EXISTS idx_trades_position_id_executed_desc
    ON trades (position_id, executed_at_utc DESC);

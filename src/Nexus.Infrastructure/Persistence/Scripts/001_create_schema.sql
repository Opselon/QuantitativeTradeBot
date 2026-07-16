-- 001_create_schema.sql

CREATE TABLE IF NOT EXISTS accounts (
    id UUID PRIMARY KEY,
    broker_account_id VARCHAR(64) NOT NULL UNIQUE,
    broker_name VARCHAR(64) NOT NULL,
    currency VARCHAR(10) NOT NULL,
    balance NUMERIC(20, 4) NOT NULL,
    equity NUMERIC(20, 4) NOT NULL,
    margin NUMERIC(20, 4) NOT NULL,
    free_margin NUMERIC(20, 4) NOT NULL,
    leverage INT NOT NULL,
    is_live BOOLEAN NOT NULL DEFAULT FALSE,
    updated_at_utc TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL,
    symbol VARCHAR(16) NOT NULL,
    direction VARCHAR(10) NOT NULL,
    type VARCHAR(16) NOT NULL,
    volume NUMERIC(18, 4) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    stop_loss DOUBLE PRECISION,
    take_profit DOUBLE PRECISION,
    status VARCHAR(20) NOT NULL,
    status_reason TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    account_id UUID REFERENCES accounts(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS positions (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL UNIQUE,
    symbol VARCHAR(16) NOT NULL,
    direction VARCHAR(10) NOT NULL,
    volume NUMERIC(18, 4) NOT NULL,
    entry_price DOUBLE PRECISION NOT NULL,
    current_price DOUBLE PRECISION NOT NULL,
    stop_loss DOUBLE PRECISION,
    take_profit DOUBLE PRECISION,
    unrealized_pnl NUMERIC(20, 4) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    created_at_utc TIMESTAMPTZ NOT NULL,
    opened_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    account_id UUID REFERENCES accounts(id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS trades (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL,
    position_id UUID REFERENCES positions(id) ON DELETE SET NULL,
    symbol VARCHAR(16) NOT NULL,
    direction VARCHAR(10) NOT NULL,
    volume NUMERIC(18, 4) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    commission NUMERIC(18, 4) NOT NULL,
    swap NUMERIC(18, 4) NOT NULL,
    realized_pnl NUMERIC(20, 4) NOT NULL,
    executed_at_utc TIMESTAMPTZ NOT NULL
);

-- Parent tables with declarative partitioning
CREATE TABLE IF NOT EXISTS market_ticks (
    symbol VARCHAR(16) NOT NULL,
    time_utc TIMESTAMPTZ NOT NULL,
    bid DOUBLE PRECISION NOT NULL,
    ask DOUBLE PRECISION NOT NULL,
    spread DOUBLE PRECISION NOT NULL,
    source VARCHAR(32),
    PRIMARY KEY (symbol, time_utc)
) PARTITION BY RANGE (time_utc);

CREATE TABLE IF NOT EXISTS market_bars (
    symbol VARCHAR(16) NOT NULL,
    timeframe VARCHAR(10) NOT NULL,
    time_utc TIMESTAMPTZ NOT NULL,
    open_price DOUBLE PRECISION NOT NULL,
    high_price DOUBLE PRECISION NOT NULL,
    low_price DOUBLE PRECISION NOT NULL,
    close_price DOUBLE PRECISION NOT NULL,
    volume DOUBLE PRECISION NOT NULL,
    PRIMARY KEY (symbol, timeframe, time_utc)
) PARTITION BY RANGE (time_utc);

CREATE TABLE IF NOT EXISTS execution_errors (
    id UUID PRIMARY KEY,
    order_id VARCHAR(64),
    error_code VARCHAR(64) NOT NULL,
    error_message TEXT NOT NULL,
    timestamp_utc TIMESTAMPTZ NOT NULL
);

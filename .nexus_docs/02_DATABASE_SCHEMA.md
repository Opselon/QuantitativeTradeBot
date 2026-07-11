# Nexus Trading Engine (NTE) - Database Schema Document

## 1. Overview
The NTE database schema is optimized for two completely distinct access patterns:
1. **High-Integrity Transactional Data**: Open Positions, Orders, Historical Trades, and Account state. This needs strict transaction integrity, foreign keys, and ACID compliance, handled via Entity Framework Core.
2. **High-Throughput Time-Series Market Data**: Streaming Tick and Bar data. This needs to handle millions of records per day, write with absolute minimal latency, and allow fast historical lookups, implemented using PostgreSQL declarative partitioning and bulk insertion via Npgsql binary COPY.

---

## 2. Table Schemas

### A. Time-Series Partitioned Tables

#### 1. `market_ticks`
Contains raw tick data (L1 Bid/Ask updates). Partitioned by range on `time_utc` (monthly partitions).

```sql
CREATE TABLE IF NOT EXISTS market_ticks (
    symbol VARCHAR(16) NOT NULL,
    time_utc TIMESTAMPTZ NOT NULL,
    bid DOUBLE PRECISION NOT NULL,
    ask DOUBLE PRECISION NOT NULL,
    spread DOUBLE PRECISION NOT NULL,
    source VARCHAR(32),
    PRIMARY KEY (symbol, time_utc)
) PARTITION BY RANGE (time_utc);
```

#### 2. `market_bars`
Contains OHLCV candlesticks for different timeframes. Partitioned by range on `time_utc` (monthly partitions).

```sql
CREATE TABLE IF NOT EXISTS market_bars (
    symbol VARCHAR(16) NOT NULL,
    timeframe VARCHAR(10) NOT NULL, -- e.g., 'M1', 'H1', 'D1'
    time_utc TIMESTAMPTZ NOT NULL,
    open_price DOUBLE PRECISION NOT NULL,
    high_price DOUBLE PRECISION NOT NULL,
    low_price DOUBLE PRECISION NOT NULL,
    close_price DOUBLE PRECISION NOT NULL,
    volume DOUBLE PRECISION NOT NULL,
    PRIMARY KEY (symbol, timeframe, time_utc)
) PARTITION BY RANGE (time_utc);
```

---

### B. Transactional Tables

#### 1. `accounts`
Stores the high-level state of user/system trading accounts.

```sql
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
```

#### 2. `orders`
Stores historical and pending execution orders.

```sql
CREATE TABLE IF NOT EXISTS orders (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL,
    symbol VARCHAR(16) NOT NULL,
    direction VARCHAR(10) NOT NULL, -- 'Buy', 'Sell'
    type VARCHAR(16) NOT NULL, -- 'Market', 'Limit', 'Stop'
    volume NUMERIC(18, 4) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    stop_loss DOUBLE PRECISION,
    take_profit DOUBLE PRECISION,
    status VARCHAR(20) NOT NULL, -- 'Pending', 'Filled', 'Rejected', 'Cancelled'
    status_reason TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    account_id UUID REFERENCES accounts(id) ON DELETE SET NULL
);
```

#### 3. `positions`
Stores active live trading positions tracked on the platform.

```sql
CREATE TABLE IF NOT EXISTS positions (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL UNIQUE, -- Matches parent filled Order or MT5 ticket
    symbol VARCHAR(16) NOT NULL,
    direction VARCHAR(10) NOT NULL, -- 'Buy', 'Sell'
    volume NUMERIC(18, 4) NOT NULL,
    entry_price DOUBLE PRECISION NOT NULL,
    current_price DOUBLE PRECISION NOT NULL,
    stop_loss DOUBLE PRECISION,
    take_profit DOUBLE PRECISION,
    unrealized_pnl NUMERIC(20, 4) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN', -- 'OPEN', 'CLOSED'
    created_at_utc TIMESTAMPTZ NOT NULL,
    opened_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL,
    account_id UUID REFERENCES accounts(id) ON DELETE SET NULL
);
```

#### 4. `trades`
Stores historical execution trades (fills).

```sql
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
```

---

## 3. Partitioning Strategy

Declarative partitioning is applied to `market_ticks` and `market_bars` tables using the `time_utc` column. Because high-frequency data grows exponentially, storing all ticks in a single table degrades index search speed (B-Trees become massive) and locks tables during maintenance.

### Monthly Partitions
Partitions are created dynamically on a **monthly** boundary. For example:
* `market_ticks_2026_01` handles ticks where `time_utc` is `[2026-01-01 00:00:00, 2026-02-01 00:00:00)`.
* `market_bars_2026_01` handles bars where `time_utc` is `[2026-01-01 00:00:00, 2026-02-01 00:00:00)`.

### Idempotent Partition Management
The engine registers a reusable SQL function `create_monthly_partition_if_not_exists` which is safe, idempotent, and executes pre-creation of partitions dynamically for current, past, and upcoming months.

```sql
CREATE OR REPLACE FUNCTION create_monthly_partition_if_not_exists(parent_table TEXT, target_date TIMESTAMPTZ)
RETURNS VOID AS $$
DECLARE
    start_date TIMESTAMPTZ;
    end_date TIMESTAMPTZ;
    partition_name TEXT;
    year_str TEXT;
    month_str TEXT;
BEGIN
    start_date := date_trunc('month', target_date);
    end_date := start_date + INTERVAL '1 month';
    year_str := to_char(start_date, 'YYYY');
    month_str := to_char(start_date, 'MM');
    partition_name := parent_table || '_' || year_str || '_' || month_str;

    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
        partition_name, parent_table, start_date, end_date
    );
END;
$$ LANGUAGE plpgsql;
```

During startup or via automated chron jobs, NTE pre-creates partitions for `current_month` and `next_month` using this function.

---

## 4. Indexing Strategy

To keep query performance under O(log N) even with billions of rows, the following indexes are strictly applied:

1. **`idx_market_ticks_symbol_time_desc`** on `market_ticks (symbol, time_utc DESC)`
   * Optimized for: streaming/querying the latest streaming ticks for backtesting and chart rendering.
2. **`idx_market_bars_symbol_timeframe_time_desc`** on `market_bars (symbol, timeframe, time_utc DESC)`
   * Optimized for: fetching historical bars for strategic indicator calculation (e.g., EMA crossovers).
3. **`idx_orders_account_id_status_created_desc`** on `orders (account_id, status, created_at_utc DESC)`
   * Optimized for: rendering active and pending orders on user dashboards.
4. **`idx_positions_account_id_status_opened_desc`** on `positions (account_id, status, opened_at_utc DESC)`
   * Optimized for: quick retrieval of open/live position states for real-time risk evaluation and margin calls.
5. **`idx_trades_position_id_executed_desc`** on `trades (position_id, executed_at_utc DESC)`
   * Optimized for: calculating historical realized performance, trade audits, and performance tracking.

In PostgreSQL, indexes defined on parent partitioned tables are automatically inherited by any newly created monthly child tables.

---

## 5. Retention and Archival Policy
Because high-frequency ticks can exceed hundreds of gigabytes per month, an automated retention policy is supported:
1. **Drop Old Partitions**: Since each month is a separate physical table, we can instantly drop older data using `DROP TABLE market_ticks_2024_01;` instead of costly and lock-heavy `DELETE` statements.
2. **Cold Storage/Pg_dump**: Partitions older than 3 months are exported to compressed CSVs or Parquet files for cold-storage analytical backtests, keeping the live Postgres database extremely lean.

---

## 6. High-Speed Bulk Import via Binary COPY
To bypass EF Core's change tracking, reflection, and SQL statement construction overhead, the NTE `MarketDataRepository` leverages **PostgreSQL Binary COPY** via `NpgsqlBinaryImporter`.

During bulk operations, a dedicated stream is established:
```csharp
using var writer = await conn.BeginBinaryImportAsync(
    "COPY market_ticks (symbol, time_utc, bid, ask, spread, source) FROM STDIN (FORMAT BINARY)",
    cancellationToken
);
```
This writes raw binary arrays directly to PostgreSQL socket buffers, easily achieving **100,000+ ticks/sec** throughput, maintaining a stable CPU/Memory profile on high-frequency live markets.

---

## 7. UTC Timestamp Policy
To prevent datetime misalignment, DST (Daylight Saving Time) artifacts, and timezone mismatch across MT5 and global servers:
1. All database timestamp columns use `TIMESTAMPTZ` (`timestamp with time zone`).
2. The domain layers exclusively deal with UTC `DateTime` values (`DateTimeKind.Utc`).
3. Inside `NexusDbContext`, a custom interceptor is implemented inside `SaveChangesAsync()` that intercepts and validates every tracked entity. If any property is saved with `DateTimeKind.Local`, the system throws an exception to block corrupted inputs. `DateTimeKind.Unspecified` timestamps are automatically normalized to `DateTimeKind.Utc`.

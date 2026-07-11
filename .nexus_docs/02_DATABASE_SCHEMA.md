# Nexus Trading Engine (NTE) - Database Schema Document

## 1. Overview
The NTE database schema is optimized for two completely distinct access patterns:
1. **High-Integrity Transactional Data**: Open Positions, Orders, Historical Trades, and Account state. This needs strict transaction integrity, foreign keys, and ACID compliance, handled via Entity Framework Core.
2. **High-Throughput Time-Series Market Data**: Streaming Tick and Bar data. This needs to handle millions of records per day, write with absolute minimal latency, and allow fast historical lookups, implemented using PostgreSQL Partitioning and bulk insertion via Dapper/binary copy.

---

## 2. Table Schemas

### A. Time-Series Partitioned Tables

#### 1. `market_ticks`
Contains raw tick data (L1 Bid/Ask updates). Partitioned by range on `time` (e.g., monthly partitions).

```sql
CREATE TABLE market_ticks (
    symbol VARCHAR(12) NOT NULL,
    time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    bid DOUBLE PRECISION NOT NULL,
    ask DOUBLE PRECISION NOT NULL,
    PRIMARY KEY (symbol, time)
) PARTITION BY RANGE (time);

-- Example Monthly Partition Creation (handled dynamically or via migration scripts)
CREATE TABLE market_ticks_y2025m01 PARTITION OF market_ticks
    FOR VALUES FROM ('2025-01-01 00:00:00') TO ('2025-02-01 00:00:00');
```

#### 2. `market_bars`
Contains OHLCV candlesticks for different timeframes. Partitioned by range on `time` (e.g., yearly partitions).

```sql
CREATE TABLE market_bars (
    symbol VARCHAR(12) NOT NULL,
    timeframe VARCHAR(8) NOT NULL, -- e.g., 'M1', 'H1', 'D1'
    time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    open DOUBLE PRECISION NOT NULL,
    high DOUBLE PRECISION NOT NULL,
    low DOUBLE PRECISION NOT NULL,
    close DOUBLE PRECISION NOT NULL,
    volume DOUBLE PRECISION NOT NULL,
    PRIMARY KEY (symbol, timeframe, time)
) PARTITION BY RANGE (time);
```

---

### B. Transactional Tables

#### 1. `accounts`
Stores the high-level state of user/system trading accounts.

```sql
CREATE TABLE accounts (
    id UUID PRIMARY KEY,
    broker_account_id VARCHAR(64) NOT NULL UNIQUE,
    broker_name VARCHAR(64) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    balance DECIMAL(18, 4) NOT NULL,
    equity DECIMAL(18, 4) NOT NULL,
    margin DECIMAL(18, 4) NOT NULL,
    free_margin DECIMAL(18, 4) NOT NULL,
    leverage INT NOT NULL,
    is_live BOOLEAN NOT NULL DEFAULT FALSE,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
```

#### 2. `orders`
Stores historical and pending execution orders.

```sql
CREATE TABLE orders (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL UNIQUE, -- MT5 assigned ticket ID
    symbol VARCHAR(12) NOT NULL,
    direction VARCHAR(4) NOT NULL, -- 'BUY', 'SELL'
    order_type VARCHAR(16) NOT NULL, -- 'MARKET', 'LIMIT', 'STOP'
    volume DECIMAL(10, 4) NOT NULL,
    price DOUBLE PRECISION NOT NULL,
    stop_loss DOUBLE PRECISION,
    take_profit DOUBLE PRECISION,
    status VARCHAR(20) NOT NULL, -- 'PENDING', 'FILLED', 'REJECTED', 'CANCELLED'
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
```

#### 3. `positions`
Stores active live trading positions tracked on the platform.

```sql
CREATE TABLE positions (
    id UUID PRIMARY KEY,
    ticket_id VARCHAR(64) NOT NULL UNIQUE, -- Matches parent filled Order or MT5 ticket
    symbol VARCHAR(12) NOT NULL,
    direction VARCHAR(4) NOT NULL, -- 'BUY', 'SELL'
    volume DECIMAL(10, 4) NOT NULL,
    entry_price DOUBLE PRECISION NOT NULL,
    current_price DOUBLE PRECISION NOT NULL,
    stop_loss DOUBLE PRECISION,
    take_profit DOUBLE PRECISION,
    unrealized_pnl DECIMAL(18, 4) NOT NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL
);
```

---

## 3. High-Speed Bulk Import via Binary COPY

To bypass standard EF Core reflection and SQL parsing overhead when handling massive inflows of streaming ticks, NTE uses ADO.NET Binary COPY:

```csharp
using var writer = connection.BeginBinaryImport(
    "COPY market_ticks (symbol, time, bid, ask) FROM STDIN (FORMAT BINARY)"
);
foreach (var tick in tickBatch) {
    writer.StartRow();
    writer.Write(tick.Symbol, NpgsqlDbType.Varchar);
    writer.Write(tick.Time, NpgsqlDbType.Timestamp);
    writer.Write(tick.Bid, NpgsqlDbType.Double);
    writer.Write(tick.Ask, NpgsqlDbType.Double);
}
writer.Complete();
```
This reduces resource usage and maintains stable performance under heavy load.

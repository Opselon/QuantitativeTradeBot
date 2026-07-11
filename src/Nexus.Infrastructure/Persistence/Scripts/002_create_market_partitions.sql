-- 002_create_market_partitions.sql

CREATE OR REPLACE FUNCTION create_monthly_partition_if_not_exists(parent_table TEXT, target_date TIMESTAMPTZ)
RETURNS VOID AS $$
DECLARE
    start_date TIMESTAMPTZ;
    end_date TIMESTAMPTZ;
    partition_name TEXT;
    year_str TEXT;
    month_str TEXT;
BEGIN
    -- Calculate start and end of the month
    start_date := date_trunc('month', target_date);
    end_date := start_date + INTERVAL '1 month';

    -- Format year and month strings
    year_str := to_char(start_date, 'YYYY');
    month_str := to_char(start_date, 'MM');
    partition_name := parent_table || '_' || year_str || '_' || month_str;

    -- Execute partition creation dynamically
    EXECUTE format(
        'CREATE TABLE IF NOT EXISTS %I PARTITION OF %I FOR VALUES FROM (%L) TO (%L)',
        partition_name, parent_table, start_date, end_date
    );
END;
$$ LANGUAGE plpgsql;

-- Pre-create partitions for:
-- 1. Previous month
-- 2. Current month
-- 3. Next month
-- 4. Month after next
DO $$
DECLARE
    curr_date TIMESTAMPTZ;
BEGIN
    FOR i IN -1..2 LOOP
        curr_date := NOW() + (i || ' month')::INTERVAL;
        PERFORM create_monthly_partition_if_not_exists('market_ticks', curr_date);
        PERFORM create_monthly_partition_if_not_exists('market_bars', curr_date);
    END LOOP;
END $$;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Nexus.Application.Ports;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Infrastructure.Persistence.Repositories
{
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly NexusDbContext _context;
        private static readonly List<Tick> FallbackTicks = new();
        private static readonly List<Bar> FallbackBars = new();
        private static readonly object Lock = new();

        public MarketDataRepository(NexusDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async ValueTask AppendTickAsync(Tick tick, CancellationToken cancellationToken = default)
        {
            await AppendTicksAsync(new[] { tick }, cancellationToken);
        }

        public async Task AppendTicksAsync(IReadOnlyCollection<Tick> ticks, CancellationToken cancellationToken = default)
        {
            if (ticks == null) throw new ArgumentNullException(nameof(ticks));
            if (ticks.Count == 0) return;

            // Validate all ticks before performing import
            foreach (var tick in ticks)
            {
                if (tick.Bid <= 0 || tick.Ask <= 0)
                    throw new ArgumentException("Bid and Ask values must be positive.", nameof(ticks));
                if (tick.Ask < tick.Bid)
                    throw new ArgumentException($"Ask ({tick.Ask}) must be greater than or equal to Bid ({tick.Bid}). Inverted spread is not allowed.", nameof(ticks));
                if (tick.Time.Kind != DateTimeKind.Utc)
                    throw new ArgumentException($"Tick timestamp must be UTC. Got: {tick.Time.Kind}", nameof(ticks));
            }

            bool isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            if (isInMemory)
            {
                lock (Lock)
                {
                    FallbackTicks.AddRange(ticks);
                }
                return;
            }

            var conn = _context.Database.GetDbConnection();
            if (conn is NpgsqlConnection npgsqlConn)
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var writer = await npgsqlConn.BeginBinaryImportAsync(
                    "COPY market_ticks (symbol, time_utc, bid, ask, spread, source) FROM STDIN (FORMAT BINARY)",
                    cancellationToken
                );

                foreach (var tick in ticks)
                {
                    await writer.StartRowAsync(cancellationToken);
                    await writer.WriteAsync(tick.Symbol.Name, cancellationToken);
                    await writer.WriteAsync(tick.Time, cancellationToken);
                    await writer.WriteAsync(tick.Bid, cancellationToken);
                    await writer.WriteAsync(tick.Ask, cancellationToken);
                    await writer.WriteAsync(tick.Spread, cancellationToken);
                    await writer.WriteAsync("NTE", cancellationToken);
                }

                await writer.CompleteAsync(cancellationToken);
            }
            else
            {
                // Fallback for SQLite or other relational provider
                try
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        await conn.OpenAsync(cancellationToken);
                    }

                    foreach (var tick in ticks)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO market_ticks (symbol, time_utc, bid, ask, spread, source) VALUES (@sym, @time, @bid, @ask, @spread, 'NTE')";

                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@sym"; p1.Value = tick.Symbol.Name; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@time"; p2.Value = tick.Time; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@bid"; p3.Value = tick.Bid; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@ask"; p4.Value = tick.Ask; cmd.Parameters.Add(p4);
                        var p5 = cmd.CreateParameter(); p5.ParameterName = "@spread"; p5.Value = tick.Spread; cmd.Parameters.Add(p5);

                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch
                {
                    // Fallback to memory list if relational insertion fails
                    lock (Lock)
                    {
                        FallbackTicks.AddRange(ticks);
                    }
                }
            }
        }



        public async Task<IReadOnlyList<Candle>> GetCandlesAsync(
            string symbol,
            string timeframe,
            DateTime startDate,
            DateTime endDate,
            CancellationToken ct = default)
        {
            var domainCandles = new List<Candle>();

            try
            {
                // 1. Fetch raw Bars using the existing battle-tested Data Access method
                var bars = await GetBarsAsync(new Symbol(symbol), timeframe, startDate, endDate, ct);

                // 2. Map legacy Bar objects into the newer immutable Candle objects expected by the AI Pipeline
                foreach (var bar in bars)
                {
                    try
                    {
                        var candle = new Candle(
                            symbol: bar.Symbol,
                            timeframe: new Timeframe(bar.Timeframe),
                            timestamp: bar.Time,
                            open: new Price(bar.Open),
                            high: new Price(bar.High),
                            low: new Price(bar.Low),
                            close: new Price(bar.Close),
                            volume: new Volume(bar.Volume)
                        );

                        domainCandles.Add(candle);
                    }
                    catch
                    {
                        // Skip malformed data
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback / log mapping errors
                Console.WriteLine($"[MarketDataRepository] Error mapping Bars to Candles: {ex.Message}");
            }

            return domainCandles;
        }


        public async Task AppendBarsAsync(IReadOnlyCollection<Bar> bars, CancellationToken cancellationToken = default)
        {
            if (bars == null) throw new ArgumentNullException(nameof(bars));
            if (bars.Count == 0) return;

            foreach (var bar in bars)
            {
                if (bar.Time.Kind != DateTimeKind.Utc)
                    throw new ArgumentException("Bar timestamp must be UTC.", nameof(bars));
                if (bar.Open < 0 || bar.High < 0 || bar.Low < 0 || bar.Close < 0)
                    throw new ArgumentException("Bar prices must be positive.", nameof(bars));
            }

            bool isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            if (isInMemory)
            {
                lock (Lock)
                {
                    FallbackBars.AddRange(bars);
                }
                return;
            }

            var conn = _context.Database.GetDbConnection();
            if (conn is NpgsqlConnection npgsqlConn)
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var writer = await npgsqlConn.BeginBinaryImportAsync(
                    "COPY market_bars (symbol, timeframe, time_utc, open_price, high_price, low_price, close_price, volume) FROM STDIN (FORMAT BINARY)",
                    cancellationToken
                );

                foreach (var bar in bars)
                {
                    await writer.StartRowAsync(cancellationToken);
                    await writer.WriteAsync(bar.Symbol.Name, cancellationToken);
                    await writer.WriteAsync(bar.Timeframe, cancellationToken);
                    await writer.WriteAsync(bar.Time, cancellationToken);
                    await writer.WriteAsync(bar.Open, cancellationToken);
                    await writer.WriteAsync(bar.High, cancellationToken);
                    await writer.WriteAsync(bar.Low, cancellationToken);
                    await writer.WriteAsync(bar.Close, cancellationToken);
                    await writer.WriteAsync(bar.Volume, cancellationToken);
                }

                await writer.CompleteAsync(cancellationToken);
            }
            else
            {
                try
                {
                    if (conn.State != ConnectionState.Open)
                    {
                        await conn.OpenAsync(cancellationToken);
                    }

                    foreach (var bar in bars)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO market_bars (symbol, timeframe, time_utc, open_price, high_price, low_price, close_price, volume) VALUES (@sym, @tf, @time, @open, @high, @low, @close, @vol)";

                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@sym"; p1.Value = bar.Symbol.Name; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@tf"; p2.Value = bar.Timeframe; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@time"; p3.Value = bar.Time; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@open"; p4.Value = bar.Open; cmd.Parameters.Add(p4);
                        var p5 = cmd.CreateParameter(); p5.ParameterName = "@high"; p5.Value = bar.High; cmd.Parameters.Add(p5);
                        var p6 = cmd.CreateParameter(); p6.ParameterName = "@low"; p6.Value = bar.Low; cmd.Parameters.Add(p6);
                        var p7 = cmd.CreateParameter(); p7.ParameterName = "@close"; p7.Value = bar.Close; cmd.Parameters.Add(p7);
                        var p8 = cmd.CreateParameter(); p8.ParameterName = "@vol"; p8.Value = bar.Volume; cmd.Parameters.Add(p8);

                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch
                {
                    lock (Lock)
                    {
                        FallbackBars.AddRange(bars);
                    }
                }
            }
        }

        public async IAsyncEnumerable<Tick> StreamTicksAsync(
            Symbol symbol,
            DateTime fromUtc,
            DateTime toUtc,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));
            if (fromUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("From timestamp must be UTC.", nameof(fromUtc));
            if (toUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("To timestamp must be UTC.", nameof(toUtc));

            bool isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            if (isInMemory)
            {
                List<Tick> localTicks;
                lock (Lock)
                {
                    localTicks = FallbackTicks.Where(t => t.Symbol.Equals(symbol) && t.Time >= fromUtc && t.Time <= toUtc).OrderBy(t => t.Time).ToList();
                }
                foreach (var t in localTicks)
                {
                    yield return t;
                }
                yield break;
            }

            var conn = _context.Database.GetDbConnection();
            if (conn is NpgsqlConnection)
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var cmd = new NpgsqlCommand(
                    "SELECT symbol, time_utc, bid, ask FROM market_ticks WHERE symbol = @symbol AND time_utc >= @from AND time_utc <= @to ORDER BY time_utc ASC",
                    (NpgsqlConnection)conn
                );
                cmd.Parameters.AddWithValue("symbol", symbol.Name);
                cmd.Parameters.AddWithValue("from", fromUtc);
                cmd.Parameters.AddWithValue("to", toUtc);

                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var symName = reader.GetString(0);
                    var time = reader.GetDateTime(1);
                    var bid = reader.GetDouble(2);
                    var ask = reader.GetDouble(3);

                    yield return new Tick(new Symbol(symName), DateTime.SpecifyKind(time, DateTimeKind.Utc), bid, ask);
                }
            }
            else
            {
                List<Tick> localTicks;
                lock (Lock)
                {
                    localTicks = FallbackTicks.Where(t => t.Symbol.Equals(symbol) && t.Time >= fromUtc && t.Time <= toUtc).OrderBy(t => t.Time).ToList();
                }
                foreach (var t in localTicks)
                {
                    yield return t;
                }
            }
        }

        public async Task<IReadOnlyList<Bar>> GetBarsAsync(Symbol symbol, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));
            if (fromframeEmptyOrNull(timeframe)) throw new ArgumentException("Timeframe cannot be empty.", nameof(timeframe));
            if (fromUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("From timestamp must be UTC.", nameof(fromUtc));
            if (toUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("To timestamp must be UTC.", nameof(toUtc));

            bool isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            if (isInMemory)
            {
                lock (Lock)
                {
                    return FallbackBars.Where(b => b.Symbol.Equals(symbol) && b.Timeframe == timeframe && b.Time >= fromUtc && b.Time <= toUtc).OrderBy(b => b.Time).ToList();
                }
            }

            var conn = _context.Database.GetDbConnection();
            if (conn is NpgsqlConnection)
            {
                if (conn.State != ConnectionState.Open)
                {
                    await conn.OpenAsync(cancellationToken);
                }

                using var cmd = new NpgsqlCommand(
                    "SELECT symbol, timeframe, time_utc, open_price, high_price, low_price, close_price, volume FROM market_bars WHERE symbol = @symbol AND timeframe = @tf AND time_utc >= @from AND time_utc <= @to ORDER BY time_utc ASC",
                    (NpgsqlConnection)conn
                );
                cmd.Parameters.AddWithValue("symbol", symbol.Name);
                cmd.Parameters.AddWithValue("tf", timeframe);
                cmd.Parameters.AddWithValue("from", fromUtc);
                cmd.Parameters.AddWithValue("to", toUtc);

                var bars = new List<Bar>();
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var symName = reader.GetString(0);
                    var tf = reader.GetString(1);
                    var time = reader.GetDateTime(2);
                    var open = reader.GetDouble(3);
                    var high = reader.GetDouble(4);
                    var low = reader.GetDouble(5);
                    var close = reader.GetDouble(6);
                    var vol = reader.GetDouble(7);

                    bars.Add(new Bar(new Symbol(symName), tf, DateTime.SpecifyKind(time, DateTimeKind.Utc), open, high, low, close, vol));
                }

                return bars;
            }
            else
            {
                lock (Lock)
                {
                    return FallbackBars.Where(b => b.Symbol.Equals(symbol) && b.Timeframe == timeframe && b.Time >= fromUtc && b.Time <= toUtc).OrderBy(b => b.Time).ToList();
                }
            }
        }

        private static bool fromframeEmptyOrNull(string tf) => string.IsNullOrWhiteSpace(tf);
    }
}

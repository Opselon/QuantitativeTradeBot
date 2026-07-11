using System;
using System.Collections.Generic;
using System.Data;
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

            var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            using var writer = await conn.BeginBinaryImportAsync(
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

            var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            using var writer = await conn.BeginBinaryImportAsync(
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

            var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            using var cmd = new NpgsqlCommand(
                "SELECT symbol, time_utc, bid, ask FROM market_ticks WHERE symbol = @symbol AND time_utc >= @from AND time_utc <= @to ORDER BY time_utc ASC",
                conn
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

        public async Task<IReadOnlyList<Bar>> GetBarsAsync(Symbol symbol, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));
            if (fromframeEmptyOrNull(timeframe)) throw new ArgumentException("Timeframe cannot be empty.", nameof(timeframe));
            if (fromUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("From timestamp must be UTC.", nameof(fromUtc));
            if (toUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("To timestamp must be UTC.", nameof(toUtc));

            var conn = (NpgsqlConnection)_context.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            using var cmd = new NpgsqlCommand(
                "SELECT symbol, timeframe, time_utc, open_price, high_price, low_price, close_price, volume FROM market_bars WHERE symbol = @symbol AND timeframe = @tf AND time_utc >= @from AND time_utc <= @to ORDER BY time_utc ASC",
                conn
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

        private static bool fromframeEmptyOrNull(string tf) => string.IsNullOrWhiteSpace(tf);
    }
}

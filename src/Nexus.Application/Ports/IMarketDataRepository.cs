using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;
using Nexus.Core.ValueObjects;

namespace Nexus.Application.Ports
{
    public interface IMarketDataRepository
    {
        ValueTask AppendTickAsync(Tick tick, CancellationToken cancellationToken = default);
        Task AppendTicksAsync(IReadOnlyCollection<Tick> ticks, CancellationToken cancellationToken = default);
        Task AppendBarsAsync(IReadOnlyCollection<Bar> bars, CancellationToken cancellationToken = default);
        IAsyncEnumerable<Tick> StreamTicksAsync(Symbol symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Bar>> GetBarsAsync(Symbol symbol, string timeframe, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    }
}

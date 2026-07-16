using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.MarketIntelligence.DataSources
{
    /// <summary>
    /// Service provider interface for macro economic data and calendar releases.
    /// </summary>
    public interface IMacroDataProvider
    {
        /// <summary>
        /// Retrieves upcoming macro economic and calendar releases.
        /// </summary>
        Task<IReadOnlyList<EconomicEvent>> GetUpcomingEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    }
}

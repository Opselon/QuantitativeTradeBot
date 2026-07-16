using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus.MarketIntelligence.DataSources
{
    /// <summary>
    /// Service provider interface for financial news feeds.
    /// </summary>
    public interface INewsProvider
    {
        /// <summary>
        /// Retrieves the latest financial news articles/events.
        /// </summary>
        Task<IReadOnlyList<NewsEvent>> GetLatestNewsAsync(int limit = 50, CancellationToken ct = default);
    }
}

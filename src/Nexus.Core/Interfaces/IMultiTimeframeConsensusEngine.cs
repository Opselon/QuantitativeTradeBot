using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    /// <summary>
    /// Service contract for multi-timeframe analytical consensus tracking.
    /// Manages independent timeframe signals (from M1 up to D1) and merges them to extract an overall market bias and entry triggers.
    /// </summary>
    public interface IMultiTimeframeConsensusEngine
    {
        /// <summary>
        /// Registers a signal received from a specific timeframe.
        /// </summary>
        void RegisterTimeframeSignal(MultiTimeframeSignal signal);

        /// <summary>
        /// Evaluates all active registered timeframe signals and produces a consolidated consensus.
        /// </summary>
        ConsensusState GetCurrentConsensus();
    }
}

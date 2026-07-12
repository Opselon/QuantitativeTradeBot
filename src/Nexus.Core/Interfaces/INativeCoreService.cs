using System;
using Nexus.Core.Entities;

namespace Nexus.Core.Interfaces
{
    public interface INativeCoreService
    {
        bool IsAvailable { get; }
        string LastError { get; }
        void UpdateTick(Tick tick);
        MarketVector GetMarketVector();
        MarketState GetMarketState();
    }
}

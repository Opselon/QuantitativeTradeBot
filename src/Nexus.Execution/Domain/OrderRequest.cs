using Nexus.Execution.Enums;

namespace Nexus.Execution.Domain
{
    public class OrderRequest
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Symbol { get; }
        public string Side { get; } // e.g. "Buy" or "Sell"
        public double Volume { get; }
        public double Entry { get; }
        public double? StopLoss { get; set; }
        public double? TakeProfit { get; set; }
        public string Reason { get; }
        public ExecutionState State { get; private set; } = ExecutionState.Created;
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

        public OrderRequest(string symbol, string side, double volume, double entry, double? stopLoss, double? takeProfit, string reason)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Side = side ?? throw new ArgumentNullException(nameof(side));
            Volume = volume;
            Entry = entry;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            Reason = reason ?? string.Empty;
        }

        public void TransitionTo(ExecutionState newState)
        {
            State = newState;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

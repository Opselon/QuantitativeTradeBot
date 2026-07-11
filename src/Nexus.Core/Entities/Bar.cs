using System;
using Nexus.Core.ValueObjects;

namespace Nexus.Core.Entities
{
    public sealed class Bar
    {
        public Symbol Symbol { get; }
        public string Timeframe { get; }
        public DateTime Time { get; }
        public double Open { get; private set; }
        public double High { get; private set; }
        public double Low { get; private set; }
        public double Close { get; private set; }
        public double Volume { get; private set; }

        public Bar(Symbol symbol, string timeframe, DateTime time, double open, double high, double low, double close, double volume)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Timeframe = timeframe ?? throw new ArgumentNullException(nameof(timeframe));
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public void Update(double price, double volumeIncrement)
        {
            if (price > High) High = price;
            if (price < Low) Low = price;
            Close = price;
            Volume += volumeIncrement;
        }

        public override string ToString() => $"{Symbol} ({Timeframe}) {Time:yyyy-MM-dd HH:mm} O:{Open:F5} H:{High:F5} L:{Low:F5} C:{Close:F5} V:{Volume:F2}";
    }
}

using System;

namespace Nexus.Application.Mt5
{
    public class OpenPositionDto
    {
        public long Ticket { get; }
        public string Symbol { get; }
        public string Side { get; } // "Buy" or "Sell"
        public decimal Volume { get; }
        public decimal OpenPrice { get; }
        public decimal CurrentPrice { get; }
        public decimal StopLoss { get; }
        public decimal TakeProfit { get; }
        public decimal Profit { get; }
        public decimal Swap { get; }
        public long MagicNumber { get; }
        public string Comment { get; }
        public DateTime OpenTime { get; }

        public OpenPositionDto(
            long ticket,
            string symbol,
            string side,
            decimal volume,
            decimal openPrice,
            decimal currentPrice,
            decimal stopLoss,
            decimal takeProfit,
            decimal profit,
            decimal swap,
            long magicNumber,
            string comment,
            DateTime openTime)
        {
            Ticket = ticket;
            Symbol = symbol;
            Side = side;
            Volume = volume;
            OpenPrice = openPrice;
            CurrentPrice = currentPrice;
            StopLoss = stopLoss;
            TakeProfit = takeProfit;
            Profit = profit;
            Swap = swap;
            MagicNumber = magicNumber;
            Comment = comment;
            OpenTime = openTime;
        }
    }
}

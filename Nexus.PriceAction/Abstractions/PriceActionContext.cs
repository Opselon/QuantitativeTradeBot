// Imports the System namespace for fundamental base types and exceptions. [Ref: Core-Sys]
using System;
// Imports the Concurrent collections for thread-safe dictionary operations. [Ref: Core-ThreadSafe]
using System.Collections.Concurrent;
// Imports generic collections for defining IReadOnlyList. [Ref: Core-Collections]
using System.Collections.Generic;
// Maps the Domain Candle entity to avoid namespace collisions with the Candle folder. [Ref: CS0118-Fix]
using DomainCandle = Nexus.Core.Entities.Candle;
// Imports the candle analysis result model. [Ref: Domain-Models]
using Nexus.PriceAction.Candle.Models;

// Defines the namespace for abstractions used across all price action engines. [Ref: Arch-Layer]
namespace Nexus.PriceAction.Abstractions
{
    // Declares the main context class that travels through the pipeline. [Ref: Pipe-Filter-Pattern]
    public class PriceActionContext
    {
        // Holds the trading symbol (e.g., EURUSD) for this context. [Ref: Context-State]
        public string Symbol { get; }

        // Holds the timeframe (e.g., M15, H1) of the current data flow. [Ref: Context-State]
        public string Timeframe { get; }

        // Holds the immutable list of raw domain candles fetched from the database. [Ref: Context-State]
        public IReadOnlyList<DomainCandle> RawCandles { get; }

        // Provides a thread-safe dictionary to store analysis results keyed by timestamp. [Ref: Thread-Safety]
        public ConcurrentDictionary<DateTime, CandleAnalysisResult> CandleResults { get; }

        // Initializes a new instance of the PriceActionContext with required parameters. [Ref: Constructor]
        public PriceActionContext(string symbol, string timeframe, IReadOnlyList<DomainCandle> rawCandles)
        {
            // Validates that the symbol is not null or empty to prevent invalid state. [Ref: Guard-Clause]
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentNullException(nameof(symbol));

            // Validates that the timeframe is not null or empty. [Ref: Guard-Clause]
            if (string.IsNullOrWhiteSpace(timeframe)) throw new ArgumentNullException(nameof(timeframe));

            // Assigns the validated symbol to the readonly property. [Ref: Assignment]
            Symbol = symbol;

            // Assigns the validated timeframe to the readonly property. [Ref: Assignment]
            Timeframe = timeframe;

            // Validates the candle list and assigns it, throwing an exception if null. [Ref: Guard-Clause]
            RawCandles = rawCandles ?? throw new ArgumentNullException(nameof(rawCandles));

            // Initializes the concurrent dictionary to hold pipeline results safely. [Ref: Initialization]
            CandleResults = new ConcurrentDictionary<DateTime, CandleAnalysisResult>();
        }
    }
}
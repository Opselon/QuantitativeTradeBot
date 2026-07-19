// ============================================================================
// PROJECT: NEXUS QUANTITATIVE TRADING PLATFORM
// LAYER:   APPLICATION LAYER (Observability & Diagnostics Ports)
// FILE:    DiagnosticRingBuffer.cs
// REFERENCED BY: 
//   - src/Nexus.Desktop/ViewModels/Workspaces/DiagnosticsViewModel.cs (UI Binding)
//   - src/Nexus.Infrastructure/Mt5Bridge/Mt5BridgeService.cs (Log Publisher)
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nexus.Application.Ports;

namespace Nexus.Application.Observability
{
    /// <summary>
    /// Represents a high-performance, thread-safe, memory-bounded ring buffer 
    /// designed to store bridge diagnostic log entries in-memory.
    /// Provides real-time reactive events for presentation boundaries.
    /// </summary>
    public class DiagnosticRingBuffer
    {
        /// <summary>
        /// The maximum limit of log entries allowed in the queue before discarding older records.
        /// Prevents unbounded memory growth in the trading application context.
        /// </summary>
        private readonly int _capacity;

        /// <summary>
        /// The internal first-in, first-out queue storing the diagnostic log entries.
        /// Access must be synchronized using the private lock object.
        /// </summary>
        private readonly Queue<BridgeDiagnosticLogEntry> _queue = new();

        /// <summary>
        /// Dedicated mutual exclusion synchronization object to secure the internal queue 
        /// from concurrent read/write race conditions across multiple worker threads.
        /// </summary>
        private readonly object _lock = new();

        /// <summary>
        /// A running counter tracking the total number of log entries dropped due to capacity limits.
        /// Useful for telemetry and monitoring queue overflow events.
        /// </summary>
        private long _droppedCount;

        /// <summary>
        /// Gets the maximum capacity threshold allocated for this diagnostic ring buffer instance.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets the total cumulative count of diagnostic log entries dropped since buffer initialization.
        /// </summary>
        public long DroppedCount => _droppedCount;

        /// <summary>
        /// Multicast event triggered immediately after a new diagnostic entry is appended to the buffer.
        /// Allows presentation layer view models to reactively stream logs on WPF grids in real-time.
        /// </summary>
        public event EventHandler<BridgeDiagnosticLogEntry>? EntryAdded;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticRingBuffer"/> class with a specified capacity.
        /// </summary>
        /// <param name="capacity">The maximum number of logs to retain. Clamped between 100 and 10000.</param>
        public DiagnosticRingBuffer(int capacity = 1000)
        {
            // Validate and enforce guard limits on the buffer capacity size
            if (capacity < 100 || capacity > 10000)
            {
                // Fallback to safe default if input is out of stable bounds
                capacity = 1000;
            }

            // Assign verified capacity to the read-only backing field
            _capacity = capacity;
        }

        /// <summary>
        /// Thread-safely appends a new bridge diagnostic log entry into the ring buffer queue.
        /// Discards oldest log entries if the maximum capacity limit has been exceeded.
        /// </summary>
        /// <param name="entry">The diagnostic log entry containing metadata, category, and payload.</param>
        public void Add(BridgeDiagnosticLogEntry entry)
        {
            // Guard clause to reject null entries and prevent NullReferenceException down the road
            if (entry == null) return;

            // Enter a mutual exclusion lock to synchronize writes on the inner queue
            lock (_lock)
            {
                // Loop to clear space if the queue has reached or exceeded its designated capacity limit
                while (_queue.Count >= _capacity)
                {
                    // Dequeue the oldest log record from the front of the queue
                    _queue.Dequeue();

                    // Increment the atomic tracking counter for dropped records
                    _droppedCount++;
                }

                // Enqueue the newly arrived log entry to the rear of the queue
                _queue.Enqueue(entry);
            }

            // Raise the notification event outside the lock scope.
            // This is a critical security-by-default measure to prevent deadlocks 
            // if any event listener blocks or invokes synchronous operations on other threads.
            EntryAdded?.Invoke(this, entry);
        }

        /// <summary>
        /// Thread-safely queries the cached log entries, applying optional filters and size limitations.
        /// </summary>
        /// <param name="severity">Optional severity level filter (e.g., INFO, WARN, ERROR).</param>
        /// <param name="category">Optional category filter indicating the logging subsystem.</param>
        /// <param name="symbol">Optional trading instrument filter (e.g., EURUSD, XAUUSD).</param>
        /// <param name="limit">Optional maximum limit of records to return from the end of the buffer.</param>
        /// <returns>An immutable read-only snapshot list of the matching diagnostic log entries.</returns>
        public IReadOnlyList<BridgeDiagnosticLogEntry> Query(
            string? severity = null,
            string? category = null,
            string? symbol = null,
            int? limit = null)
        {
            // Synchronize read access on the queue to prevent collection modification exceptions
            lock (_lock)
            {
                // Convert the queue to an enumerable chain to begin lazy evaluation pipeline
                var query = _queue.AsEnumerable();

                // Apply severity filtering if a non-whitespace search argument was provided
                if (!string.IsNullOrWhiteSpace(severity))
                {
                    // Case-insensitive evaluation of the Severity field
                    query = query.Where(e => string.Equals(e.Severity, severity, StringComparison.OrdinalIgnoreCase));
                }

                // Apply category filtering if a non-whitespace search argument was provided
                if (!string.IsNullOrWhiteSpace(category))
                {
                    // Case-insensitive evaluation of the Category field
                    query = query.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
                }

                // Apply trading symbol filtering if a non-whitespace search argument was provided
                if (!string.IsNullOrWhiteSpace(symbol))
                {
                    // Case-insensitive evaluation of the Symbol field
                    query = query.Where(e => string.Equals(e.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
                }

                // Apply quantity limitation to retrieve only the last N items of the matched criteria
                if (limit.HasValue && limit.Value > 0)
                {
                    // TakeLast grabs elements from the tail of the sequence preserving historical order
                    query = query.TakeLast(limit.Value);
                }

                // Materialize the filtered query into a static, thread-safe memory list allocation
                return query.ToList();
            }
        }

        /// <summary>
        /// Exports the filtered subset of diagnostic logs into a unified JSON newline string.
        /// Commonly consumed by administrative file exporters or analytics tools.
        /// </summary>
        /// <param name="severity">Severity level filter to apply.</param>
        /// <param name="category">Category subsystem filter to apply.</param>
        /// <param name="symbol">Financial trading instrument filter to apply.</param>
        /// <returns>A newline separated sequence of serialized JSON diagnostic records.</returns>
        public string ExportFilteredToJsonLines(
            string? severity = null,
            string? category = null,
            string? symbol = null)
        {
            // Retrieve the matching records using the thread-safe Query function
            var logs = Query(severity, category, symbol);

            // Serialize each record individually into a JSON string using LINQ projection
            var lines = logs.Select(e => JsonSerializer.Serialize(e));

            // Concatenate all serialized elements into a single payload using Unix-style newline spacing
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Resets the internal state of the buffer, clearing all cached logs and telemetry metrics.
        /// </summary>
        public void Clear()
        {
            // Lock to ensure atomic state cleanup
            lock (_lock)
            {
                // Flush the inner collection to free memory allocations of diagnostic entries
                _queue.Clear();

                // Reset telemetry counters to default initial state
                _droppedCount = 0;
            }
        }
    }
}
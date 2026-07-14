using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nexus.Application.Ports;

namespace Nexus.Application.Observability
{
    public class DiagnosticRingBuffer
    {
        private readonly int _capacity;
        private readonly Queue<BridgeDiagnosticLogEntry> _queue = new();
        private readonly object _lock = new();
        private long _droppedCount;

        public int Capacity => _capacity;
        public long DroppedCount => _droppedCount;

        public DiagnosticRingBuffer(int capacity = 1000)
        {
            if (capacity < 100 || capacity > 10000)
            {
                capacity = 1000;
            }
            _capacity = capacity;
        }

        public void Add(BridgeDiagnosticLogEntry entry)
        {
            if (entry == null) return;

            // Sanitize sensitive credentials from the diagnostic log entry
            entry.Message = LogSanitizer.Sanitize(entry.Message);
            entry.PayloadSummary = LogSanitizer.Sanitize(entry.PayloadSummary);
            entry.ExceptionSummary = LogSanitizer.Sanitize(entry.ExceptionSummary);

            lock (_lock)
            {
                while (_queue.Count >= _capacity)
                {
                    _queue.Dequeue();
                    _droppedCount++;
                }
                _queue.Enqueue(entry);
            }
        }

        public IReadOnlyList<BridgeDiagnosticLogEntry> Query(
            string? severity = null,
            string? category = null,
            string? symbol = null,
            int? limit = null)
        {
            lock (_lock)
            {
                var query = _queue.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(severity))
                {
                    query = query.Where(e => string.Equals(e.Severity, severity, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(symbol))
                {
                    query = query.Where(e => string.Equals(e.Symbol, symbol, StringComparison.OrdinalIgnoreCase));
                }

                if (limit.HasValue && limit.Value > 0)
                {
                    query = query.TakeLast(limit.Value);
                }

                return query.ToList();
            }
        }

        public string ExportFilteredToJsonLines(
            string? severity = null,
            string? category = null,
            string? symbol = null)
        {
            var logs = Query(severity, category, symbol);
            var lines = logs.Select(e => JsonSerializer.Serialize(e));
            return string.Join("\n", lines);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
                _droppedCount = 0;
            }
        }
    }
}

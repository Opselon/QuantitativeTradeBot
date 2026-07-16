using System;
using System.Collections.Generic;
using System.Linq;
using Nexus.Core.Entities;

namespace Nexus.Training
{
    /// <summary>
    /// Thread-safe reinforcement learning experience replay storage.
    /// Stores historical experience samples and retrieves subsets to avoid learning biases.
    /// </summary>
    public sealed class ExperienceReplayBuffer
    {
        private readonly List<ExperienceSample> _buffer = new();
        private readonly object _lock = new();
        private readonly int _maxCapacity;

        public int Count
        {
            get
            {
                lock (_lock) return _buffer.Count;
            }
        }

        public ExperienceReplayBuffer(int maxCapacity = 10000)
        {
            if (maxCapacity <= 0) throw new ArgumentException("Capacity must be positive.", nameof(maxCapacity));
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Adds a single experience sample to the replay buffer.
        /// Evicts the oldest sample if the buffer reaches maximum capacity.
        /// </summary>
        public void Add(ExperienceSample experience)
        {
            if (experience == null) throw new ArgumentNullException(nameof(experience));

            lock (_lock)
            {
                if (_buffer.Count >= _maxCapacity)
                {
                    _buffer.RemoveAt(0); // FIFO eviction
                }
                _buffer.Add(experience);
            }
        }

        /// <summary>
        /// Adds a collection of experience samples to the replay buffer.
        /// </summary>
        public void AddRange(IEnumerable<ExperienceSample> experiences)
        {
            if (experiences == null) throw new ArgumentNullException(nameof(experiences));

            lock (_lock)
            {
                foreach (var exp in experiences)
                {
                    Add(exp);
                }
            }
        }

        /// <summary>
        /// Clears all experiences currently stored in the replay buffer.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }

        /// <summary>
        /// Retrieves a random batch of experiences of the specified size.
        /// </summary>
        public IReadOnlyList<ExperienceSample> GetRandomSamples(int batchSize)
        {
            if (batchSize <= 0) return Array.Empty<ExperienceSample>();

            lock (_lock)
            {
                if (_buffer.Count == 0) return Array.Empty<ExperienceSample>();

                var random = new Random();
                var list = _buffer.OrderBy(_ => random.Next()).Take(batchSize).ToList();
                return list;
            }
        }

        /// <summary>
        /// Retrieves experiences that occurred within a specific UTC time range.
        /// </summary>
        public IReadOnlyList<ExperienceSample> GetTimeBasedSamples(DateTime startUtc, DateTime endUtc)
        {
            lock (_lock)
            {
                return _buffer
                    .Where(x => x.TimestampUtc >= startUtc && x.TimestampUtc <= endUtc)
                    .OrderBy(x => x.TimestampUtc)
                    .ToList();
            }
        }

        /// <summary>
        /// Retrieves experiences that occurred under a specific market regime.
        /// </summary>
        public IReadOnlyList<ExperienceSample> GetRegimeBasedSamples(string regime, int limit)
        {
            if (string.IsNullOrWhiteSpace(regime)) return Array.Empty<ExperienceSample>();
            if (limit <= 0) return Array.Empty<ExperienceSample>();

            lock (_lock)
            {
                return _buffer
                    .Where(x => string.Equals(x.MarketRegimeLabel, regime, StringComparison.OrdinalIgnoreCase))
                    .Take(limit)
                    .ToList();
            }
        }

        /// <summary>
        /// Returns all experience samples stored in the buffer.
        /// </summary>
        public IReadOnlyList<ExperienceSample> GetAllSamples()
        {
            lock (_lock)
            {
                return _buffer.ToList();
            }
        }
    }
}

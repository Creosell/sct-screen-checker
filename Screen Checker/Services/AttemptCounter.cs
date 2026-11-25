using System.Collections.Concurrent;
using ScreenChecker.Interfaces;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Keeps the number of rejected measurement attempts per measurement point in memory.
    /// </summary>
    public class AttemptCounter : IAttemptCounter
        {
        private readonly ConcurrentDictionary<string, int> _counts = new();

        /// <inheritdoc />
        public int Get(string location) => _counts.GetValueOrDefault(location);

        /// <inheritdoc />
        public void Increment(string location) => _counts.AddOrUpdate(location, 1, (_, count) => count + 1);

        /// <inheritdoc />
        public void Reset(string location) => _counts[location] = 0;
        }
    }

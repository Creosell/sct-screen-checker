namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// Counts the rejected measurement attempts of every measurement point in a row.
    /// </summary>
    public interface IAttemptCounter
    {
        /// <summary>
        /// Gets the number of rejected attempts counted for the point.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        /// <returns>The number of rejected attempts; 0 when none was counted.</returns>
        int Get(string location);

        /// <summary>
        /// Counts one more rejected attempt of the point.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        void Increment(string location);

        /// <summary>
        /// Resets the counter of the point to zero.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        void Reset(string location);
    }
}

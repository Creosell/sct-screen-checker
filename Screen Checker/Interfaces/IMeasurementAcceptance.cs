using ScreenChecker.Models;

namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// Verdict on a measurement the analyzer returned.
    /// </summary>
    public abstract record AcceptanceResult
    {
        /// <summary>The measurement is plausible and is taken as an attempt of its point.</summary>
        public sealed record Accepted : AcceptanceResult;

        /// <summary>The measurement is implausible (the probe is probably misplaced): the operator is asked to measure again.</summary>
        /// <param name="Reason">What is wrong with the measurement.</param>
        /// <param name="Attempt">Number of the rejected attempt of the point, starting from 1.</param>
        public sealed record Rejected(string Reason, int Attempt) : AcceptanceResult;

        /// <summary>The measurement is implausible again after the repeat: the operator decides whether to keep it.</summary>
        /// <param name="Reason">What is wrong with the measurement.</param>
        public sealed record ConfirmationNeeded(string Reason) : AcceptanceResult;
    }

    /// <summary>
    /// Decides whether a measurement taken by the analyzer is plausible enough to be kept.
    /// </summary>
    public interface IMeasurementAcceptance
    {
        /// <summary>
        /// Checks the luminance and the coordinates of the measurement against the point it was taken at and counts
        /// the rejected attempts of the point in a row.
        /// </summary>
        /// <param name="measurement">The measurement with its location.</param>
        /// <returns>The verdict.</returns>
        AcceptanceResult Assess(Measurement measurement);
    }
}

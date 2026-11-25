using ScreenChecker.Models;

namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// What the specification check needs to know about the current state of the measurement page.
    /// </summary>
    /// <param name="Configuration">Selected device configuration; empty when none is selected.</param>
    /// <param name="Device">Device under test; null before the first measurement (an empty device is evaluated instead).</param>
    /// <param name="PreviewSerialNumber">Serial number of the empty device evaluated when there is no device.</param>
    /// <param name="IsTv">Whether the device is a TV.</param>
    public readonly record struct SpecCheckRequest(string? Configuration, DeviceUnderTest? Device, string PreviewSerialNumber, bool IsTv);

    /// <summary>
    /// Outcome of a specification check, delivered to the UI thread.
    /// </summary>
    public abstract record SpecCheckResult
    {
        /// <summary>No configuration is selected, so there is nothing to check against.</summary>
        public sealed record ConfigurationNeeded : SpecCheckResult;

        /// <summary>The generator evaluated the device.</summary>
        /// <param name="Device">The evaluated device.</param>
        /// <param name="Evaluation">The evaluation.</param>
        /// <param name="MeasuredPoints">Number of points the device holds.</param>
        /// <param name="TotalPoints">Number of points the device has to have.</param>
        public sealed record Evaluated(DeviceUnderTest Device, SpecEvaluation Evaluation, int MeasuredPoints, int TotalPoints) : SpecCheckResult;

        /// <summary>The generator could not evaluate the device.</summary>
        /// <param name="Error">Reason reported by the generator.</param>
        /// <param name="IsNew">False when the same reason was already reported by the previous check.</param>
        public sealed record Unavailable(string Error, bool IsNew) : SpecCheckResult;
    }

    /// <summary>
    /// Runs the specification checks of the measurement page: only the latest request is answered, an older one is abandoned.
    /// </summary>
    public interface ISpecCheckCoordinator
    {
        /// <summary>
        /// Gets the task of the latest request; completes when its result was delivered or it was abandoned.
        /// </summary>
        Task Pending { get; }

        /// <summary>
        /// Starts a check, abandoning the previous one.
        /// </summary>
        /// <param name="request">State of the measurement page.</param>
        /// <param name="deliver">Receives the result on the UI thread, unless the request was abandoned.</param>
        void Request(SpecCheckRequest request, Action<SpecCheckResult> deliver);

        /// <summary>
        /// Abandons the running check.
        /// </summary>
        void Cancel();
    }
}

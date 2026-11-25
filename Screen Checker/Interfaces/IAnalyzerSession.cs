using System.ComponentModel;
using ScreenChecker.Models;

namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// How a zero calibration run ended; the analyzer is already disconnected after every failure.
    /// </summary>
    public abstract record CalibrationOutcome
    {
        /// <summary>The analyzer is connected, calibrated and its channel is confirmed.</summary>
        public sealed record Ready : CalibrationOutcome;

        /// <summary>The analyzer could not be connected.</summary>
        public sealed record ConnectFailed : CalibrationOutcome;

        /// <summary>The analyzer refused the zero calibration.</summary>
        public sealed record CalibrationFailed : CalibrationOutcome;

        /// <summary>The inspector dismissed the channel choice.</summary>
        public sealed record ChannelNotConfirmed : CalibrationOutcome;

        /// <summary>An unexpected error interrupted the run.</summary>
        public sealed record Faulted : CalibrationOutcome;
    }

    /// <summary>
    /// The work with the color analyzer: connection, zero calibration, confirmation of the active channel and measuring,
    /// with the state the measurement page shows.
    /// </summary>
    public interface IAnalyzerSession : INotifyPropertyChanged, IDisposable
    {
        /// <summary>
        /// Gets or sets whether the analyzer is connected; follows the events of the analyzer and is changed on the UI thread.
        /// </summary>
        bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets whether the analyzer is calibrated; a lost calibration also drops the channel confirmation.
        /// </summary>
        bool IsCalibrated { get; set; }

        /// <summary>
        /// Gets or sets whether the inspector has confirmed the active channel after the calibration.
        /// </summary>
        bool IsChannelConfirmed { get; set; }

        /// <summary>
        /// Gets whether a connection attempt is running.
        /// </summary>
        bool IsConnecting { get; }

        /// <summary>
        /// Gets whether a calibration run is in progress.
        /// </summary>
        bool IsCalibrating { get; }

        /// <summary>
        /// Gets the probe, channel and analyzer confirmed last; null before the first confirmation.
        /// </summary>
        ColorimeterInfo? Colorimeter { get; }

        /// <summary>
        /// Gets the family of the connected analyzer.
        /// </summary>
        ColorAnalyzerModel Model { get; }

        /// <summary>
        /// Gets the channel currently set on the analyzer.
        /// </summary>
        int CurrentChannel { get; }

        /// <summary>
        /// Gets the user-defined name of the current channel; empty when it has none.
        /// </summary>
        string ChannelName { get; }

        /// <summary>
        /// Occurs when the analyzer reports a status message (any thread).
        /// </summary>
        event EventHandler<string> StatusMessage;

        /// <summary>
        /// Occurs on the UI thread after the connection or the calibration state was refreshed, and after a calibration run ended,
        /// so the page recomputes what it enables.
        /// </summary>
        event EventHandler StateRefreshed;

        /// <summary>
        /// Connects the analyzer when needed, runs the zero calibration and confirms the active channel; on a failure the
        /// analyzer is disconnected first.
        /// </summary>
        /// <param name="report">Receives the outcome while the run is still marked as in progress.</param>
        /// <returns>The task of the run.</returns>
        Task CalibrateAsync(Action<CalibrationOutcome> report);

        /// <summary>
        /// Takes one measurement at the point the probe is placed.
        /// </summary>
        /// <param name="measurementTime">Measurement time setting.</param>
        /// <returns>The measurement as the analyzer returned it.</returns>
        Task<Measurement> MeasureAsync(int measurementTime);
    }
}

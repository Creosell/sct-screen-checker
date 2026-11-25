namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// Outcome of a report generator run.
    /// </summary>
    public enum ReportGenerationStatus
    {
        /// <summary>The reports were generated.</summary>
        Success,

        /// <summary>The generator found no measurement data to build a report from.</summary>
        NoDataFound,

        /// <summary>The generator rejected its configuration.</summary>
        ConfigError,

        /// <summary>The generator failed with another exit code.</summary>
        Failed,

        /// <summary>The run was canceled and the generator process was killed.</summary>
        Canceled
    }

    /// <summary>
    /// Result of a finished report generator run.
    /// </summary>
    /// <param name="Status">Meaning of the outcome.</param>
    /// <param name="ExitCode">Exit code of the generator process; 0 for a canceled run.</param>
    public readonly record struct ReportGenerationResult(ReportGenerationStatus Status, int ExitCode);

    /// <summary>
    /// Runs the Report Generator that builds the reports of the measured devices.
    /// </summary>
    public interface IReportGenerationService
    {
        /// <summary>
        /// Gets the full path where the generator executable is expected, next to the application.
        /// </summary>
        string ExecutablePath { get; }

        /// <summary>
        /// Gets whether the generator executable exists.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Runs the generator and waits for it to finish; cancelling the token kills the generator process tree.
        /// </summary>
        /// <param name="cancellationToken">Token that cancels the run.</param>
        /// <returns>The outcome of the run.</returns>
        Task<ReportGenerationResult> GenerateAsync(CancellationToken cancellationToken);
    }
}

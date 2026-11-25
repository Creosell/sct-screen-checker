using System.IO.Abstractions;
using ScreenChecker.Interfaces;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Runs <c>Report Generator.exe</c> from the application folder through <see cref="IProcessRunner"/>.
    /// </summary>
    public class ReportGenerationService : IReportGenerationService
        {
        private readonly IFileSystem _fileSystem;
        private readonly IPathProvider _pathProvider;
        private readonly IProcessRunner _processRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportGenerationService"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction used to look for the generator.</param>
        /// <param name="pathProvider">Provider of the application folder.</param>
        /// <param name="processRunner">Runner of the external generator process.</param>
        public ReportGenerationService(IFileSystem fileSystem, IPathProvider pathProvider, IProcessRunner processRunner)
            {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            }

        /// <inheritdoc />
        public string ExecutablePath => _fileSystem.Path.Combine(_pathProvider.BaseDirectory, ExternalApps.ReportGenerator);

        /// <inheritdoc />
        public bool IsAvailable => _fileSystem.File.Exists(ExecutablePath);

        /// <inheritdoc />
        public async Task<ReportGenerationResult> GenerateAsync(CancellationToken cancellationToken)
            {
            try
                {
                var result = await _processRunner.RunAsync(ExecutablePath, [], _pathProvider.BaseDirectory, string.Empty, Timeout.InfiniteTimeSpan, cancellationToken);
                return new ReportGenerationResult(ToStatus(result.ExitCode), result.ExitCode);
                }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                return new ReportGenerationResult(ReportGenerationStatus.Canceled, 0);
                }
            }

        private static ReportGenerationStatus ToStatus(int exitCode) => exitCode switch
            {
                0 => ReportGenerationStatus.Success,
                2 => ReportGenerationStatus.NoDataFound,
                3 => ReportGenerationStatus.ConfigError,
                _ => ReportGenerationStatus.Failed
                };
        }
    }

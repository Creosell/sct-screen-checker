using ScreenChecker.Interfaces;
using ScreenChecker.Models;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Evaluates the measurements through <see cref="ISpecEvaluationService"/> and answers only the latest request.
    /// </summary>
    public class SpecCheckCoordinator : ISpecCheckCoordinator
        {
        private readonly ISpecEvaluationService _specEvaluationService;
        private readonly IFileService _fileService;
        private readonly IMeasurementStatusService _measurementStatusService;
        private readonly IDispatcher _dispatcher;
        private CancellationTokenSource? _cts;
        private string? _lastError;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecCheckCoordinator"/> class.
        /// </summary>
        /// <param name="specEvaluationService">Service evaluating a device against its specification.</param>
        /// <param name="fileService">Service serializing the device for the evaluation.</param>
        /// <param name="measurementStatusService">Service owning the measurement buttons, whose number is the number of points.</param>
        /// <param name="dispatcher">Dispatcher delivering the results on the UI thread.</param>
        public SpecCheckCoordinator(ISpecEvaluationService specEvaluationService, IFileService fileService, IMeasurementStatusService measurementStatusService, IDispatcher dispatcher)
            {
            _specEvaluationService = specEvaluationService ?? throw new ArgumentNullException(nameof(specEvaluationService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _measurementStatusService = measurementStatusService ?? throw new ArgumentNullException(nameof(measurementStatusService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            }

        /// <inheritdoc />
        public Task Pending { get; private set; } = Task.CompletedTask;

        /// <inheritdoc />
        public void Request(SpecCheckRequest request, Action<SpecCheckResult> deliver) => Pending = CheckAsync(request, deliver);

        /// <inheritdoc />
        public void Cancel() => _cts?.Cancel();

        /// <summary>
        /// Evaluates the measurements taken so far against the selected configuration. Without a device an empty one is
        /// evaluated, so the table lists the checked metrics as pending.
        /// </summary>
        private async Task CheckAsync(SpecCheckRequest request, Action<SpecCheckResult> deliver)
            {
            _cts?.Cancel();
            var cts = _cts = new CancellationTokenSource();

            if (string.IsNullOrEmpty(request.Configuration))
                {
                RunOnUiThread(() => deliver(new SpecCheckResult.ConfigurationNeeded()));
                return;
                }

            var device = request.Device ?? new DeviceUnderTest(request.PreviewSerialNumber);
            device.DeviceConfiguration = request.Configuration;
            device.IsTV = request.IsTv;
            string deviceJson = _fileService.SerializeDevice(device);
            int measuredPoints = device.Measurements.Count;
            int totalPoints = _measurementStatusService.AllMeasurementButtonStatuses.Count - (device.IsTV ? 0 : 1);

            SpecEvaluationOutcome outcome;
            try
                {
                outcome = await _specEvaluationService.EvaluateAsync(deviceJson, cts.Token);
                }
            catch (OperationCanceledException)
                {
                return;
                }
            if (cts.IsCancellationRequested) return;

            RunOnUiThread(() =>
            {
                if (outcome.Evaluation != null)
                    {
                    _lastError = null;
                    deliver(new SpecCheckResult.Evaluated(device, outcome.Evaluation, measuredPoints, totalPoints));
                    return;
                    }

                string error = outcome.Error ?? "?";
                bool isNew = error != _lastError;
                _lastError = error;
                deliver(new SpecCheckResult.Unavailable(error, isNew));
            });
            }

        private void RunOnUiThread(Action action)
            {
            if (_dispatcher.CheckAccess()) action();
            else _dispatcher.BeginInvoke(action);
            }
        }
    }

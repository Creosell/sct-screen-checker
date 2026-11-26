using MvvmHelpers;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Drives <see cref="IColorMeasurementService"/> through the connection, the zero calibration and the confirmation
    /// of the active channel, and keeps the state the measurement page shows.
    /// </summary>
    public class AnalyzerSession : BaseViewModel, IAnalyzerSession
        {
        private readonly IColorMeasurementService _device;
        private readonly IAnalyzerRegistryService _registry;
        private readonly IDialogService _dialogService;
        private readonly IDispatcher _dispatcher;
        private bool _isConnected;
        private bool _isCalibrated;
        private bool _isChannelConfirmed;
        private bool _isConnecting;
        private bool _isCalibrating;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyzerSession"/> class.
        /// </summary>
        /// <param name="device">The color analyzer.</param>
        /// <param name="registry">Registry of the last confirmed channel per probe.</param>
        /// <param name="dialogService">Service asking the inspector which channel to use and reporting errors.</param>
        /// <param name="dispatcher">Dispatcher delivering the analyzer events on the UI thread.</param>
        public AnalyzerSession(IColorMeasurementService device, IAnalyzerRegistryService registry, IDialogService dialogService, IDispatcher dispatcher)
            {
            _device = device ?? throw new ArgumentNullException(nameof(device));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

            _device.StatusMessage += Device_StatusMessage;
            _device.ConnectionStatusChanged += Device_ConnectionStatusChanged;
            _device.CalibrationStatusChanged += Device_CalibrationStatusChanged;
            _device.CurrentChannelChanged += Device_CurrentChannelChanged;
            }

        /// <inheritdoc />
        public event EventHandler<string>? StatusMessage;

        /// <inheritdoc />
        public event EventHandler? StateRefreshed;

        /// <inheritdoc />
        public bool IsConnected
            {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
            }

        /// <inheritdoc />
        public bool IsCalibrated
            {
            get => _isCalibrated;
            set
                {
                if (SetProperty(ref _isCalibrated, value) && !value)
                    IsChannelConfirmed = false;
                }
            }

        /// <inheritdoc />
        public bool IsChannelConfirmed
            {
            get => _isChannelConfirmed;
            set => SetProperty(ref _isChannelConfirmed, value);
            }

        /// <inheritdoc />
        public bool IsConnecting => _isConnecting;

        /// <inheritdoc />
        public bool IsCalibrating => _isCalibrating;

        /// <inheritdoc />
        public ColorimeterInfo? Colorimeter { get; private set; }

        /// <inheritdoc />
        public ColorAnalyzerModel Model => _device.Model;

        /// <inheritdoc />
        public int CurrentChannel => _device.CurrentChannel;

        /// <inheritdoc />
        public string ChannelName => _device.ChannelName;

        /// <inheritdoc />
        public Task<Measurement> MeasureAsync(int measurementTime) => _device.MeasureAsync(measurementTime);

        /// <inheritdoc />
        public async Task CalibrateAsync(Action<CalibrationOutcome> report)
            {
            try
                {
                // Use return value directly to avoid race condition with event-based property updates
                bool isConnected = _device.IsDeviceConnected;
                if (!isConnected)
                    isConnected = await ConnectAsync();

                if (isConnected)
                    {
                    _isCalibrating = true;
                    if (!await _device.CalibrateZeroAsync())
                        {
                        Disconnect();
                        report(new CalibrationOutcome.CalibrationFailed());
                        return;
                        }
                    if (!ConfirmActiveChannel())
                        {
                        Disconnect();
                        report(new CalibrationOutcome.ChannelNotConfirmed());
                        return;
                        }
                    report(new CalibrationOutcome.Ready());
                    }
                else
                    {
                    report(new CalibrationOutcome.ConnectFailed());
                    }
                }
            catch
                {
                // Force disconnect on error to cleanup COM objects
                if (_device.IsDeviceConnected)
                    Disconnect();
                report(new CalibrationOutcome.Faulted());
                }
            finally
                {
                _isCalibrating = false;
                StateRefreshed?.Invoke(this, EventArgs.Empty);
                }
            }

        /// <summary>
        /// Attempts to establish a connection with the analyzer; a failed attempt disconnects it.
        /// </summary>
        /// <returns>True if the connection was successful, false otherwise.</returns>
        private async Task<bool> ConnectAsync()
            {
            if (IsConnected || _isCalibrating || _isConnecting) return false;

            try
                {
                _isConnecting = true;
                bool connected = await _device.ConnectAsync();
                if (!connected)
                    Disconnect();
                return connected;
                }
            catch
                {
                Disconnect();
                return false;
                }
            finally
                {
                _isConnecting = false;
                }
            }

        /// <summary>
        /// Disconnects the analyzer regardless of the state, for error recovery; a failure is reported to the inspector.
        /// </summary>
        private void Disconnect()
            {
            try
                {
                _device.Disconnect();
                }
            catch (Exception ex)
                {
                _dialogService.ShowMessage($"{ErrUnexpected}: {ex.Message}", $"{Err}");
                }
            StateRefreshed?.Invoke(this, EventArgs.Empty);
            }

        /// <summary>
        /// Reconciles the channel currently set on the probe with the last channel registered for it.
        /// On mismatch the inspector picks one of the two; the confirmed channel is stored in the registry and in the settings.
        /// </summary>
        /// <returns>True when a channel was confirmed; false when the inspector dismissed the dialog.</returns>
        private bool ConfirmActiveChannel()
            {
            string probeSN = _device.ProbeSN;
            int channel = _device.CurrentChannel;

            if (_registry.GetLastChannel(probeSN) is int lastChannel && lastChannel != channel)
                {
                int? choice = _dialogService.ShowChoice(
                    string.Format(ChannelMismatchMessage, lastChannel, channel),
                    ChannelMismatchTitle,
                    string.Format(UseProbeChannel, channel),
                    string.Format(UseLastChannel, lastChannel));

                if (choice is null) return false;

                if (choice == 1)
                    {
                    _device.CurrentChannel = lastChannel;
                    channel = lastChannel;
                    }
                }

            _registry.Update(probeSN, _device.Model, channel);
            Colorimeter = ReadColorimeter(probeSN, channel);
            IsChannelConfirmed = true;
            return true;
            }

        /// <summary>
        /// Describes the connected analyzer with the channel and the user-defined channel name read from it now.
        /// </summary>
        /// <param name="probeSN">Serial number of the probe.</param>
        /// <param name="channel">Active channel number.</param>
        private ColorimeterInfo ReadColorimeter(string probeSN, int channel)
            {
            string channelName = _device.ChannelName;
            return new ColorimeterInfo(
                _device.Model.ToDisplayName(),
                probeSN,
                channel,
                string.IsNullOrWhiteSpace(channelName) ? null : channelName);
            }

        private void Device_StatusMessage(object? sender, string message) => StatusMessage?.Invoke(this, message);

        private void Device_ConnectionStatusChanged(object? sender, bool isConnected) =>
            RunOnUiThread(() => { IsConnected = isConnected; StateRefreshed?.Invoke(this, EventArgs.Empty); });

        private void Device_CalibrationStatusChanged(object? sender, bool isCalibrated) =>
            RunOnUiThread(() => { IsCalibrated = isCalibrated; StateRefreshed?.Invoke(this, EventArgs.Empty); });

        /// <summary>
        /// Re-reads the channel and its name for the report header when the channel of a confirmed analyzer changes (for example in the settings).
        /// </summary>
        private void Device_CurrentChannelChanged(object? sender, int channel) =>
            RunOnUiThread(() =>
                {
                if (IsChannelConfirmed && Colorimeter != null)
                    Colorimeter = ReadColorimeter(Colorimeter.ProbeSN, channel);
                OnPropertyChanged(nameof(CurrentChannel));
                });

        private void RunOnUiThread(Action action)
            {
            if (_dispatcher.CheckAccess()) action();
            else _dispatcher.BeginInvoke(action);
            }

        /// <summary>
        /// Stops listening to the analyzer, disposes it and forgets the connection state.
        /// </summary>
        public void Dispose()
            {
            _device.StatusMessage -= Device_StatusMessage;
            _device.ConnectionStatusChanged -= Device_ConnectionStatusChanged;
            _device.CalibrationStatusChanged -= Device_CalibrationStatusChanged;
            _device.CurrentChannelChanged -= Device_CurrentChannelChanged;
            _device.Dispose();
            _isConnected = false;
            _isCalibrated = false;
            GC.SuppressFinalize(this);
            }
        }
    }

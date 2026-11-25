using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.CompilerServices;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Wrappers;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Service for handling communications and measurements with Color Analyzer device (CA-410 via CA-SDK2, CA-200/CA-310 via legacy CA-SDK COM).
    /// </summary>
    public class ColorMeasurementService : IDisposable, IColorMeasurementService
        {
        private IColorAnalyzer200? _objCa200;
        private readonly IReadOnlyList<Func<IColorAnalyzer200>> _analyzerFactories = [];
        private readonly ISettingsService? _settingsService;
        private IColorAnalyzer? _objCa = null;
        private IColorAnalyzerMemory? _objMemory = null;
        private bool _isDeviceConnected = false;
        private bool _isDeviceCalibrated = false;
        private int _channel = 0;
        private string _probeSN = "Unknown";

        private const double DefaultDisplayRange = 2.5;
        private const int LvxyDisplayMode = 0;

        /// <summary>
        /// Occurs when device connection status changes.
        /// </summary>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <summary>
        /// Occurs when device calibration status changes.
        /// </summary>
        public event EventHandler<bool>? CalibrationStatusChanged;

        /// <summary>
        /// Occurs when status message needs to be reported.
        /// </summary>
        public event EventHandler<string>? StatusMessage;

        /// <summary>
        /// Occurs when measurement progress updates.
        /// </summary>
        public event EventHandler<double>? MeasurementProgress;

        /// <summary>
        /// Occurs when current channel changes.
        /// </summary>
        public event EventHandler<int>? CurrentChannelChanged;

        /// <summary>
        /// Gets whether device is currently connected.
        /// </summary>
        public bool IsDeviceConnected => _isDeviceConnected;

        /// <summary>
        /// Gets whether device is calibrated.
        /// </summary>
        public bool IsDeviceCalibrated => _isDeviceCalibrated;

        /// <summary>
        /// Gets the detected analyzer family, or <see cref="ColorAnalyzerModel.Unknown"/> when not connected.
        /// </summary>
        public ColorAnalyzerModel Model => _isDeviceConnected ? _objCa200?.Model ?? ColorAnalyzerModel.Unknown : ColorAnalyzerModel.Unknown;

        /// <summary>
        /// Gets the probe serial number.
        /// </summary>
        public string ProbeSN
            {
            get
                {
                if (_objCa != null)
                    {
                    _probeSN = _objCa.SingleProbe.SerialNO;
                    }
                return _probeSN;
                }
            }

        /// <summary>
        /// Gets the user-defined name of the current analyzer channel, or an empty string when none is set or it cannot be read.
        /// </summary>
        public string ChannelName
            {
            get
                {
                try
                    {
                    return _objMemory?.ChannelID ?? string.Empty;
                    }
                catch (Exception)
                    {
                    return string.Empty;
                    }
                }
            }

        /// <summary>
        /// Gets or sets the current measurement channel (0-99).
        /// </summary>
        public int CurrentChannel
            {
            get => _channel;
            set { ChangeChannel(value); }
            }

        /// <summary>
        /// Initializes a new instance of ColorMeasurementService.
        /// </summary>
        /// <param name="settingsService">Source of the measurement mode settings; defaults are used when omitted.</param>
        public ColorMeasurementService(ISettingsService? settingsService = null)
            {
            _settingsService = settingsService;
            _analyzerFactories =
                [
                CreateCa410Analyzer,
                () => new Ca310AnalyzerWrapper()
                ];
            }

        /// <summary>
        /// Creates the CA-410 driver once the CA-SDK2 libraries are located. Kept out of the lambda so the SDK types
        /// are not loaded before the libraries are registered.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown when CA-SDK2 is not installed.</exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IColorAnalyzer200 CreateCa410Analyzer() =>
            Ca410SdkLoader.TryInitialize()
                ? new Ca410AnalyzerWrapper()
                : throw new FileNotFoundException("CA-SDK2 is not installed (CA-S40 setup)");

        /// <summary>
        /// Initializes a new instance of ColorMeasurementService with dependency injection for testing.
        /// </summary>
        /// <param name="ca200Wrapper">Color analyzer wrapper for testing.</param>
        /// <param name="settingsService">Source of the measurement mode settings; defaults are used when omitted.</param>
        internal ColorMeasurementService(IColorAnalyzer200 ca200Wrapper, ISettingsService? settingsService = null)
            {
            _settingsService = settingsService;
            _objCa200 = ca200Wrapper ?? throw new ArgumentNullException(nameof(ca200Wrapper));
            }

        /// <summary>
        /// Asynchronously attempts to connect to color analyzer device.
        /// </summary>
        /// <returns>True if connection succeeded, false otherwise.</returns>
        public async Task<bool> ConnectAsync()
            {
            await Task.Run(() =>
            {
                try
                    {
                    if (!_isDeviceConnected)
                        {
                        StatusMessage?.Invoke(this, ConnectingCA);

                        OpenAnalyzer();
                        _objCa = _objCa200!.SingleCa;
                        _objMemory = _objCa.Memory;

                        _isDeviceConnected = true;
                        ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                        StatusMessage?.Invoke(this, ConnectedCA);
                        }
                    }
                catch (COMException ex)
                    {
                    StatusMessage?.Invoke(this, $"{ConnectionError}: {Describe(ex)}");
                    _isDeviceConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                    }
                catch (Exception ex)
                    {
                    StatusMessage?.Invoke(this, $"{ConnectionError}: {Describe(ex)}");
                    _isDeviceConnected = false;
                    ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                    }
            });
            return _isDeviceConnected;
            }

        /// <summary>
        /// Connects to the first available analyzer driver. An injected driver is reused as is;
        /// otherwise every registered driver is probed in order and the failed ones are released.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no driver could connect.</exception>
        private void OpenAnalyzer()
            {
            if (_objCa200 != null)
                {
                _objCa200.AutoConnect();
                return;
                }

            var errors = new List<string>();
            foreach (var factory in _analyzerFactories)
                {
                IColorAnalyzer200? candidate = null;
                try
                    {
                    candidate = factory();
                    candidate.AutoConnect();
                    _objCa200 = candidate;
                    return;
                    }
                catch (Exception ex)
                    {
                    errors.Add(AnalyzerErrorMessages.Describe(ex, candidate?.Model ?? ColorAnalyzerModel.Unknown));
                    candidate?.Dispose();
                    }
                }

            throw new InvalidOperationException(string.Join(" | ", errors));
            }

        /// <summary>
        /// Describes a failed analyzer call for the status message: the localized text with the model and the code when the SDK gave a code,
        /// otherwise the message of the exception.
        /// </summary>
        /// <param name="exception">Exception thrown by the analyzer call.</param>
        private string Describe(Exception exception) =>
            AnalyzerErrorMessages.Describe(exception, _objCa200?.Model ?? ColorAnalyzerModel.Unknown);

        /// <summary>
        /// Returns the sync setting from the settings when the connected analyzer supports it;
        /// otherwise reports it and falls back to the universal synchronization.
        /// </summary>
        private SyncSetting ResolveSync(AppSettings settings)
            {
            var sync = settings.ToSyncSetting();
            if (AnalyzerCapabilities.IsSupported(Model, sync)) return sync;

            StatusMessage?.Invoke(this, string.Format(SyncSettingNotSupported, Model.ToDisplayName()));
            return new SyncSetting(SyncMode.Universal);
            }

        /// <summary>
        /// Disconnects from device by disposing resources.
        /// </summary>
        private void Disconnect() => Dispose(true);

        /// <summary>
        /// Asynchronously performs zero calibration and configures device settings.
        /// A device that was already calibrated is reported as uncalibrated for the duration of the new calibration.
        /// </summary>
        /// <returns>True if calibration succeeded, false otherwise.</returns>
        public async Task<bool> CalibrateZeroAsync()
            {
            var success = false;
            await Task.Run(() =>
            {
                try
                    {
                    if (_isDeviceCalibrated)
                        {
                        _isDeviceCalibrated = false;
                        CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                        }

                    StatusMessage?.Invoke(this, CalibratingZeroCA);
                    if (_objCa != null)
                        {
                        _objCa.CalZero();

                        var settings = _settingsService?.CurrentSettings ?? new AppSettings();
                        _objCa.Sync = ResolveSync(settings);
                        _objCa.AveragingMode = (int)settings.MeasurementSpeed;
                        _objCa.SetAnalogRange(
                            Convert.ToSingle(DefaultDisplayRange),
                            Convert.ToSingle(DefaultDisplayRange)
                        );
                        _objCa.DisplayMode = LvxyDisplayMode;

                        if (_objMemory != null)
                            {
                            _channel = _objMemory.ChannelNO;
                            CurrentChannelChanged?.Invoke(this, _channel);
                            }
                        }

                    _isDeviceCalibrated = true;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                    StatusMessage?.Invoke(this, ZeroCalibratedCA);
                    success = true;
                    }
                catch (COMException ex)
                    {
                    StatusMessage?.Invoke(this, $"{CheckConnectionCA}: {Describe(ex)}");
                    _isDeviceCalibrated = false;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                    }
                catch (Exception ex)
                    {
                    StatusMessage?.Invoke(this, $"{ErrAtCalibration}: {Describe(ex)}");
                    _isDeviceCalibrated = false;
                    CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                    }
            });
            return success;
            }

        /// <summary>
        /// Updates current channel and synchronizes with connected device memory if available.
        /// Channel value is stored regardless of device connection state.
        /// </summary>
        /// <param name="channel">Target channel identifier to set (0-99).</param>
        private void ChangeChannel(int channel)
            {
            if (!IsValidChannel(channel))
                return;

            _channel = channel;
            CurrentChannelChanged?.Invoke(this, _channel);

            if (!_isDeviceConnected || _objMemory == null)
                return;

            try
                {
                _objMemory.ChannelNO = channel;
                StatusMessage?.Invoke(this, string.Format(ChannelChanged, channel));
                }
            catch (Exception ex)
                {
                StatusMessage?.Invoke(this, string.Format(ErrAtChangeChannel, Describe(ex)));
                }
            }

        /// <summary>
        /// Validates if channel number is within acceptable range.
        /// </summary>
        /// <param name="channel">Channel number to validate.</param>
        /// <returns>True if channel is valid, false otherwise.</returns>
        private bool IsValidChannel(int channel) => channel >= AppConstants.ColorAnalyzer.MinChannel && channel <= AppConstants.ColorAnalyzer.MaxChannel;

        /// <summary>
        /// Asynchronously performs series of measurements and calculates average values of the successful ones;
        /// an iteration that failed is reported and left out of the average.
        /// </summary>
        /// <param name="measurementTime">Number of measurements to perform.</param>
        /// <returns>Measurement object containing averaged results; invalid when no iteration succeeded.</returns>
        public async Task<Measurement> MeasureAsync(int measurementTime)
            {
            var result = new Measurement();
            await Task.Run(() =>
            {
                try
                    {
                    if (!_isDeviceConnected)
                        {
                        StatusMessage?.Invoke(this, MeasureWihoutConnectionError);
                        result.IsValid = false;
                        return;
                        }
                    if (!_isDeviceCalibrated)
                        {
                        StatusMessage?.Invoke(this, MakeZeroCalibration);
                        result.IsValid = false;
                        return;
                        }

                    StatusMessage?.Invoke(this, Measuring);

                    var xValues = new List<double>(measurementTime);
                    var yValues = new List<double>(measurementTime);
                    var LvValues = new List<double>(measurementTime);
                    var temperatures = new List<double>(measurementTime);

                    for (var i = 0; i < measurementTime; i++)
                        {
                        try
                            {
                            if (_objCa200 != null)
                                {
                                _objCa200.SingleCa.Measure();
                                var probe = _objCa200.SingleCa.SingleProbe;
                                xValues.Add(probe.sx);
                                yValues.Add(probe.sy);
                                LvValues.Add(probe.Lv);
                                temperatures.Add(probe.T);
                                }
                            MeasurementProgress?.Invoke(this, (double)( i + 1 ) / measurementTime * 100);
                            }
                        catch (COMException measureEx)
                            {
                            StatusMessage?.Invoke(this, $"{ErrorAtMeasuringIteration}{i}: {Describe(measureEx)}");
                            continue;
                            }
                        catch (Exception measureEx)
                            {
                            StatusMessage?.Invoke(this, $"{ErrorAtMeasuringIteration}{i}: {Describe(measureEx)}");
                            continue;
                            }

                        if (i < measurementTime - 1)
                            {
                            Thread.Sleep(1000);
                            }
                        }

                    if (xValues.Count == 0)
                        {
                        result.IsValid = false;
                        return;
                        }

                    result.x = xValues.Average();
                    result.y = yValues.Average();
                    result.Lv = LvValues.Average();
                    result.T = AverageTemperature(temperatures);
                    result.IsValid = true;
                    }
                catch (Exception ex)
                    {
                    StatusMessage?.Invoke(this, $"{ErrUnexpected}: {ex.Message}");
                    result.IsValid = false;
                    }
            });

            return result;
            }

        /// <summary>
        /// Averages the color temperature samples of a measurement. A color temperature is never negative or zero: an analyzer reports
        /// such a value (CA-310) or infinity (CA-410) when the color has none, so a measurement with a sample like that has no color
        /// temperature at all and gets infinity, which the table shows as "n/a" and the saved file as null.
        /// </summary>
        /// <param name="samples">The temperatures of the successful iterations.</param>
        /// <returns>The average, or <see cref="double.PositiveInfinity"/> when the temperature is undefined.</returns>
        private static double AverageTemperature(IReadOnlyCollection<double> samples) =>
            samples.Count == 0 || samples.Any(sample => !double.IsFinite(sample) || sample <= 0) ? double.PositiveInfinity : samples.Average();

        /// <summary>
        /// Releases all resources used by ColorMeasurementService.
        /// </summary>
        void IDisposable.Dispose()
            {
            Dispose(true);
            GC.SuppressFinalize(this);
            }

        /// <summary>
        /// Releases unmanaged and managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources.</param>
        public virtual void Dispose(bool disposing)
            {
            if (disposing)
                {

                if (_objCa200 != null)
                    {
                    _objCa200.Dispose();
                    _objCa200 = null;
                    }

                _isDeviceConnected = false;
                ConnectionStatusChanged?.Invoke(this, _isDeviceConnected);
                _isDeviceCalibrated = false;
                CalibrationStatusChanged?.Invoke(this, _isDeviceCalibrated);
                StatusMessage?.Invoke(this, DisconnectedCA);
                }
            }

        /// <summary>
        /// Finalizer for ColorMeasurementService.
        /// </summary>
        ~ColorMeasurementService() => Dispose(false);

        /// <summary>
        /// Disconnects from color analyzer device.
        /// </summary>
        void IColorMeasurementService.Disconnect() => Disconnect();
        }
    }
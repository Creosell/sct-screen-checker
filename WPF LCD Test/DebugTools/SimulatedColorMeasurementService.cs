#if DEBUG
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.DebugTools
    {
    /// <summary>
    /// A color analyzer that exists only in a Debug build started with <c>--simulate</c>. It connects, calibrates and measures with the
    /// delays, statuses and events of the real analyzer, so the real interface can be driven without a device: the buttons of the points,
    /// the statuses, the specification panel, repeated measurements, saving and the report all work on its values.
    /// </summary>
    public sealed class SimulatedColorMeasurementService : IColorMeasurementService
        {
        private const int ConnectDelayMs = 700;
        private const int CalibrationDelayMs = 1600;
        private const int MeasurementStepCount = 10;

        /// <summary>The values of a TV of a real batch: x, y and luminance of every point of the measurement sequence.</summary>
        private static readonly Dictionary<string, (double X, double Y, double Lv)> Baseline = new()
            {
                ["TopLeft"] = (0.2764, 0.2802, 279.74),
                ["TopCenter"] = (0.2769, 0.2806, 313.21),
                ["TopRight"] = (0.2761, 0.2799, 266.51),
                ["MiddleLeft"] = (0.2766, 0.2804, 321.14),
                ["Center"] = (0.2768, 0.2805, 341.34),
                ["MiddleRight"] = (0.2767, 0.2803, 318.63),
                ["BottomLeft"] = (0.2763, 0.2801, 296.76),
                ["BottomCenter"] = (0.2768, 0.2806, 317.13),
                ["BottomRight"] = (0.2762, 0.2800, 274.47),
                ["WhiteColor"] = (0.2766, 0.2802, 363.78),
                ["RedColor"] = (0.6647, 0.3164, 75.2),
                ["GreenColor"] = (0.2702, 0.6398, 238.4),
                ["BlueColor"] = (0.1516, 0.0548, 27.1),
                ["BlackColor"] = (0.2760, 0.2800, 0.0503)
            };

        private readonly SimulationOptions _options;
        private readonly IMeasurementStatusService _statusService;
        private readonly Random _random = new(32);
        private readonly Dictionary<string, int> _attempts = [];
        private int _channel = 2;
        private bool _isConnected;
        private bool _isCalibrated;

        /// <inheritdoc/>
        public bool IsDeviceConnected => _isConnected;

        /// <inheritdoc/>
        public bool IsDeviceCalibrated => _isCalibrated;

        /// <inheritdoc/>
        public ColorAnalyzerModel Model => _isConnected ? _options.Model : ColorAnalyzerModel.Unknown;

        /// <inheritdoc/>
        public string ProbeSN => _options.ProbeSerialNumber;

        /// <inheritdoc/>
        public string ChannelName => "Simulated";

        /// <inheritdoc/>
        public int CurrentChannel
            {
            get => _channel;
            set
                {
                _channel = value;
                CurrentChannelChanged?.Invoke(this, _channel);
                }
            }

        /// <inheritdoc/>
        public event EventHandler<bool>? ConnectionStatusChanged;

        /// <inheritdoc/>
        public event EventHandler<bool>? CalibrationStatusChanged;

        /// <inheritdoc/>
        public event EventHandler<string>? StatusMessage;

        /// <inheritdoc/>
        public event EventHandler<double>? MeasurementProgress;

        /// <inheritdoc/>
        public event EventHandler<int>? CurrentChannelChanged;

        /// <summary>
        /// Creates the simulated analyzer.
        /// </summary>
        /// <param name="options">Model, scenario and probe serial number of the simulation.</param>
        /// <param name="statusService">Statuses of the measurement buttons; the one that is being measured tells which point to simulate.</param>
        public SimulatedColorMeasurementService(SimulationOptions options, IMeasurementStatusService statusService)
            {
            _options = options;
            _statusService = statusService;
            }

        /// <inheritdoc/>
        public async Task<bool> ConnectAsync()
            {
            StatusMessage?.Invoke(this, ConnectingCA);
            await Task.Delay(ConnectDelayMs);
            _isConnected = true;
            ConnectionStatusChanged?.Invoke(this, true);
            StatusMessage?.Invoke(this, ConnectedCA);
            return true;
            }

        /// <inheritdoc/>
        public void Disconnect()
            {
            _isConnected = false;
            _isCalibrated = false;
            ConnectionStatusChanged?.Invoke(this, false);
            CalibrationStatusChanged?.Invoke(this, false);
            StatusMessage?.Invoke(this, DisconnectedCA);
            }

        /// <inheritdoc/>
        public async Task<bool> CalibrateZeroAsync()
            {
            StatusMessage?.Invoke(this, CalibratingZeroCA);
            await Task.Delay(CalibrationDelayMs);
            _isCalibrated = true;
            CurrentChannelChanged?.Invoke(this, _channel);
            CalibrationStatusChanged?.Invoke(this, true);
            StatusMessage?.Invoke(this, ZeroCalibratedCA);
            return true;
            }

        /// <inheritdoc/>
        public async Task<Measurement> MeasureAsync(int measurmentTime)
            {
            if (!_isConnected)
                {
                StatusMessage?.Invoke(this, MeasureWihoutConnectionError);
                return new Measurement { IsValid = false };
                }

            if (!_isCalibrated)
                {
                StatusMessage?.Invoke(this, MakeZeroCalibration);
                return new Measurement { IsValid = false };
                }

            StatusMessage?.Invoke(this, Measuring);
            int stepDelay = Math.Clamp(measurmentTime * 250, 400, 2500) / MeasurementStepCount;
            for (int step = 1; step <= MeasurementStepCount; step++)
                {
                await Task.Delay(stepDelay);
                MeasurementProgress?.Invoke(this, 100.0 * step / MeasurementStepCount);
                }

            return Simulate(CurrentLocation());
            }

        /// <inheritdoc/>
        public void Dispose()
            {
            }

        /// <summary>
        /// Finds the point being measured: the measurement button the interface has just put into the measuring state.
        /// </summary>
        /// <returns>The identifier of the point; the white point when no button is measuring.</returns>
        private string CurrentLocation() =>
            _statusService.AllMeasurementButtonStatuses.FirstOrDefault(s => s.IsPassed == null && s.MeasuredValuesString == Measuring)?.Location
            ?? nameof(MeasurementLocation.WhiteColor);

        /// <summary>
        /// Makes one measurement of a point: the values of the batch, bent by the scenario and the number of the attempt, with a little noise.
        /// </summary>
        /// <param name="location">Identifier of the measured point.</param>
        /// <returns>The measurement.</returns>
        private Measurement Simulate(string location)
            {
            var (x, y, lv) = Baseline.GetValueOrDefault(location, Baseline[nameof(MeasurementLocation.WhiteColor)]);
            int attempt = _attempts.GetValueOrDefault(location);
            _attempts[location] = attempt + 1;

            var (lvFactor, drift) = Bend(location, attempt);
            x += drift + Noise(0.0008);
            y += drift / 2 + Noise(0.0008);
            lv *= lvFactor * (1 + Noise(0.006));

            bool isBlack = location == nameof(MeasurementLocation.BlackColor);
            return new Measurement
                {
                Location = location,
                x = Math.Round(x, 4),
                y = Math.Round(y, 4),
                Lv = Math.Round(lv, isBlack ? 4 : 2),
                T = isBlack ? double.PositiveInfinity : Math.Round(ColorTemperature(x, y)),
                IsValid = true
                };
            }

        /// <summary>
        /// The deviation the scenario gives to a point at an attempt: a factor of the luminance and a shift of the coordinates.
        /// </summary>
        /// <param name="location">Identifier of the measured point.</param>
        /// <param name="attempt">Zero-based number of the attempt at this point.</param>
        /// <returns>The luminance factor and the coordinate shift (1 and 0 for a point that is not bent).</returns>
        private (double LvFactor, double Drift) Bend(string location, int attempt)
            {
            int step = Math.Min(attempt, 2);
            if (_options.Scenario == SimulationScenario.Reject && attempt == 0)
                return (location == nameof(MeasurementLocation.BlackColor) ? 240 : 0.003, 0);  // luminance the validation refuses: too high for black, too low for the rest

            return (_options.Scenario, location) switch
                {
                    (SimulationScenario.Retry, nameof(MeasurementLocation.WhiteColor)) => (new[] { 0.86, 0.93, 1.0 }[step], 0),
                    (SimulationScenario.Retry, nameof(MeasurementLocation.BlackColor)) => (new[] { 1.45, 1.12, 1.0 }[step], 0),
                    (SimulationScenario.Retry, nameof(MeasurementLocation.RedColor)) => (1, new[] { 0.025, 0.012, 0.0 }[step]),
                    (SimulationScenario.Uniformity, nameof(MeasurementLocation.TopRight)) => (new[] { 0.82, 0.857, 1.0 }[step], 0),
                    (SimulationScenario.Fail, nameof(MeasurementLocation.WhiteColor)) => (0.84, 0),
                    (SimulationScenario.Fail, nameof(MeasurementLocation.BlackColor)) => (2.2, 0),
                    (SimulationScenario.Fail, nameof(MeasurementLocation.RedColor)) => (1, 0.03),
                    _ => (1, 0)
                    };
            }

        /// <summary>
        /// Gives a random deviation of the given amplitude.
        /// </summary>
        /// <param name="amplitude">Largest deviation up or down.</param>
        /// <returns>A deviation between minus and plus the amplitude.</returns>
        private double Noise(double amplitude) => (_random.NextDouble() * 2 - 1) * amplitude;

        /// <summary>
        /// Estimates the correlated color temperature from the chromaticity coordinates (McCamy's formula).
        /// </summary>
        /// <param name="x">CIE 1931 x.</param>
        /// <param name="y">CIE 1931 y.</param>
        /// <returns>Temperature in kelvins.</returns>
        private static double ColorTemperature(double x, double y)
            {
            double n = (x - 0.3320) / (0.1858 - y);
            return 449 * n * n * n + 3525 * n * n + 6823.3 * n + 5520.33;
            }
        }
    }
#endif

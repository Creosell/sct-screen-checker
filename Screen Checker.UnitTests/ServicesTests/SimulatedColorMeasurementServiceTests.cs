#if DEBUG
using System.Collections.ObjectModel;
using Moq;
using ScreenChecker.DebugTools;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.UnitTests.ServicesTests
    {
    [TestFixture]
    [Category("Simulator")]
    public class SimulatedColorMeasurementServiceTests
        {
        private ObservableCollection<MeasurementStatus> _statuses = null!;

        private SimulatedColorMeasurementService CreateService(SimulationScenario scenario = SimulationScenario.Ok, ColorAnalyzerModel model = ColorAnalyzerModel.CA410)
            {
            _statuses = new ObservableCollection<MeasurementStatus>(Enum.GetNames<MeasurementLocation>().Select(l => new MeasurementStatus(l)));
            var statusService = new Mock<IMeasurementStatusService>();
            statusService.SetupGet(s => s.AllMeasurementButtonStatuses).Returns(_statuses);
            return new SimulatedColorMeasurementService(new SimulationOptions(model, scenario, "12345678"), statusService.Object);
            }

        /// <summary>Puts a point into the measuring state, the way the measurement view model does before it asks the analyzer.</summary>
        private void StartMeasuring(MeasurementLocation location)
            {
            var status = _statuses.First(s => s.Location == location.ToString());
            status.IsPassed = null;
            status.MeasuredValuesString = Measuring;
            }

        private async Task<SimulatedColorMeasurementService> CalibratedServiceAsync(SimulationScenario scenario = SimulationScenario.Ok)
            {
            var service = CreateService(scenario);
            await service.ConnectAsync();
            await service.CalibrateZeroAsync();
            return service;
            }

        [Test]
        public void Parse_WithoutTheFlag_ReturnsNull()
            {
            Assert.That(SimulationOptions.Parse(["--other"]), Is.Null);
            }

        [Test]
        public void Parse_TheFlagAlone_SimulatesACa410WithTheOkScenario()
            {
            var options = SimulationOptions.Parse(["--simulate"])!;

            Assert.That((options.Model, options.Scenario), Is.EqualTo((ColorAnalyzerModel.CA410, SimulationScenario.Ok)));
            }

        [Test]
        public void Parse_ModelScenarioAndProbe_AreRead()
            {
            var options = SimulationOptions.Parse(["--simulate=ca310", "--scenario=retry", "--probe-sn=123"])!;

            Assert.That((options.Model, options.Scenario, options.ProbeSerialNumber), Is.EqualTo((ColorAnalyzerModel.CA310, SimulationScenario.Retry, "123")));
            }

        [Test]
        public async Task ConnectAndCalibrate_RaiseTheEventsOfTheRealAnalyzerInOrder()
            {
            var service = CreateService();
            var events = new List<string>();
            service.ConnectionStatusChanged += (_, connected) => events.Add($"connection {connected}");
            service.CalibrationStatusChanged += (_, calibrated) => events.Add($"calibration {calibrated}");
            Assume.That(service.Model, Is.EqualTo(ColorAnalyzerModel.Unknown));

            await service.ConnectAsync();
            await service.CalibrateZeroAsync();

            Assert.That(events, Is.EqualTo(new[] { "connection True", "calibration True" }));
            Assert.That((service.IsDeviceConnected, service.IsDeviceCalibrated, service.Model), Is.EqualTo((true, true, ColorAnalyzerModel.CA410)));
            }

        [Test]
        public async Task Measure_BeforeTheCalibration_IsInvalid()
            {
            var service = CreateService();
            await service.ConnectAsync();

            var measurement = await service.MeasureAsync(1);

            Assert.That(measurement.IsValid, Is.False);
            }

        [Test]
        public async Task Measure_TakesThePointThatIsBeingMeasured()
            {
            var service = await CalibratedServiceAsync();
            StartMeasuring(MeasurementLocation.BlackColor);

            var measurement = await service.MeasureAsync(1);

            Assert.That(measurement.Location, Is.EqualTo(nameof(MeasurementLocation.BlackColor)));
            Assert.That(measurement.Lv, Is.LessThan(0.1));  // a black point, not the white one
            Assert.That(double.IsInfinity(measurement.T), Is.True);  // black has no temperature
            }

        [Test]
        public async Task RetryScenario_GetsTheWhitePointBetterWithEveryAttempt()
            {
            var service = await CalibratedServiceAsync(SimulationScenario.Retry);
            var values = new List<double>();

            for (int attempt = 0; attempt < 3; attempt++)
                {
                StartMeasuring(MeasurementLocation.WhiteColor);
                values.Add((await service.MeasureAsync(1)).Lv);
                }

            Assert.That(values, Is.Ordered.Ascending);
            Assert.That(values[0], Is.LessThan(320));  // below the min of the CH32 TVs
            Assert.That(values[2], Is.GreaterThan(350));
            }

        [Test]
        public async Task RejectScenario_RefusesTheFirstAttemptAndGivesAGoodRepeat()
            {
            var service = await CalibratedServiceAsync(SimulationScenario.Reject);
            StartMeasuring(MeasurementLocation.Center);
            var first = await service.MeasureAsync(1);
            StartMeasuring(MeasurementLocation.Center);
            var repeat = await service.MeasureAsync(1);

            Assert.That(first.Lv, Is.LessThan(5));  // the validation of the measurement view model refuses it
            Assert.That(repeat.Lv, Is.GreaterThan(100));
            }

        [Test]
        public async Task RejectScenario_AfterThePointIsReset_RefusesTheFirstAttemptAgain()
            {
            var service = await CalibratedServiceAsync(SimulationScenario.Reject);
            StartMeasuring(MeasurementLocation.Center);
            await service.MeasureAsync(1);
            StartMeasuring(MeasurementLocation.Center);
            await service.MeasureAsync(1);

            var status = _statuses.First(s => s.Location == nameof(MeasurementLocation.Center));
            status.IsPassed = null;
            status.MeasuredValuesString = "";
            StartMeasuring(MeasurementLocation.Center);
            var afterReset = await service.MeasureAsync(1);

            Assert.That(afterReset.Lv, Is.LessThan(5));
            }

        [Test]
        public async Task UniformityScenario_MakesTheTopRightPointBrighterWithEveryAttempt()
            {
            var service = await CalibratedServiceAsync(SimulationScenario.Uniformity);
            var values = new List<double>();

            for (int attempt = 0; attempt < 3; attempt++)
                {
                StartMeasuring(MeasurementLocation.TopRight);
                values.Add((await service.MeasureAsync(1)).Lv);
                }

            Assert.That(values, Is.Ordered.Ascending);
            Assert.That(values[0] / 341.0, Is.LessThan(0.65));  // below the minimum uniformity of the CH32 TVs against the center
            Assert.That(values[2] / 341.0, Is.GreaterThan(0.75));
            }

        [Test]
        public async Task Disconnect_ResetsTheState()
            {
            var service = await CalibratedServiceAsync();

            service.Disconnect();

            Assert.That((service.IsDeviceConnected, service.IsDeviceCalibrated), Is.EqualTo((false, false)));
            }
        }
    }
#endif

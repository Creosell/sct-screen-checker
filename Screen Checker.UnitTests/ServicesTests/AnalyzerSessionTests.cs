using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class AnalyzerSessionTests
    {
        private Mock<IColorMeasurementService> _device;
        private Mock<IAnalyzerRegistryService> _registry;
        private Mock<IDialogService> _dialog;
        private Mock<ISettingsService> _settings;
        private Mock<IDispatcher> _dispatcher;
        private AnalyzerSession _session;
        private List<CalibrationOutcome> _outcomes;

        [SetUp]
        public void SetUp()
        {
            _device = new Mock<IColorMeasurementService>();
            _device.Setup(d => d.ProbeSN).Returns("PROBE1");
            _device.Setup(d => d.CurrentChannel).Returns(2);
            _device.Setup(d => d.ChannelName).Returns("Lab");
            _device.Setup(d => d.Model).Returns(ColorAnalyzerModel.CA410);
            _registry = new Mock<IAnalyzerRegistryService>();
            _dialog = new Mock<IDialogService>();
            _settings = new Mock<ISettingsService>();
            _dispatcher = new Mock<IDispatcher>();
            _dispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _session = new AnalyzerSession(_device.Object, _registry.Object, _dialog.Object, _settings.Object, _dispatcher.Object);
            _outcomes = [];
        }

        [TearDown]
        public void TearDown() => _session.Dispose();

        private void GivenConnected(bool connectsWhenAsked = true)
        {
            _device.Setup(d => d.IsDeviceConnected).Returns(false);
            _device.Setup(d => d.ConnectAsync()).ReturnsAsync(connectsWhenAsked);
            _device.Setup(d => d.CalibrateZeroAsync()).ReturnsAsync(true);
        }

        [Test]
        public async Task CalibrateAsync_ConnectsCalibratesAndConfirmsTheChannel()
        {
            GivenConnected();

            await _session.CalibrateAsync(_outcomes.Add);

            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.Ready>());
            Assert.That(_session.IsChannelConfirmed, Is.True);
            Assert.That(_session.Colorimeter, Is.EqualTo(new ColorimeterInfo("CA-410", "PROBE1", 2, "Lab")));
            _registry.Verify(r => r.Update("PROBE1", ColorAnalyzerModel.CA410, 2), Times.Once);
            _settings.Verify(s => s.UpdateChannel(2), Times.Once);
        }

        [Test]
        public async Task CalibrateAsync_WhenTheAnalyzerIsAlreadyConnected_DoesNotConnectAgain()
        {
            _device.Setup(d => d.IsDeviceConnected).Returns(true);
            _device.Setup(d => d.CalibrateZeroAsync()).ReturnsAsync(true);

            await _session.CalibrateAsync(_outcomes.Add);

            _device.Verify(d => d.ConnectAsync(), Times.Never);
            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.Ready>());
        }

        [Test]
        public async Task CalibrateAsync_ConnectionFailure_DisconnectsAndReportsIt()
        {
            GivenConnected(connectsWhenAsked: false);

            await _session.CalibrateAsync(_outcomes.Add);

            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.ConnectFailed>());
            _device.Verify(d => d.Disconnect(), Times.Once);
            _device.Verify(d => d.CalibrateZeroAsync(), Times.Never);
        }

        [Test]
        public async Task CalibrateAsync_RefusedCalibration_DisconnectsBeforeReporting()
        {
            GivenConnected();
            _device.Setup(d => d.CalibrateZeroAsync()).ReturnsAsync(false);
            bool disconnectedWhenReported = false;

            await _session.CalibrateAsync(outcome =>
            {
                disconnectedWhenReported = _device.Invocations.Any(i => i.Method.Name == nameof(IColorMeasurementService.Disconnect));
                _outcomes.Add(outcome);
            });

            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.CalibrationFailed>());
            Assert.That(disconnectedWhenReported, Is.True);
        }

        [Test]
        public async Task CalibrateAsync_IsMarkedInProgressWhileTheOutcomeIsReported()
        {
            GivenConnected();
            bool calibratingWhenReported = false;

            await _session.CalibrateAsync(outcome => calibratingWhenReported = _session.IsCalibrating);

            Assert.That(calibratingWhenReported, Is.True);
            Assert.That(_session.IsCalibrating, Is.False);
        }

        [Test]
        public async Task CalibrateAsync_AlwaysRaisesStateRefreshedWhenTheRunEnds()
        {
            GivenConnected();
            int refreshed = 0;
            _session.StateRefreshed += (_, _) => refreshed++;

            await _session.CalibrateAsync(_outcomes.Add);

            Assert.That(refreshed, Is.GreaterThan(0));
        }

        [Test]
        public async Task CalibrateAsync_ExceptionDuringCalibration_DisconnectsAConnectedAnalyzerAndReportsFaulted()
        {
            GivenConnected();
            _device.SetupSequence(d => d.IsDeviceConnected).Returns(false).Returns(true);
            _device.Setup(d => d.CalibrateZeroAsync()).ThrowsAsync(new InvalidOperationException("boom"));

            await _session.CalibrateAsync(_outcomes.Add);

            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.Faulted>());
            _device.Verify(d => d.Disconnect(), Times.Once);
        }

        [Test]
        public async Task CalibrateAsync_DisconnectFailure_IsShownToTheInspector()
        {
            GivenConnected();
            _device.Setup(d => d.CalibrateZeroAsync()).ReturnsAsync(false);
            _device.Setup(d => d.Disconnect()).Throws(new InvalidOperationException("com"));

            await _session.CalibrateAsync(_outcomes.Add);

            _dialog.Verify(d => d.ShowMessage(It.Is<string>(m => m.Contains("com")), It.IsAny<string>()), Times.Once);
            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.CalibrationFailed>());
        }

        [TestCase(0, 3, TestName = "InspectorKeepsTheProbeChannel")]
        [TestCase(1, 5, TestName = "InspectorChoosesTheLastChannel")]
        public async Task CalibrateAsync_ChannelMismatch_StoresTheChosenChannel(int choice, int lastChannel)
        {
            GivenConnected();
            _device.Setup(d => d.CurrentChannel).Returns(3);
            _registry.Setup(r => r.GetLastChannel("PROBE1")).Returns(5);
            _dialog.Setup(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(choice);

            await _session.CalibrateAsync(_outcomes.Add);

            int expected = choice == 1 ? lastChannel : 3;
            _registry.Verify(r => r.Update("PROBE1", ColorAnalyzerModel.CA410, expected), Times.Once);
            _settings.Verify(s => s.UpdateChannel(expected), Times.Once);
            _device.VerifySet(d => d.CurrentChannel = lastChannel, choice == 1 ? Times.Once() : Times.Never());
        }

        [Test]
        public async Task CalibrateAsync_DismissedChannelChoice_DisconnectsAndReportsIt()
        {
            GivenConnected();
            _device.Setup(d => d.CurrentChannel).Returns(3);
            _registry.Setup(r => r.GetLastChannel("PROBE1")).Returns(5);
            _dialog.Setup(d => d.ShowChoice(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((int?)null);

            await _session.CalibrateAsync(_outcomes.Add);

            Assert.That(_outcomes.Single(), Is.InstanceOf<CalibrationOutcome.ChannelNotConfirmed>());
            Assert.That(_session.IsChannelConfirmed, Is.False);
            _device.Verify(d => d.Disconnect(), Times.Once);
            _registry.Verify(r => r.Update(It.IsAny<string>(), It.IsAny<ColorAnalyzerModel>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void IsCalibrated_LostCalibration_DropsTheChannelConfirmation()
        {
            _session.IsCalibrated = true;
            _session.IsChannelConfirmed = true;

            _session.IsCalibrated = false;

            Assert.That(_session.IsChannelConfirmed, Is.False);
        }

        [Test]
        public void DeviceEvents_UpdateTheStateThroughTheDispatcher()
        {
            _dispatcher.Setup(d => d.CheckAccess()).Returns(false);
            var queued = new List<Action>();
            _dispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>())).Callback<Action>(queued.Add);

            _device.Raise(d => d.ConnectionStatusChanged += null, _device.Object, true);
            _device.Raise(d => d.CalibrationStatusChanged += null, _device.Object, true);
            Assert.That((_session.IsConnected, _session.IsCalibrated), Is.EqualTo((false, false)));

            queued.ForEach(action => action());
            Assert.That((_session.IsConnected, _session.IsCalibrated), Is.EqualTo((true, true)));
        }

        [Test]
        public void Dispose_StopsListeningAndDisposesTheAnalyzer()
        {
            _session.Dispose();

            _device.Raise(d => d.ConnectionStatusChanged += null, _device.Object, true);

            Assert.That(_session.IsConnected, Is.False);
            _device.Verify(d => d.Dispose(), Times.Once);
        }
    }
}

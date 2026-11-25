using System.Collections.ObjectModel;
using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class SpecCheckCoordinatorTests
    {
        private Mock<ISpecEvaluationService> _evaluation;
        private Mock<IFileService> _fileService;
        private Mock<IDispatcher> _dispatcher;
        private SpecCheckCoordinator _coordinator;
        private List<SpecCheckResult> _results;

        [SetUp]
        public void SetUp()
        {
            _evaluation = new Mock<ISpecEvaluationService>();
            _fileService = new Mock<IFileService>();
            _fileService.Setup(f => f.SerializeDevice(It.IsAny<DeviceUnderTest>())).Returns("{}");
            _dispatcher = new Mock<IDispatcher>();
            _dispatcher.Setup(d => d.CheckAccess()).Returns(true);
            var statuses = new ObservableCollection<MeasurementStatus>(Enumerable.Range(0, 14).Select(i => new MeasurementStatus($"P{i}")));
            var statusService = new Mock<IMeasurementStatusService>();
            statusService.Setup(s => s.AllMeasurementButtonStatuses).Returns(statuses);
            _coordinator = new SpecCheckCoordinator(_evaluation.Object, _fileService.Object, statusService.Object, _dispatcher.Object);
            _results = [];
        }

        private static SpecEvaluation Evaluation() => new(SpecVerdict.Pass, false, []);

        private static SpecCheckRequest Request(string? configuration = "Config", DeviceUnderTest? device = null, bool isTv = true) =>
            new(configuration, device, "PREVIEW", isTv);

        [Test]
        public async Task Request_WithoutConfiguration_AsksForOneAndDoesNotEvaluate()
        {
            _coordinator.Request(Request(configuration: ""), _results.Add);
            await _coordinator.Pending;

            Assert.That(_results.Single(), Is.InstanceOf<SpecCheckResult.ConfigurationNeeded>());
            _evaluation.Verify(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Request_Evaluates_TheDeviceWithTheConfigurationAndTvFlag()
        {
            var device = new DeviceUnderTest("SN1");
            device.RecordAttempt(new Measurement("Center", 0.3, 0.3, 100, 6500));
            var evaluation = Evaluation();
            _evaluation.Setup(e => e.EvaluateAsync("{}", It.IsAny<CancellationToken>())).ReturnsAsync(new SpecEvaluationOutcome(evaluation, null));

            _coordinator.Request(Request(device: device, isTv: false), _results.Add);
            await _coordinator.Pending;

            var evaluated = (SpecCheckResult.Evaluated)_results.Single();
            Assert.That(evaluated.Evaluation, Is.SameAs(evaluation));
            Assert.That((device.DeviceConfiguration, device.IsTV), Is.EqualTo(("Config", false)));
            Assert.That((evaluated.MeasuredPoints, evaluated.TotalPoints), Is.EqualTo((1, 13)), "a device that is not a TV has no white point");
        }

        [Test]
        public async Task Request_WithoutDevice_EvaluatesAnEmptyOneWithThePreviewSerialNumber()
        {
            _evaluation.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new SpecEvaluationOutcome(Evaluation(), null));

            _coordinator.Request(Request(), _results.Add);
            await _coordinator.Pending;

            var evaluated = (SpecCheckResult.Evaluated)_results.Single();
            Assert.That((evaluated.Device.SerialNumber, evaluated.MeasuredPoints, evaluated.TotalPoints), Is.EqualTo(("PREVIEW", 0, 14)));
        }

        [Test]
        public async Task Request_SameErrorTwice_IsNewOnlyTheFirstTimeAndANewOneIsNewAgain()
        {
            var errors = new Queue<string>(["not found", "not found", "boom"]);
            _evaluation.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new SpecEvaluationOutcome(null, errors.Dequeue()));

            for (int i = 0; i < 3; i++)
            {
                _coordinator.Request(Request(), _results.Add);
                await _coordinator.Pending;
            }

            Assert.That(_results.Cast<SpecCheckResult.Unavailable>().Select(r => r.IsNew), Is.EqualTo(new[] { true, false, true }));
        }

        [Test]
        public async Task Request_AfterAnEvaluation_ReportsTheSameErrorAsNewAgain()
        {
            var outcomes = new Queue<SpecEvaluationOutcome>([new(null, "gone"), new(Evaluation(), null), new(null, "gone")]);
            _evaluation.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(outcomes.Dequeue);

            for (int i = 0; i < 3; i++)
            {
                _coordinator.Request(Request(), _results.Add);
                await _coordinator.Pending;
            }

            Assert.That(_results.OfType<SpecCheckResult.Unavailable>().Select(r => r.IsNew), Is.EqualTo(new[] { true, true }));
        }

        [Test]
        public async Task Request_NewRequestWhileEvaluating_AbandonsTheOlderOne()
        {
            var first = new TaskCompletionSource<SpecEvaluationOutcome>();
            var second = new TaskCompletionSource<SpecEvaluationOutcome>();
            var calls = new Queue<TaskCompletionSource<SpecEvaluationOutcome>>([first, second]);
            _evaluation.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(() => calls.Dequeue().Task);
            var firstResults = new List<SpecCheckResult>();

            _coordinator.Request(Request(), firstResults.Add);
            var firstPending = _coordinator.Pending;
            _coordinator.Request(Request(), _results.Add);
            first.SetResult(new SpecEvaluationOutcome(Evaluation(), null));
            second.SetResult(new SpecEvaluationOutcome(Evaluation(), null));
            await firstPending;
            await _coordinator.Pending;

            Assert.That(firstResults, Is.Empty, "the answer of the abandoned request is dropped");
            Assert.That(_results, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Cancel_AbandonsTheRunningCheck()
        {
            var pending = new TaskCompletionSource<SpecEvaluationOutcome>();
            _evaluation.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(pending.Task);

            _coordinator.Request(Request(), _results.Add);
            _coordinator.Cancel();
            pending.SetResult(new SpecEvaluationOutcome(Evaluation(), null));
            await _coordinator.Pending;

            Assert.That(_results, Is.Empty);
        }

        [Test]
        public async Task Request_OffTheUiThread_DeliversThroughTheDispatcher()
        {
            _dispatcher.Setup(d => d.CheckAccess()).Returns(false);
            var queued = new List<Action>();
            _dispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>())).Callback<Action>(queued.Add);
            _evaluation.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new SpecEvaluationOutcome(Evaluation(), null));

            _coordinator.Request(Request(), _results.Add);
            await _coordinator.Pending;
            Assert.That(_results, Is.Empty);

            queued.ForEach(action => action());
            Assert.That(_results, Has.Count.EqualTo(1));
        }
    }
}

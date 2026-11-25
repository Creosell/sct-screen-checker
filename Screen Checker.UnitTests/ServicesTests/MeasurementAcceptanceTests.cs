using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class MeasurementAcceptanceTests
    {
        private MeasurementAcceptance _acceptance;

        [SetUp]
        public void SetUp() => _acceptance = new MeasurementAcceptance(new AttemptCounter());

        private static Measurement At(MeasurementLocation location, double x, double y, double lv) =>
            new(location.ToString(), x, y, lv, 6500);

        [TestCase(MeasurementLocation.WhiteColor, 0.3127, 0.329, 400)]
        [TestCase(MeasurementLocation.RedColor, 0.67, 0.33, 100)]
        [TestCase(MeasurementLocation.TopLeft, 0.30, 0.31, 350)]
        [TestCase(MeasurementLocation.BlackColor, 0.3, 0.3, 0.05)]
        public void Assess_PlausibleMeasurement_IsAccepted(MeasurementLocation location, double x, double y, double lv)
        {
            Assert.That(_acceptance.Assess(At(location, x, y, lv)), Is.InstanceOf<AcceptanceResult.Accepted>());
        }

        [TestCase(MeasurementLocation.WhiteColor, 0.3127, 0.329, 5.0, TestName = "LuminanceAtTheLowerBoundIsTooLow")]
        [TestCase(MeasurementLocation.RedColor, 0.5, 0.5, 100.0, TestName = "RedFarFromItsPrimary")]
        [TestCase(MeasurementLocation.TopCenter, 0.60, 0.30, 300.0, TestName = "UnnamedPointIsComparedWithWhite")]
        [TestCase(MeasurementLocation.BlackColor, 0.3, 0.3, 5.0, TestName = "BlackNotDarkEnough")]
        public void Assess_ImplausibleMeasurement_IsRejectedAsTheFirstAttempt(MeasurementLocation location, double x, double y, double lv)
        {
            var result = _acceptance.Assess(At(location, x, y, lv));

            var rejected = (AcceptanceResult.Rejected)result;
            Assert.That(rejected.Attempt, Is.EqualTo(1));
            Assert.That(rejected.Reason, Is.Not.Empty);
        }

        [Test]
        public void Assess_CoordinatesOnTheToleranceBoundaryAreAccepted()
        {
            double edge = 0.67 - MeasurementAcceptance.ColorCoordinatesTolerance;

            Assert.That(_acceptance.Assess(At(MeasurementLocation.RedColor, edge, 0.33, 100)), Is.InstanceOf<AcceptanceResult.Accepted>());
            Assert.That(_acceptance.Assess(At(MeasurementLocation.RedColor, edge - 0.001, 0.33, 100)), Is.InstanceOf<AcceptanceResult.Rejected>());
        }

        [Test]
        public void Assess_SecondImplausibleMeasurementInARow_AsksForConfirmationAndStartsOver()
        {
            var bad = At(MeasurementLocation.WhiteColor, 0.3127, 0.329, 1);

            Assert.That(_acceptance.Assess(bad), Is.InstanceOf<AcceptanceResult.Rejected>());
            Assert.That(_acceptance.Assess(bad), Is.InstanceOf<AcceptanceResult.ConfirmationNeeded>());
            Assert.That(((AcceptanceResult.Rejected)_acceptance.Assess(bad)).Attempt, Is.EqualTo(1), "the count starts over after the question");
        }

        [Test]
        public void Assess_AcceptedMeasurementResetsTheRejectedAttempts()
        {
            var bad = At(MeasurementLocation.WhiteColor, 0.3127, 0.329, 1);
            var good = At(MeasurementLocation.WhiteColor, 0.3127, 0.329, 400);

            _acceptance.Assess(bad);
            _acceptance.Assess(good);

            Assert.That(_acceptance.Assess(bad), Is.InstanceOf<AcceptanceResult.Rejected>(), "the count of rejected attempts was reset");
        }

        [Test]
        public void Assess_RejectedAttemptsAreCountedPerPoint()
        {
            var badWhite = At(MeasurementLocation.WhiteColor, 0.3127, 0.329, 1);
            var badRed = At(MeasurementLocation.RedColor, 0.67, 0.33, 1);

            _acceptance.Assess(badWhite);

            Assert.That(_acceptance.Assess(badRed), Is.InstanceOf<AcceptanceResult.Rejected>());
        }
    }
}

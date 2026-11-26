using ScreenChecker.Models;
using ScreenChecker.ViewModels.MeasurementPage;

namespace ScreenChecker.UnitTests.ViewModelsTests.MeasurementPage
    {
    [TestFixture]
    public class MeasurementResultRowTests
        {
        private MeasurementStatus _status;
        private MeasurementResultRow _row;

        [SetUp]
        public void Setup()
            {
            _status = new MeasurementStatus("Center");
            _row = new MeasurementResultRow(_status);
            }

        [Test]
        public void Attention_NotMeasuredPoint_IsPass() =>
            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Pass));

        [Test]
        public void Attention_RejectedMeasurement_IsFail()
            {
            _status.IsPassed = false;

            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Fail));
            }

        [TestCase(SpecStatus.Warning)]
        [TestCase(SpecStatus.Fail)]
        public void Attention_AcceptedMeasurementWithRemark_FollowsRemark(SpecStatus remark)
            {
            _status.IsPassed = true;
            _status.SpecRemark = remark;

            Assert.That(_row.Attention, Is.EqualTo(remark));
            }

        [Test]
        public void Attention_RemarkOfAPointWithEnoughAttempts_AsksForNothing()
            {
            _status.IsPassed = true;
            _status.SpecRemark = SpecStatus.Fail;
            _row.ShowAttempts(new SpecAttemptInfo(MeasurementResultRow.EnoughAttempts, 2, true));

            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Pass));
            }

        [Test]
        public void Attention_RemarkOfAPointWithFewAttempts_KeepsAsking()
            {
            _status.IsPassed = true;
            _status.SpecRemark = SpecStatus.Fail;
            _row.ShowAttempts(new SpecAttemptInfo(1, 0, true));

            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Fail));
            }

        [Test]
        public void Attention_RejectedMeasurementOfAPointWithEnoughAttempts_StillAsks()
            {
            _row.ShowAttempts(new SpecAttemptInfo(MeasurementResultRow.EnoughAttempts, 2, true));
            _status.IsPassed = false;

            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Fail));
            }

        [Test]
        public void Verdict_PointMeasuredAgain_KeepsTheLastVerdict()
            {
            _status.IsPassed = false;
            _status.MeasuredValuesString = "x=0.3";

            _status.IsPassed = null;
            _status.MeasuredValuesString = "Measuring";

            Assert.That(_row.Verdict, Is.False);
            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Fail));
            }

        [Test]
        public void Verdict_PointReset_IsCleared()
            {
            _status.IsPassed = true;
            _status.MeasuredValuesString = "x=0.3";

            _status.IsPassed = null;
            _status.MeasuredValuesString = "";

            Assert.That(_row.Verdict, Is.Null);
            Assert.That(_row.Attention, Is.EqualTo(SpecStatus.Pass));
            }

        [Test]
        public void Attention_StatusChanged_RaisesPropertyChanged()
            {
            var raised = new List<string?>();
            _row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            _status.IsPassed = false;

            Assert.That(raised, Does.Contain(nameof(MeasurementResultRow.Attention)));
            }
        }
    }

using ScreenChecker.Models;
using ScreenChecker.Views;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// Shows the measurement page at the smallest window size in every language, takes it through the states an operator meets with
    /// the longest data the application can hold, and audits what is written on it after every step (<see cref="LayoutAudit"/>).
    /// One window serves one language, so the whole check stays fast.
    /// </summary>
    [TestFixture]
    [Category("Ui")]
    public class PageLayoutTests : UiTestHost
        {
        /// <summary>The longest channel name the analyzers keep (10 characters): the widest Latin letters, the Chinese for the language that has it.</summary>
        private static string WorstChannelName(string culture) => culture == "zh-Hans" ? "实验室电视机测试通道" : "WWWWWWWWWW";

        /// <summary>
        /// The longest device configuration name of the delivered configurations (<c>SDNB-M16iA_GW04</c>) with the factory suffix the
        /// naming rules of the configurations allow (<c>_CH19</c>).
        /// </summary>
        private const string LongConfiguration = "SDNB-M16iA_GW04_CH19";

        [TestCase("", TestName = "MeasurementPage_ReadsWell_InEveryState_English")]
        [TestCase("ru", TestName = "MeasurementPage_ReadsWell_InEveryState_Russian")]
        [TestCase("zh-Hans", TestName = "MeasurementPage_ReadsWell_InEveryState_Chinese")]
        public void MeasurementPage_ReadsWell_InEveryState(string culture)
            {
            Ui(() =>
                {
                UseLanguage(culture);
                var cultureInfo = CultureOf(culture);
                string language = LanguageName(culture);
                var problems = new List<string>();
                double normalSpeed = MeasurementView.TransitionSpeed;
                MeasurementView.TransitionSpeed = 100;

                using var page = CreatePage();
                var view = new MeasurementView { DataContext = page.ViewModel };
                var window = ShowPage(view);
                try
                    {
                    void Audit(string state, bool cardChanges = false)
                        {
                        Flush(window);
                        if (cardChanges) Pump(80);
                        Shot(view, state);
                        problems.AddRange(LayoutAudit.Run(view, cultureInfo, $"[{language}/{state}]"));
                        }

                    Audit("not connected");

                    page.ViewModel.IsDeviceConnected = true;
                    Audit("calibrating", cardChanges: true);

                    CalibrateAnalyzer(page, WorstChannelName(culture));
                    Audit("ready, longest channel name", cardChanges: true);

                    page.ViewModel.SelectedDeviceConfiguration = LongConfiguration;
                    page.ViewModel.SerialNumber = "12345678901234567890";
                    FillResultsWithLongestValues(page.ViewModel);
                    Audit("configuration chosen, every point measured");

                    page.ViewModel.Spec.Show(WorstSpecEvaluation(), measuredPoints: 14, totalPoints: 14);
                    Audit("device info with the longest checks");

                    page.ViewModel.SelectedTabIndex = 1;
                    Audit("log tab");
                    page.ViewModel.SelectedTabIndex = 0;

                    page.ViewModel.ZeroCalibrationFeedback.Succeed();
                    page.ViewModel.SaveFeedback.Succeed();
                    page.ViewModel.ClearFeedback.Succeed();
                    page.ViewModel.ReportFeedback.Fail();
                    page.ViewModel.UploadFeedback.Succeed();
                    Pump(120);
                    Audit("feedback of the buttons");
                    }
                finally
                    {
                    window.Close();
                    MeasurementView.TransitionSpeed = normalSpeed;
                    }

                Assert.That(problems, Is.Empty, Environment.NewLine + string.Join(Environment.NewLine, problems));
                });
            }
        }
    }

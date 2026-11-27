using ScreenChecker.Models;
using ScreenChecker.ViewModels;
using ScreenChecker.Views;
using System.Windows;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// Shows the real main window (title bar, navigation, the page) at the smallest size in every language and audits the
    /// measurement page and the settings page, each with the longest data, after every state the operator can bring them to.
    /// </summary>
    [TestFixture]
    [Category("Ui")]
    public class WindowLayoutTests : UiTestHost
        {
        private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
            {
            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
                {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T match) return match;
                var nested = FindDescendant<T>(child);
                if (nested != null) return nested;
                }
            return null;
            }

        [TestCase("", "Maximize", "Restore")]
        [TestCase("ru", "Развернуть", "Восстановить")]
        [TestCase("zh-Hans", "最大化", "向下还原")]
        public void MaximizeButton_ShowsRestore_WhileTheWindowIsMaximized(string culture, string maximizeTip, string restoreTip)
            {
            Ui(() =>
                {
                UseLanguage(culture);
                using var page = CreatePage();
                var window = ShowMainWindow(page, CreateSettings(page.Analyzer));
                try
                    {
                    var button = (System.Windows.Controls.Image?)window.FindName("MaximizeRestoreImage");
                    Assert.That(button, Is.Not.Null, "the maximize button is not found");
                    string Icon() => ((System.Windows.Media.Imaging.BitmapImage)button!.Source).UriSource.OriginalString;

                    Assert.That(Icon(), Does.EndWith("window-maximize.png"));

                    window.WindowState = WindowState.Maximized;
                    Flush(window);
                    Assert.That(Icon(), Does.EndWith("window-restore.png"), "a maximized window offers to restore");
                    Assert.That(button.ToolTip, Is.EqualTo(restoreTip));

                    window.WindowState = WindowState.Normal;
                    Flush(window);
                    Assert.That(Icon(), Does.EndWith("window-maximize.png"), "a restored window offers to maximize");
                    Assert.That(button.ToolTip, Is.EqualTo(maximizeTip));
                    }
                finally
                    {
                    window.Close();
                    }
                });
            }

        [TestCase("", TestName = "MainWindow_ReadsWell_OnEveryPage_English")]
        [TestCase("ru", TestName = "MainWindow_ReadsWell_OnEveryPage_Russian")]
        [TestCase("zh-Hans", TestName = "MainWindow_ReadsWell_OnEveryPage_Chinese")]
        public void MainWindow_ReadsWell_OnEveryPage(string culture)
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
                var settings = CreateSettings(page.Analyzer);
                var window = ShowMainWindow(page, settings);
                try
                    {
                    var root = (FrameworkElement)window.Content;
                    void Audit(string state, bool animates = false)
                        {
                        Flush(window);
                        if (animates) Pump(80);
                        problems.AddRange(LayoutAudit.Run(root, cultureInfo, $"[{language}/{state}]"));
                        }

                    var settingsView = FindDescendant<SettingsView>(root);
                    var measurementView = FindDescendant<MeasurementView>(root);
                    Assert.That(settingsView!.IsVisible, Is.False, $"the settings page is visible at the start (its visibility {((FrameworkElement)settingsView.Parent ?? settingsView).Visibility}, parent {settingsView.Parent?.GetType().Name}, vm page {((MainWindowViewModel)window.DataContext).CurrentPageIdentifier})");
                    Assert.That(measurementView!.IsVisible, Is.True);
                    Audit("measurement page");

                    CalibrateAnalyzer(page, culture == "zh-Hans" ? "实验室电视机测试通道" : "WWWWWWWWWW");
                    page.ViewModel.SelectedDeviceConfiguration = "SDNB-M16iA_GW04_CH19";
                    page.ViewModel.SerialNumber = "12345678901234567890";
                    FillResultsWithLongestValues(page.ViewModel);
                    page.ViewModel.Spec.Show(WorstSpecEvaluation(), measuredPoints: 14, totalPoints: 14);
                    Audit("measurement page, longest data", animates: true);

                    var navigate = ((MainWindowViewModel)window.DataContext).NavigateCommand;
                    navigate.Execute("Settings");
                    Audit("settings page");

                    settings.ColorAnalyzerChannel = "99";
                    settings.SyncMode = SyncMode.Internal;
                    settings.SyncFrequency = "240.0";
                    Audit("settings page, internal synchronization");

                    settings.SyncMode = SyncMode.Manual;
                    settings.IntegrationTime = "4000.0";
                    Audit("settings page, manual integration time");

                    settings.SaveFeedback.Succeed();
                    settings.ResetFeedback.Fail();
                    Pump(120);
                    Audit("settings page, feedback of the buttons");

                    navigate.Execute("Measurement");
                    Audit("measurement page again");
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

using System.Globalization;
using System.Windows;
using ScreenChecker.Models;
using ScreenChecker.ViewModels;
using ScreenChecker.ViewModels.SettingsPage;
using ScreenChecker.Views;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// Shows the real main window on every connected display at every display scale a user can set (the work area of the layout is the
    /// physical work area of the display divided by the scale), and checks that the window stays within the screen and that everything on
    /// its pages still reads: the page is the same layout, shrunk in proportion by <see cref="MinimumSizeViewbox"/> when the room is less than
    /// its design size. The window is shown at its starting size and, where the room differs, maximized.
    /// </summary>
    [TestFixture]
    [Category("Ui")]
    public class DpiLayoutTests : UiTestHost
        {
        /// <summary>The display scales of Windows (125%, 150%, 175%, ...) and the largest one it offers, 400%.</summary>
        private static readonly double[] Scales = [1.0, 1.25, 1.5, 1.75, 2.0, 2.5, 3.0, 4.0];

        /// <summary>A display at a display scale: the room the window has in pixels of the layout.</summary>
        private sealed record Screen(string Name, Size Area);

        private static Size AreaOf(DisplayInfo display, double scale) => new(Math.Round(display.WorkWidth / scale), Math.Round(display.WorkHeight / scale));

        /// <summary>
        /// The screens to try: every connected display at every scale, the same room once (a display at 125% and another at 150% that leave the
        /// same room are the same screen for the layout).
        /// </summary>
        private static List<Screen> ScreensOfThisComputer() =>
            Displays.Connected()
                .SelectMany(display => Scales.Select(scale => new Screen($"{display.Name} at {scale * 100:F0}%", AreaOf(display, scale))))
                .GroupBy(screen => screen.Area)
                .Select(group => group.First())
                .ToList();

        /// <summary>The room the window starts with on a screen: the design size, or the screen when that is less.</summary>
        private static Size StartingSize(Size area) => new(Math.Min(MinWindowWidth, area.Width), Math.Min(MinWindowHeight, area.Height));

        [TestCase("", TestName = "MainWindow_FitsEveryDisplayAtEveryScale_AndReadsWell_English")]
        [TestCase("ru", TestName = "MainWindow_FitsEveryDisplayAtEveryScale_AndReadsWell_Russian")]
        [TestCase("zh-Hans", TestName = "MainWindow_FitsEveryDisplayAtEveryScale_AndReadsWell_Chinese")]
        public void MainWindow_FitsEveryDisplayAtEveryScale_AndReadsWell(string culture)
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
                CalibrateAnalyzer(page, culture == "zh-Hans" ? "实验室电视机测试通道" : "WWWWWWWWWW");
                page.ViewModel.SelectedDeviceConfiguration = "SDNB-M16iA_GW04_CH19";
                page.ViewModel.SerialNumber = "12345678901234567890";
                FillResultsWithLongestValues(page.ViewModel);
                page.ViewModel.Spec.Show(WorstSpecEvaluation(), measuredPoints: 14, totalPoints: 14);
                settings.ColorAnalyzerChannel = "99";
                settings.SyncMode = SyncMode.Manual;
                settings.IntegrationTime = "4000.0";

                var starting = new HashSet<Size>();
                var maximized = new HashSet<Size>();
                MainWindow? stretched = null;
                try
                    {
                    foreach (var screen in ScreensOfThisComputer())
                        {
                        UiMetrics.WorkAreaOverride = screen.Area;

                        // The window as it starts: the same layout on every screen that leaves the design size, so once
                        if (starting.Add(StartingSize(screen.Area)))
                            CheckStartingWindow(screen, page, settings, cultureInfo, language, problems);

                        // The window maximized: the page stretched over the whole work area of the screen. One window is resized to every
                        // work area, as a window maximized on another monitor is
                        if (screen.Area != StartingSize(screen.Area) && maximized.Add(screen.Area))
                            {
                            stretched ??= ShowMainWindow(page, settings, sizeFromScreen: true);
                            CheckMaximizedWindow(stretched, screen, cultureInfo, language, problems);
                            }
                        }
                    }
                finally
                    {
                    stretched?.Close();
                    UiMetrics.WorkAreaOverride = null;
                    MeasurementView.TransitionSpeed = normalSpeed;
                    }

                Assert.That(problems, Is.Empty, Environment.NewLine + string.Join(Environment.NewLine, problems));
                });
            }

        /// <summary>Shows the window as it starts on a screen and checks that it fits, that the page is shrunk as the room asks and that everything on both pages reads.</summary>
        private void CheckStartingWindow(Screen screen, PageFixture page, SettingsViewModel settings, CultureInfo culture, string language, List<string> problems)
            {
            var window = ShowMainWindow(page, settings, sizeFromScreen: true);
            try
                {
                string where = $"{language}, {screen.Name} ({screen.Area.Width:F0}x{screen.Area.Height:F0}), starting size";
                Pump(25);
                Flush(window);
                var root = CheckFit(window, screen.Area, where, problems);

                problems.AddRange(LayoutAudit.Run(root, culture, $"[{where}, measurement page]"));
                Shot(root, $"{where} measurement page");

                ((MainWindowViewModel)window.DataContext).NavigateCommand.Execute("Settings");
                Flush(window);
                problems.AddRange(LayoutAudit.Run(root, culture, $"[{where}, settings page]"));
                Shot(root, $"{where} settings page");
                }
            finally
                {
                window.Close();
                }
            }

        /// <summary>Stretches the window over a whole work area and checks the measurement page, which has the most to lay out.</summary>
        private void CheckMaximizedWindow(MainWindow window, Screen screen, CultureInfo culture, string language, List<string> problems)
            {
            string where = $"{language}, {screen.Name} ({screen.Area.Width:F0}x{screen.Area.Height:F0}), maximized";
            window.MinWidth = window.MinHeight = 0;
            window.Width = screen.Area.Width;
            window.Height = screen.Area.Height;
            Flush(window);
            var root = CheckFit(window, screen.Area, where, problems);

            problems.AddRange(LayoutAudit.Run(root, culture, $"[{where}, measurement page]"));
            Shot(root, $"{where} measurement page");
            }

        /// <summary>Checks that the window is within the screen and that the page is shrunk as much as the room asks.</summary>
        private static MinimumSizeViewbox CheckFit(MainWindow window, Size area, string where, List<string> problems)
            {
            if (window.ActualWidth > area.Width + 1 || window.ActualHeight > area.Height + 1)
                problems.Add($"[{where}]: the window is {window.ActualWidth:F0}x{window.ActualHeight:F0}, the screen gives {area.Width:F0}x{area.Height:F0}");

            var root = (MinimumSizeViewbox)window.Content;
            double expected = Math.Min(1, Math.Min(window.ActualWidth / MinWindowWidth, window.ActualHeight / MinWindowHeight));
            if (Math.Abs(root.Scale - expected) > 0.01)
                problems.Add($"[{where}]: the page is shrunk by {root.Scale:F2}, the room asks for {expected:F2}");
            return root;
            }

        [Test]
        public void ScreensOfThisComputer_CoverEveryConnectedDisplayAtEveryScale()
            {
            var displays = Displays.Connected();
            var screens = ScreensOfThisComputer();

            Assert.That(displays, Is.Not.Empty);
            foreach (var display in displays)
                foreach (double scale in Scales)
                    Assert.That(screens.Any(screen => screen.Area == AreaOf(display, scale)), $"{display.Name} at {scale * 100:F0}% is not tried");
            }

        [Test]
        public void MinimumSizeViewbox_ShrinksOnlyBelowTheMinimum_AndNeverEnlarges()
            {
            Ui(() =>
                {
                var content = new System.Windows.Controls.Grid();
                var viewbox = new MinimumSizeViewbox { MinContentWidth = 1100, MinContentHeight = 600, Child = content };

                (double Scale, Size Content) Arrange(double width, double height)
                    {
                    viewbox.Measure(new Size(width, height));
                    viewbox.Arrange(new Rect(0, 0, width, height));
                    return (viewbox.Scale, new Size(content.ActualWidth, content.ActualHeight));
                    }

                var roomy = Arrange(1920, 1040);
                Assert.That(roomy.Scale, Is.EqualTo(1).Within(0.0001), "with room enough the content is not shrunk");
                Assert.That(roomy.Content, Is.EqualTo(new Size(1920, 1040)), "with room enough the content fills the room, it is not left at the design size");

                var exact = Arrange(1100, 600);
                Assert.That(exact.Scale, Is.EqualTo(1).Within(0.0001));

                var narrow = Arrange(960, 520);
                Assert.That(narrow.Content, Is.EqualTo(new Size(1100, 600)), "below the minimum the content keeps the design size");
                Assert.That(narrow.Scale, Is.EqualTo(Math.Min(960 / 1100.0, 520 / 600.0)).Within(0.0001), "it is shrunk by the tighter side");

                var tiny = Arrange(640, 347);
                Assert.That(tiny.Scale, Is.EqualTo(Math.Min(640 / 1100.0, 347 / 600.0)).Within(0.0001));
                });
            }
        }
    }

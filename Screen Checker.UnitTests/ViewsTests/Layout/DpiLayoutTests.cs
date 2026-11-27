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

        /// <summary>The interface sizes of the settings the operator can choose.</summary>
        private static IEnumerable<SettingsViewModel.InterfaceScaleOption> InterfaceSizes => SettingsViewModel.AvailableInterfaceScales;

        /// <summary>
        /// A display at a display scale and an interface size of the settings: the work area the window has in pixels of the system and the
        /// room the layout has in it (the interface drawn larger is laid out in less room).
        /// </summary>
        private sealed record Screen(string Name, Size Area, SettingsViewModel.InterfaceScaleOption InterfaceSize)
            {
            public double InterfaceScale => InterfaceSize.Factor;

            public Size Room => new(Math.Round(Area.Width / InterfaceScale), Math.Round(Area.Height / InterfaceScale));
            }

        private static Size AreaOf(DisplayInfo display, double scale) => new(Math.Round(display.WorkWidth / scale), Math.Round(display.WorkHeight / scale));

        /// <summary>
        /// The screens to try: every connected display at every display scale with every interface size of the settings.
        /// </summary>
        private static List<Screen> ScreensOfThisComputer() =>
            Displays.Connected()
                .SelectMany(display => Scales.SelectMany(scale => InterfaceSizes.Select(size => new Screen($"{display.Name} at {scale * 100:F0}%, interface {size.DisplayName}", AreaOf(display, scale), size))))
                .ToList();

        /// <summary>Puts a room into a class of rooms that differ by less than a quarter in width and in height.</summary>
        private static (long, long) RoomClass(Size room) =>
            ((long)Math.Round(Math.Log(room.Width) / Math.Log(1.25)), (long)Math.Round(Math.Log(room.Height) / Math.Log(1.25)));

        /// <summary>The room the layout has when the window starts on a screen: the design size, or the room when that is less.</summary>
        private static Size StartingSize(Size room) => new(Math.Min(MinWindowWidth, room.Width), Math.Min(MinWindowHeight, room.Height));

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

                var starting = new HashSet<(bool, double)>();
                var maximized = new HashSet<(long, long)>();
                MainWindow? stretched = null;
                try
                    {
                    foreach (var screen in ScreensOfThisComputer())
                        {
                        UiMetrics.WorkAreaOverride = screen.Area;
                        settings.SelectedInterfaceScale = screen.InterfaceSize;

                        // The window as it starts: the layout has the design size on every screen, shrunk when the room is less, so once for
                        // an interface size and once for a room that is less than the design size
                        if (starting.Add((StartingSize(screen.Room) != new Size(MinWindowWidth, MinWindowHeight), screen.InterfaceScale)))
                            CheckStartingWindow(screen, page, settings, cultureInfo, language, problems);

                        // The window maximized: the page stretched over the whole work area of the screen. One window is resized to every
                        // work area, as a window maximized on another monitor is; rooms that differ by less than a quarter are laid out once
                        if (screen.Room != StartingSize(screen.Room) && maximized.Add(RoomClass(screen.Room)))
                            {
                            stretched ??= ShowMainWindow(page, settings, sizeFromScreen: true);
                            CheckMaximizedWindow(stretched, screen, cultureInfo, language, problems);
                            }
                        }
                    }
                finally
                    {
                    stretched?.Close();
                    settings.SelectedInterfaceScale = SettingsViewModel.AvailableInterfaceScales[0];
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
                var root = CheckFit(window, screen, where, problems);

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
            var root = CheckFit(window, screen, where, problems);

            problems.AddRange(LayoutAudit.Run(root, culture, $"[{where}, measurement page]"));
            Shot(root, $"{where} measurement page");
            }

        /// <summary>Checks that the window is within the screen and that the page is shrunk as much as the room asks.</summary>
        private static MinimumSizeViewbox CheckFit(MainWindow window, Screen screen, string where, List<string> problems)
            {
            var area = screen.Area;
            if (window.ActualWidth > area.Width + 1 || window.ActualHeight > area.Height + 1)
                problems.Add($"[{where}]: the window is {window.ActualWidth:F0}x{window.ActualHeight:F0}, the screen gives {area.Width:F0}x{area.Height:F0}");

            var root = (MinimumSizeViewbox)window.Content;
            double expected = Math.Min(1, Math.Min(window.ActualWidth / screen.InterfaceScale / MinWindowWidth, window.ActualHeight / screen.InterfaceScale / MinWindowHeight));
            if (Math.Abs(root.Scale - expected) > 0.01)
                problems.Add($"[{where}]: the page is shrunk by {root.Scale:F2}, the room asks for {expected:F2}");
            return root;
            }

        [Test]
        public void ScreensOfThisComputer_CoverEveryConnectedDisplayAtEveryScaleAndInterfaceSize()
            {
            var displays = Displays.Connected();
            var screens = ScreensOfThisComputer();

            Assert.That(displays, Is.Not.Empty);
            Assert.That(InterfaceSizes.Count(), Is.GreaterThan(1), "the settings offer sizes besides the one of the system");
            foreach (var display in displays)
                foreach (double scale in Scales)
                    foreach (var size in InterfaceSizes)
                        Assert.That(screens.Any(screen => screen.Area == AreaOf(display, scale) && screen.InterfaceSize == size), $"{display.Name} at {scale * 100:F0}%, interface {size.DisplayName} is not tried");
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

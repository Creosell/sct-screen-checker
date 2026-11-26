using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using Moq;
using ScreenChecker.Commands;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;
using Microsoft.Extensions.DependencyInjection;
using ScreenChecker.ViewModels;
using ScreenChecker.ViewModels.MeasurementPage;
using ScreenChecker.ViewModels.SettingsPage;
using ScreenChecker.Views;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// The mocks behind a view model of the measurement page; a test sets what the analyzer reports through them.
    /// </summary>
    /// <param name="ViewModel">The view model under test.</param>
    /// <param name="Analyzer">The color analyzer service the page talks to.</param>
    /// <param name="Registry">The registry of the analyzers (the channel of every probe).</param>
    /// <param name="Statuses">The statuses of the measurement points (their buttons and rows).</param>
    public sealed record PageFixture(MeasurementViewModel ViewModel, Mock<IColorMeasurementService> Analyzer, Mock<IAnalyzerRegistryService> Registry, IMeasurementStatusService Statuses) : IDisposable
        {
        public void Dispose() => ViewModel.Dispose();
        }

    /// <summary>
    /// What the layout tests share: the UI thread, the resources of the application in a language and the view model of the
    /// measurement page. The languages are the three the application is delivered in.
    /// </summary>
    public abstract class UiTestHost
        {
        /// <summary>The cultures of the interface: English is the invariant culture, the others are named like the application does.</summary>
        public static readonly string[] Cultures = ["", "ru", "zh-Hans"];

        /// <summary>Smallest window of the application (<c>MainWindow.MinWidth</c> x <c>MinHeight</c>).</summary>
        protected const double MinWindowWidth = 1100, MinWindowHeight = 600;

        /// <summary>The navigation column (160) and the title bar (30) of the main window around a page.</summary>
        protected const double SidebarWidth = 160, TitleBarHeight = 30;

        private Thread _uiThread = null!;
        private Dispatcher _dispatcher = null!;

        [OneTimeSetUp]
        public void StartUiThread()
            {
            var started = new ManualResetEventSlim();
            _uiThread = new Thread(() =>
                {
                _dispatcher = Dispatcher.CurrentDispatcher;
                if (Application.Current == null) _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                started.Set();
                Dispatcher.Run();
                }) { IsBackground = true };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
            started.Wait();
            }

        [OneTimeTearDown]
        public void StopUiThread() => _dispatcher.InvokeShutdown();

        /// <summary>Runs the body on the UI thread and rethrows what it threw.</summary>
        protected void Ui(Action body)
            {
            Exception? error = null;
            _dispatcher.Invoke(() =>
                {
                try { body(); }
                catch (Exception e) { error = e; }
                });
            if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
            }

        /// <summary>Lets the UI thread work (layout, animations) for the time.</summary>
        protected static void Pump(int milliseconds)
            {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            Dispatcher.PushFrame(frame);
            }

        /// <summary>
        /// Lets the window finish what is queued (bindings, layout, templates) without waiting for a fixed time: the idle priority
        /// is reached when everything above it is done.
        /// </summary>
        protected static void Flush(Window window)
            {
            window.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, () => { });
            window.UpdateLayout();
            }

        /// <summary>
        /// Saves a picture of a shown element when the environment variable <c>SCT_UI_SHOTS</c> names a folder: the tests then double as
        /// the source of before/after screenshots of what the audit found. Without the variable nothing is drawn, so the run stays fast.
        /// </summary>
        /// <param name="element">The shown element (a page, the content of a window).</param>
        /// <param name="state">What the picture shows; part of the file name.</param>
        protected static void Shot(FrameworkElement element, string state)
            {
            string? folder = Environment.GetEnvironmentVariable("SCT_UI_SHOTS");
            if (string.IsNullOrEmpty(folder)) return;

            Directory.CreateDirectory(folder);
            // The picture is drawn with the margin of the element, so take the first element above it that has none
            while (element.Margin != default && VisualTreeHelper.GetParent(element) is FrameworkElement parent) element = parent;
            const double scale = 2;
            int width = (int)Math.Ceiling(element.ActualWidth * scale), height = (int)Math.Ceiling(element.ActualHeight * scale);
            var bitmap = new RenderTargetBitmap(width, height, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            var background = new DrawingVisual();
            using (var context = background.RenderOpen())
                context.DrawRectangle(Application.Current.TryFindResource("AppPageBackground") as Brush ?? Brushes.DimGray, null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            bitmap.Render(background);
            bitmap.Render(element);

            string language = Thread.CurrentThread.CurrentUICulture.Name is { Length: > 0 } name ? name : "en";
            string file = new string(state.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(folder, $"{language}_{file}.png"));
            encoder.Save(stream);
            }

        /// <summary>The culture of the interface in a language.</summary>
        protected static CultureInfo CultureOf(string culture) => culture == "" ? CultureInfo.InvariantCulture : new CultureInfo(culture);

        /// <summary>The short name of a language for messages.</summary>
        protected static string LanguageName(string culture) => culture == "" ? "en" : culture;

        /// <summary>
        /// Switches the interface to a language on the UI thread: the culture of the thread (the names of the points come from it)
        /// and the resources of the application, put together like <c>App.xaml</c> and the language switch do.
        /// </summary>
        protected static void UseLanguage(string culture)
            {
            var cultureInfo = CultureOf(culture);
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            Thread.CurrentThread.CurrentCulture = cultureInfo;

            const string root = "pack://application:,,,/Screen Checker;component/Resources/";
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(root + "StringResources.xaml") });
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(root + "Themes/Theme.Dark.xaml") });
            resources.MergedDictionaries.Add(new BundledTheme { BaseTheme = BaseTheme.Dark, PrimaryColor = MaterialDesignColors.PrimaryColor.DeepPurple, SecondaryColor = MaterialDesignColors.SecondaryColor.Lime });
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml") });
            resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(root + "Styles/FieldStyles.xaml") });
            if (culture != "")
                resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"{root}StringResources.{culture}.xaml") });
            if (culture == "zh-Hans")
                resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(root + "Typography.zh-Hans.xaml") });
            resources.Add(new DataTemplateKey(typeof(MeasurementViewModel)), new DataTemplate(typeof(MeasurementViewModel)) { VisualTree = new FrameworkElementFactory(typeof(MeasurementView)) });
            resources.Add(new DataTemplateKey(typeof(SettingsViewModel)), new DataTemplate(typeof(SettingsViewModel)) { VisualTree = new FrameworkElementFactory(typeof(SettingsView)) });
            Application.Current.Resources = resources;
            }

        /// <summary>Builds the view model of the measurement page with every dependency replaced by a mock, as the view model tests do.</summary>
        protected static PageFixture CreatePage()
            {
            var dispatcher = new Mock<IDispatcher>();
            dispatcher.Setup(d => d.CheckAccess()).Returns(true);
            dispatcher.Setup(d => d.Invoke(It.IsAny<Action>())).Callback((Action action) => action());
            dispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>())).Callback((Action action) => action());
            var localization = new Mock<ILocalizationService>();
            localization.Setup(s => s.GetString(It.IsAny<string>())).Returns((string key) => key);
            var paths = new Mock<IPathProvider>();
            paths.Setup(p => p.BaseDirectory).Returns("C:\\TestApp");
            paths.Setup(p => p.ConfigDirectory).Returns("C:\\TestApp\\config");
            paths.Setup(p => p.DataDirectory).Returns("C:\\TestApp\\data");
            var statuses = new Mock<IMeasurementStatusService>();
            statuses.Setup(m => m.AllMeasurementButtonStatuses).Returns(
                new System.Collections.ObjectModel.ObservableCollection<MeasurementStatus>(
                    Enum.GetValues<MeasurementLocation>().Select(location => new MeasurementStatus(location.ToString()))));
            var files = new Mock<IFileService>();
            var dialogs = new Mock<IDialogService>();
            var reports = new Mock<IReportGenerationService>();
            reports.Setup(r => r.ExecutablePath).Returns(@"C:\TestApp\Report Generator.exe");
            var logService = new LogService(localization.Object, dispatcher.Object);
            var analyzer = new Mock<IColorMeasurementService>();
            var registry = new Mock<IAnalyzerRegistryService>();

            var viewModel = new MeasurementViewModel(
                new AnalyzerSession(analyzer.Object, registry.Object, dialogs.Object, dispatcher.Object),
                files.Object,
                dialogs.Object,
                localization.Object,
                dispatcher.Object,
                new Mock<IUploadService>().Object,
                paths.Object,
                statuses.Object,
                new SpecCheckCoordinator(new Mock<ISpecEvaluationService>().Object, files.Object, statuses.Object, dispatcher.Object),
                new MeasurementAcceptance(new AttemptCounter()),
                new DeviceConfigurationService(new System.IO.Abstractions.TestingHelpers.MockFileSystem(), paths.Object),
                reports.Object,
                logService,
                new SpecSummaryViewModel(statuses.Object, logService.Append));
            return new PageFixture(viewModel, analyzer, registry, statuses.Object);
            }

        /// <summary>
        /// Shows a page in an off-screen window of the smallest size the application allows: the main window less its title bar
        /// and navigation column.
        /// </summary>
        protected static Window ShowPage(FrameworkElement page)
            {
            var host = new Grid { Width = MinWindowWidth - SidebarWidth, Height = MinWindowHeight - TitleBarHeight };
            host.Children.Add(page);
            var window = new Window
                {
                Content = host,
                Width = host.Width,
                Height = host.Height,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                Left = -20000,
                Top = -20000
                };
            window.Show();
            Flush(window);
            return window;
            }

        /// <summary>Builds the view model of the settings page with every dependency replaced by a mock.</summary>
        protected static SettingsViewModel CreateSettings(Mock<IColorMeasurementService>? analyzer = null)
            {
            var settings = new Mock<ISettingsService>();
            settings.Setup(s => s.LoadSettings()).Returns(new AppSettings());
            settings.Setup(s => s.CurrentSettings).Returns(new AppSettings());
            var localization = new Mock<ILocalizationService>();
            localization.Setup(s => s.GetString(It.IsAny<string>())).Returns((string key) => key);
            return new SettingsViewModel(settings.Object, new Mock<IDialogService>().Object, (analyzer ?? new Mock<IColorMeasurementService>()).Object, localization.Object);
            }

        /// <summary>
        /// Shows the real main window (title bar, navigation, the page) at the smallest size the application allows. The pages come from
        /// the given view models like the service provider of the application hands them out.
        /// </summary>
        protected static MainWindow ShowMainWindow(PageFixture page, SettingsViewModel settings)
            {
            var services = new ServiceCollection();
            services.AddSingleton(page.ViewModel);
            services.AddSingleton(settings);
            var window = new MainWindow
                {
                DataContext = new MainWindowViewModel(services.BuildServiceProvider()),
                Width = MinWindowWidth,
                Height = MinWindowHeight,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000,
                Top = -20000
                };
            window.Show();
            Flush(window);
            return window;
            }

        /// <summary>
        /// A specification check with every metric the page knows (all gamuts of all color spaces, in both diagrams, in both methods),
        /// every bound present (the widest bounds of the delivered TV configuration: min 3500, typ 5000, max 1000000) and a value of
        /// five digits with three decimals, so that the Device info table, the log and the confirmation of saving hold the longest lines.
        /// </summary>
        protected static SpecEvaluation WorstSpecEvaluation()
            {
            string[] points = ["TopLeft", "TopCenter", "TopRight", "MiddleLeft", "Center", "MiddleRight", "BottomLeft", "BottomCenter", "BottomRight", "RedColor", "GreenColor", "BlueColor", "WhiteColor", "BlackColor"];
            var keys = new List<string> { "Brightness", "Black_luminance", "Brightness_uniformity", "Contrast", "Temperature", "Delta_e" };
            foreach (string space in new[] { "rgb", "ntsc", "dcip3" })
                foreach (string kind in new[] { "", "_area", "_uv", "_uv_area" })
                    keys.Add($"Cg_{space}{kind}");

            var statuses = new[] { SpecStatus.Fail, SpecStatus.Warning, SpecStatus.Pending, SpecStatus.Pass };
            var checks = keys.Select((key, index) => new SpecCheck(
                key, 99999.999, 3500, 5000, 1000000, statuses[index % statuses.Length], points, points)).ToList();
            return new SpecEvaluation(SpecVerdict.Fail, Complete: false, checks)
                {
                Attempts = points.ToDictionary(point => point, _ => new SpecAttemptInfo(99, 0, true))
                };
            }

        /// <summary>
        /// Calibrates the simulated analyzer the way the operator does: the view model goes through the zero calibration against
        /// an analyzer that reports the model, the channel with its name and the probe serial number.
        /// </summary>
        protected static void CalibrateAnalyzer(PageFixture page, string channelName, string probeSerial = "12345678", int channel = 99)
            {
            page.Analyzer.SetupGet(s => s.IsDeviceConnected).Returns(true);
            page.Analyzer.Setup(s => s.CalibrateZeroAsync()).ReturnsAsync(true);
            page.Analyzer.SetupGet(s => s.ProbeSN).Returns(probeSerial);
            page.Analyzer.SetupGet(s => s.CurrentChannel).Returns(channel);
            page.Analyzer.SetupGet(s => s.Model).Returns(ColorAnalyzerModel.CA410);
            page.Analyzer.SetupGet(s => s.ChannelName).Returns(channelName);
            page.Registry.Setup(r => r.GetLastChannel(probeSerial)).Returns((int?)null);
            ((RelayCommand)page.ViewModel.ZeroCalibrationCommand).ExecuteAsync(null).GetAwaiter().GetResult();
            }

        /// <summary>
        /// Makes every row of the Results table hold the longest text its columns can get: the widest coordinates, the luminance and
        /// temperature with the most digits (the black point shows four decimals) and the attempts counter of a point measured over and over.
        /// </summary>
        protected static void FillResultsWithLongestValues(MeasurementViewModel viewModel)
            {
            foreach (var row in viewModel.Results)
                {
                row.IsAvailable = true;
                bool isBlack = row.Location == MeasurementLocation.BlackColor.ToString();
                row.Show(new Measurement { Location = row.Location, x = 0.888, y = 0.888, Lv = isBlack ? 9.9999 : 99999, T = 99999 });
                row.ShowAttempts(new SpecAttemptInfo(Count: 99, Best: 0, Deviated: true));
                }
            }
        }
    }

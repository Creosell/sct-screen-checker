using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ScreenChecker.ViewModels;
using ScreenChecker.ViewModels.SettingsPage;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// A drop-down list and a context menu are windows of their own and do not take the scale the window draws its content with: for every
    /// interface size of the settings the lists must be drawn at that size, like the page they belong to.
    /// </summary>
    [TestFixture]
    [Category("Ui")]
    public class PopupScaleTests : UiTestHost
        {
        private static IEnumerable<SettingsViewModel.InterfaceScaleOption> InterfaceSizes => SettingsViewModel.AvailableInterfaceScales;

        private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
            {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
                {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match) yield return match;
                foreach (var nested in Descendants<T>(child)) yield return nested;
                }
            }

        /// <summary>The scale a popup root draws its content with (1 without a transform).</summary>
        private static double ScaleOf(FrameworkElement element) => (element.LayoutTransform as ScaleTransform)?.ScaleX ?? 1;

        [Test]
        public void DropDownListsAndMenus_AreDrawnAtTheInterfaceSize()
            {
            Ui(() =>
                {
                UseLanguage("");
                var problems = new List<string>();
                using var page = CreatePage();
                var settings = CreateSettings(page.Analyzer);
                CalibrateAnalyzer(page, "WWWWWWWWWW");
                UiMetrics.WorkAreaOverride = new Size(6000, 4000);
                var window = ShowMainWindow(page, settings, sizeFromScreen: true);
                try
                    {
                    var root = (FrameworkElement)window.Content;
                    var navigate = ((MainWindowViewModel)window.DataContext).NavigateCommand;

                    foreach (var size in InterfaceSizes)
                        {
                        settings.SelectedInterfaceScale = size;
                        double factor = size.Factor;

                        navigate.Execute("Settings");
                        Flush(window);
                        foreach (var combo in Descendants<ComboBox>(root).Where(c => c.IsVisible))
                            {
                            combo.IsDropDownOpen = true;
                            Flush(window);
                            var popup = (Popup)combo.Template.FindName("PART_Popup", combo);
                            if (popup.Child is not FrameworkElement list || Math.Abs(ScaleOf(list) - factor) > 0.001)
                                problems.Add($"[interface {size.DisplayName}]: the list of a drop-down of the settings is drawn at {(popup.Child as FrameworkElement is { } shown ? ScaleOf(shown) : 0):F2}, not {factor:F2}");
                            combo.IsDropDownOpen = false;
                            }

                        navigate.Execute("Measurement");
                        Flush(window);
                        var configurationButton = Descendants<Button>(root).FirstOrDefault(b => b.ContextMenu != null);
                        if (configurationButton == null)
                            {
                            problems.Add($"[interface {size.DisplayName}]: the button of the device configuration is not found");
                            continue;
                            }
                        var menu = configurationButton.ContextMenu!;
                        menu.PlacementTarget = configurationButton;
                        menu.IsOpen = true;
                        Flush(window);
                        if (Math.Abs(ScaleOf(menu) - factor) > 0.001)
                            problems.Add($"[interface {size.DisplayName}]: the menu of the device configurations is drawn at {ScaleOf(menu):F2}, not {factor:F2}");
                        menu.IsOpen = false;
                        }
                    }
                finally
                    {
                    window.Close();
                    settings.SelectedInterfaceScale = SettingsViewModel.AvailableInterfaceScales[0];
                    UiMetrics.WorkAreaOverride = null;
                    }

                Assert.That(problems, Is.Empty, Environment.NewLine + string.Join(Environment.NewLine, problems));
                });
            }

        [Test]
        public void ToolTips_AreDrawnAtTheInterfaceSize()
            {
            Ui(() =>
                {
                UseLanguage("");
                var problems = new List<string>();
                var window = new Window { Width = 200, Height = 100, WindowStyle = WindowStyle.None, ShowInTaskbar = false, Left = -20000, Top = -20000, Content = new Button { Content = "x", ToolTip = "tip" } };
                window.Show();
                try
                    {
                    foreach (var size in InterfaceSizes)
                        {
                        UiMetrics.UiScale = size.Factor;
                        var tip = new ToolTip { Content = "tip", PlacementTarget = (UIElement)window.Content, IsOpen = true };
                        Flush(window);
                        if (Math.Abs(ScaleOf(tip) - size.Factor) > 0.001)
                            problems.Add($"[interface {size.DisplayName}]: a tooltip is drawn at {ScaleOf(tip):F2}, not {size.Factor:F2}");
                        tip.IsOpen = false;
                        }
                    }
                finally
                    {
                    window.Close();
                    UiMetrics.UiScale = 1;
                    }

                Assert.That(problems, Is.Empty, Environment.NewLine + string.Join(Environment.NewLine, problems));
                });
            }
        }
    }

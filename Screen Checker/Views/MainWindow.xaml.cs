using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace ScreenChecker.Views
{
    /// <summary>
    /// Main application window with custom title bar controls.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly double _designMinWidth;
        private readonly double _designMinHeight;

        /// <summary>
        /// Initializes a new instance of MainWindow.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _designMinWidth = MinWidth;
            _designMinHeight = MinHeight;
            FitToScreen();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Title = $"Screen Checker {version?.Major}.{version?.Minor}.{version?.Build}";
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WindowProc);
            FitToScreen();
        }

        /// <inheritdoc />
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            UpdateMinimumSize();
        }

        /// <inheritdoc />
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            UpdateMinimumSize();
        }

        /// <summary>
        /// Answers the system when it asks where a maximized window goes and how small the window may become: the work area of its
        /// monitor and the smallest size in physical pixels. A window without a frame is otherwise maximized over the taskbar and beyond
        /// the edges of the monitor, and its title buttons are partly off the screen.
        /// </summary>
        private IntPtr WindowProc(IntPtr window, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WindowMonitor.WmGetMinMaxInfo)
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                handled = WindowMonitor.FillMaximizedBounds(window, lParam, (int)Math.Ceiling(MinWidth * dpi.DpiScaleX), (int)Math.Ceiling(MinHeight * dpi.DpiScaleY));
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Sets the smallest size of the window: the design size of the pages, but never more than the work area of the monitor the window
        /// is on (the monitors differ in resolution and in display scale, so it is asked again when the window moves).
        /// </summary>
        private void UpdateMinimumSize()
        {
            var area = UiMetrics.WorkAreaOf(new WindowInteropHelper(this).Handle);
            MinWidth = Math.Min(_designMinWidth * UiMetrics.UiScale, area.Width);
            MinHeight = Math.Min(_designMinHeight * UiMetrics.UiScale, area.Height);
        }

        /// <summary>
        /// Keeps the window within the screen: the smallest size the window allows (and the size it starts with) is the design size of
        /// the pages, but never more than the work area. On a screen with a large display scale the work area is smaller than the design
        /// size, and the content is shrunk to it by the <see cref="MinimumSizeViewbox"/> that holds it.
        /// </summary>
        private void FitToScreen()
        {
            UpdateMinimumSize();
            var area = UiMetrics.WorkAreaOf(new WindowInteropHelper(this).Handle);
            Width = Math.Min(Math.Max(Width, MinWidth), area.Width);
            Height = Math.Min(Math.Max(Height, MinHeight), area.Height);

#if DEBUG
            // A larger interface for a look at what a display with a larger scale shows: the window takes the same share of the screen,
            // its content is laid out in less room and drawn larger
            if (UiMetrics.UiScale != 1 && Content is FrameworkElement content)
                content.LayoutTransform = new ScaleTransform(UiMetrics.UiScale, UiMetrics.UiScale);
#endif
        }

        /// <summary>
        /// Handles minimize button click to minimize the window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void MinimizeImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles maximize/restore button click to toggle window state between maximized and normal.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void MaximizeRestoreImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// Cancels closing when the operator declines to discard unsaved measurements.
        /// </summary>
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is ScreenChecker.ViewModels.MainWindowViewModel viewModel && !viewModel.ConfirmClose())
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Handles close button click to close the window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void CloseImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles title bar mouse down events for window dragging and double-click maximize/restore.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void Grid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    try
                    {
                        this.DragMove();
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore exception if DragMove cannot be called
                    }
                }
                if (e.ClickCount == 2)
                {
                    MaximizeRestoreImage_PreviewMouseLeftButtonDown(sender, e);
                }
            }
        }
    }
}
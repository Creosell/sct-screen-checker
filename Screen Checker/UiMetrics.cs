using System.Windows;
using System.Windows.Media;

namespace ScreenChecker
    {
    /// <summary>
    /// What the windows of the application need to know about the screen they are shown on, in device-independent pixels (the pixels
    /// of the layout: the real ones divided by the display scale of the system). The screen is the same for the whole run, so the
    /// windows read it here instead of each asking the system, and the tests can show the application on any screen.
    /// </summary>
    internal static class UiMetrics
        {
        /// <summary>
        /// Gets or sets the work area (the screen without the taskbar) the windows take instead of the real one. The layout tests set it
        /// to the work area of a screen with a display scale of 150%, 200% and so on.
        /// </summary>
        internal static Size? WorkAreaOverride { get; set; }

        private static double _uiScale = 1;

        /// <summary>
        /// Occurs when <see cref="UiScale"/> changes; the windows draw their content at the new size.
        /// </summary>
        internal static event Action? UiScaleChanged;

        /// <summary>
        /// Gets or sets how much larger than the system makes it the interface is drawn (1: as the system does). It is the interface size
        /// of the settings; the Debug build can also set it (<c>--ui-scale=2</c>), to see what a display with a scale of 200% shows
        /// without changing the settings of Windows.
        /// </summary>
        internal static double UiScale
            {
            get => _uiScale;
            set
                {
                if (_uiScale == value) return;
                _uiScale = value;
                UiScaleChanged?.Invoke();
                }
            }

        /// <summary>
        /// Draws the content of a window <see cref="UiScale"/> times larger and keeps doing so while the scale changes; the content
        /// is laid out in less room, so the window takes the same share of the screen.
        /// </summary>
        /// <param name="window">The window whose content is scaled.</param>
        internal static void ScaleContentOf(Window window)
            {
            void Apply()
                {
                if (window.Content is FrameworkElement content)
                    content.LayoutTransform = _uiScale == 1 ? Transform.Identity : new ScaleTransform(_uiScale, _uiScale);
                }

            Apply();
            UiScaleChanged += Apply;
            window.Closed += (_, _) => UiScaleChanged -= Apply;
            }

        /// <summary>
        /// Scales a dialog: its content is drawn <see cref="UiScale"/> times larger and the largest size it may take grows with it, but
        /// stays within the work area. A dialog is short-lived, so the scale is read once.
        /// </summary>
        /// <param name="dialog">The dialog to scale.</param>
        /// <param name="maxWidth">The largest width at scale 1.</param>
        /// <param name="maxHeight">The largest height at scale 1.</param>
        internal static void ScaleDialog(Window dialog, double maxWidth, double maxHeight)
            {
            if (dialog.Content is FrameworkElement content && _uiScale != 1)
                content.LayoutTransform = new ScaleTransform(_uiScale, _uiScale);
            dialog.MaxWidth = Math.Min(maxWidth * _uiScale, WorkArea.Width - 40);
            dialog.MaxHeight = Math.Min(maxHeight * _uiScale, WorkArea.Height - 40);
            }

        /// <summary>
        /// Gets the work area of the screen in device-independent pixels of the system (what a window may take on the screen).
        /// </summary>
        internal static Size WorkArea => WorkAreaOverride ?? new Size(SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);

        /// <summary>
        /// Gets the work area of the monitor a window is on, in device-independent pixels of that monitor (the monitors differ in resolution
        /// and in display scale); the work area the tests set, if any, or that of the system when the window has no monitor yet.
        /// </summary>
        /// <param name="window">The handle of the window, zero before it is created.</param>
        internal static Size WorkAreaOf(IntPtr window) =>
            WorkAreaOverride ?? (Views.WindowMonitor.TryGetWorkArea(window, out var area) ? area : WorkArea);

        /// <summary>
        /// Gets the work area in the pixels of the layout: what the interface has when it is drawn <see cref="UiScale"/> times larger.
        /// </summary>
        internal static Size WorkAreaOfLayout => new(WorkArea.Width / UiScale, WorkArea.Height / UiScale);
        }
    }

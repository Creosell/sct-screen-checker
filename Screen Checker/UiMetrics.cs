using System.Windows;

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

        /// <summary>
        /// Gets or sets how much larger than the system makes it the interface is drawn (1: as the system does). Only the Debug build sets it
        /// (<c>--ui-scale=2</c>), to see what a display with a scale of 200% shows without changing the settings of Windows.
        /// </summary>
        internal static double UiScale { get; set; } = 1;

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

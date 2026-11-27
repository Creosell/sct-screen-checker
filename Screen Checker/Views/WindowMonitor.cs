using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenChecker.Views
    {
    /// <summary>
    /// What a window needs to know about the monitor it is on: the work area of that monitor in device-independent pixels (not that of the
    /// primary one: the monitors differ in resolution and in display scale) and the bounds a maximized window takes (a window without a frame
    /// is otherwise maximized over the taskbar and 10 pixels beyond the monitor on every side).
    /// </summary>
    internal static class WindowMonitor
        {
        /// <summary>Message of the system that asks a window for its position and size when maximized.</summary>
        internal const int WmGetMinMaxInfo = 0x0024;

        private const int MonitorDefaultToNearest = 2;
        private const int EffectiveDpi = 0;
        private const double BaseDpi = 96;

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect32
            {
            public int Left, Top, Right, Bottom;
            }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point32
            {
            public int X, Y;
            }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
            {
            public Point32 Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize;
            }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
            {
            public int Size;
            public Rect32 Monitor, WorkArea;
            public int Flags;
            }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);

        /// <summary>
        /// Gets where a maximized window goes on a monitor: the work area (the monitor without the taskbar), as an offset from the top left
        /// corner of the monitor and a size, in physical pixels.
        /// </summary>
        /// <param name="monitor">The whole monitor.</param>
        /// <param name="workArea">The monitor without the taskbar.</param>
        /// <returns>The position relative to the monitor and the size.</returns>
        internal static (int X, int Y, int Width, int Height) MaximizedBounds(Rect32 monitor, Rect32 workArea) =>
            (workArea.Left - monitor.Left, workArea.Top - monitor.Top, workArea.Right - workArea.Left, workArea.Bottom - workArea.Top);

        /// <summary>
        /// Gets the work area of the monitor a window is on, in device-independent pixels of that monitor.
        /// </summary>
        /// <param name="window">The handle of the window.</param>
        /// <param name="area">The work area; the work area of the system when the monitor is not known.</param>
        /// <returns>True when the monitor of the window was found.</returns>
        internal static bool TryGetWorkArea(IntPtr window, out Size area)
            {
            area = new Size(SystemParameters.WorkArea.Width, SystemParameters.WorkArea.Height);
            if (window == IntPtr.Zero) return false;

            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return false;

            double scale = GetDpiForMonitor(monitor, EffectiveDpi, out uint dpi, out _) == 0 && dpi > 0 ? dpi / BaseDpi : 1;
            area = new Size((info.WorkArea.Right - info.WorkArea.Left) / scale, (info.WorkArea.Bottom - info.WorkArea.Top) / scale);
            return true;
            }

        /// <summary>
        /// Answers the question of the system what a maximized window takes (the work area of its monitor) and how small it may become.
        /// The whole answer is given here: left to the framework, it computes the sizes from its own idea of the display scale and the
        /// window comes out larger than the monitor.
        /// </summary>
        /// <param name="window">The handle of the window.</param>
        /// <param name="minMaxInfo">The pointer the system passed with the message.</param>
        /// <param name="minTrackWidth">The smallest width of the window in physical pixels.</param>
        /// <param name="minTrackHeight">The smallest height of the window in physical pixels.</param>
        /// <returns>True when the answer was given.</returns>
        internal static bool FillMaximizedBounds(IntPtr window, IntPtr minMaxInfo, int minTrackWidth, int minTrackHeight)
            {
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return false;

            var bounds = MaximizedBounds(info.Monitor, info.WorkArea);
            var minMax = Marshal.PtrToStructure<MinMaxInfo>(minMaxInfo);
            minMax.MaxPosition = new Point32 { X = bounds.X, Y = bounds.Y };
            minMax.MaxSize = new Point32 { X = bounds.Width, Y = bounds.Height };
            minMax.MaxTrackSize = minMax.MaxSize;
            minMax.MinTrackSize = new Point32 { X = minTrackWidth, Y = minTrackHeight };
            Marshal.StructureToPtr(minMax, minMaxInfo, fDeleteOld: true);
            return true;
            }
        }
    }

using System.Runtime.InteropServices;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// A display connected to the computer the tests run on.
    /// </summary>
    /// <param name="Name">What the messages call it: the resolution and the position on the desktop.</param>
    /// <param name="WorkWidth">The width of the work area (the display without the taskbar) in physical pixels.</param>
    /// <param name="WorkHeight">The height of the work area in physical pixels.</param>
    /// <param name="Scale">The display scale of the system on this display now (1.5 is 150%).</param>
    public sealed record DisplayInfo(string Name, double WorkWidth, double WorkHeight, double Scale);

    /// <summary>
    /// The displays that are really connected: the layout tests take their resolutions from them instead of from a list written
    /// into the tests, so a new display (an ultrawide, a 5K, a small tablet) is covered as soon as it is plugged in.
    /// </summary>
    public static class Displays
        {
        private static readonly IntPtr PerMonitorV2 = new(-4);

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr dc, ref Rect32 bounds, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect32
            {
            public int Left, Top, Right, Bottom;
            }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
            {
            public int Size;
            public Rect32 Monitor, WorkArea;
            public int Flags;
            }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr dc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);

        /// <summary>
        /// Lists the connected displays with their physical work areas. The test process is not DPI aware, so the thread is made aware
        /// while it asks: otherwise the system answers with sizes already divided by the display scale.
        /// </summary>
        /// <returns>The displays; one full HD display without a taskbar when the system reports none (a session without a screen).</returns>
        public static IReadOnlyList<DisplayInfo> Connected()
            {
            var found = new List<DisplayInfo>();
            var previous = SetThreadDpiAwarenessContext(PerMonitorV2);
            try
                {
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr dc, ref Rect32 bounds, IntPtr data) =>
                    {
                    var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                    if (!GetMonitorInfo(monitor, ref info)) return true;

                    double scale = GetDpiForMonitor(monitor, 0, out uint dpi, out _) == 0 && dpi > 0 ? dpi / 96.0 : 1;
                    int width = info.Monitor.Right - info.Monitor.Left, height = info.Monitor.Bottom - info.Monitor.Top;
                    found.Add(new DisplayInfo(
                        $"{width}x{height} display at {info.Monitor.Left},{info.Monitor.Top}",
                        info.WorkArea.Right - info.WorkArea.Left,
                        info.WorkArea.Bottom - info.WorkArea.Top,
                        scale));
                    return true;
                    }, IntPtr.Zero);
                }
            finally
                {
                if (previous != IntPtr.Zero) SetThreadDpiAwarenessContext(previous);
                }

            return found.Count > 0 ? found : [new DisplayInfo("1920x1080 display (none connected)", 1920, 1040, 1)];
            }
        }
    }

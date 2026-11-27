using ScreenChecker.Views;

namespace ScreenChecker.UnitTests.ViewsTests
    {
    /// <summary>
    /// Where a maximized window goes: the work area of its monitor, so a window without a frame covers neither the taskbar nor goes beyond the
    /// edges of the monitor (it did, by 10 pixels on every side, and the title buttons were partly off the screen).
    /// </summary>
    [TestFixture]
    public class WindowMonitorTests
        {
        private static WindowMonitor.Rect32 Rect(int left, int top, int right, int bottom) => new() { Left = left, Top = top, Right = right, Bottom = bottom };

        [Test]
        public void MaximizedBounds_OfThePrimaryMonitor_AreTheWorkAreaWithoutTheTaskbar()
            {
            var bounds = WindowMonitor.MaximizedBounds(Rect(0, 0, 1920, 1200), Rect(0, 0, 1920, 1140));

            Assert.That(bounds, Is.EqualTo((0, 0, 1920, 1140)));
            }

        [Test]
        public void MaximizedBounds_OfAMonitorOnTheRightAboveTheBaseline_AreRelativeToThatMonitor()
            {
            // The 4K monitor of the lab: starts at 1920, -325 of the desktop; the system asks for the position inside its own monitor
            var bounds = WindowMonitor.MaximizedBounds(Rect(1920, -325, 5760, 1835), Rect(1920, -325, 5760, 1763));

            Assert.That(bounds, Is.EqualTo((0, 0, 3840, 2088)));
            }

        [Test]
        public void MaximizedBounds_WithTheTaskbarOnTheLeftOrTop_ShiftTheWindowByTheTaskbar()
            {
            var onTheLeft = WindowMonitor.MaximizedBounds(Rect(0, 0, 1920, 1080), Rect(48, 0, 1920, 1080));
            var onTop = WindowMonitor.MaximizedBounds(Rect(0, 0, 1920, 1080), Rect(0, 40, 1920, 1080));

            Assert.That(onTheLeft, Is.EqualTo((48, 0, 1872, 1080)));
            Assert.That(onTop, Is.EqualTo((0, 40, 1920, 1040)));
            }
        }
    }

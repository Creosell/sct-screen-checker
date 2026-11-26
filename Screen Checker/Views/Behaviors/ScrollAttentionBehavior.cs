using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ScreenChecker.Models;

namespace ScreenChecker.Views.Behaviors
{
    /// <summary>
    /// Attached behavior of a <see cref="ScrollViewer"/> that hosts an <see cref="ItemsControl"/>: it reports the strongest
    /// attention level of the rows that are scrolled out of view above and below, so the page can hint at them
    /// (<c>behaviors:ScrollAttentionBehavior.IsEnabled="True"</c>, rows carry the level through <see cref="LevelProperty"/>).
    /// </summary>
    public static class ScrollAttentionBehavior
    {
        private static readonly ConditionalWeakTable<ScrollViewer, Action> Updaters = new();

        /// <summary>Turns the tracking on for a scroll viewer.</summary>
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled", typeof(bool), typeof(ScrollAttentionBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>The attention level of a row container: only <see cref="SpecStatus.Warning"/> and <see cref="SpecStatus.Fail"/> count, any other status asks for nothing.</summary>
        public static readonly DependencyProperty LevelProperty = DependencyProperty.RegisterAttached(
            "Level", typeof(SpecStatus), typeof(ScrollAttentionBehavior), new PropertyMetadata(SpecStatus.Pass, OnLevelChanged));

        /// <summary>The strongest level among the rows above the visible area.</summary>
        public static readonly DependencyProperty AttentionAboveProperty = DependencyProperty.RegisterAttached(
            "AttentionAbove", typeof(SpecStatus), typeof(ScrollAttentionBehavior), new PropertyMetadata(SpecStatus.Pass));

        /// <summary>The strongest level among the rows below the visible area.</summary>
        public static readonly DependencyProperty AttentionBelowProperty = DependencyProperty.RegisterAttached(
            "AttentionBelow", typeof(SpecStatus), typeof(ScrollAttentionBehavior), new PropertyMetadata(SpecStatus.Pass));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static SpecStatus GetLevel(DependencyObject element) => (SpecStatus)element.GetValue(LevelProperty);

        public static void SetLevel(DependencyObject element, SpecStatus value) => element.SetValue(LevelProperty, value);

        public static SpecStatus GetAttentionAbove(DependencyObject element) => (SpecStatus)element.GetValue(AttentionAboveProperty);

        public static void SetAttentionAbove(DependencyObject element, SpecStatus value) => element.SetValue(AttentionAboveProperty, value);

        public static SpecStatus GetAttentionBelow(DependencyObject element) => (SpecStatus)element.GetValue(AttentionBelowProperty);

        public static void SetAttentionBelow(DependencyObject element, SpecStatus value) => element.SetValue(AttentionBelowProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scroll) return;

            Updaters.Remove(scroll);
            if (e.NewValue is not true) return;

            var pending = false;
            Action schedule = () =>
            {
                if (pending) return;
                pending = true;
                scroll.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
                {
                    pending = false;
                    Update(scroll);
                });
            };
            Updaters.Add(scroll, schedule);
            scroll.ScrollChanged += (_, _) => schedule();
            scroll.Loaded += (_, _) => schedule();
        }

        private static void OnLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (FindScrollViewer(d) is { } scroll && Updaters.TryGetValue(scroll, out var schedule)) schedule();
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject element)
        {
            for (var parent = VisualTreeHelper.GetParent(element); parent != null; parent = VisualTreeHelper.GetParent(parent))
                if (parent is ScrollViewer scroll) return scroll;

            return null;
        }

        /// <summary>
        /// Finds the strongest level among the rows that lie entirely above or below the visible area.
        /// </summary>
        private static void Update(ScrollViewer scroll)
        {
            if (scroll.Content is not ItemsControl items) return;

            var above = SpecStatus.Pass;
            var below = SpecStatus.Pass;
            for (var i = 0; i < items.Items.Count; i++)
            {
                if (items.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement row || !row.IsVisible || row.ActualHeight == 0) continue;

                var level = GetLevel(row);
                if (level is not (SpecStatus.Warning or SpecStatus.Fail)) continue;

                var top = row.TranslatePoint(new Point(0, 0), scroll).Y;
                if (top + row.ActualHeight <= 0) above = Max(above, level);
                else if (top >= scroll.ViewportHeight) below = Max(below, level);
            }

            SetAttentionAbove(scroll, above);
            SetAttentionBelow(scroll, below);
        }

        private static SpecStatus Max(SpecStatus first, SpecStatus second) => first >= second ? first : second;
    }
}

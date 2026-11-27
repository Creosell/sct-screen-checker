using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ScreenChecker.Views.Behaviors
    {
    /// <summary>
    /// Draws an element at the interface size of the settings. A popup (the list of a combo box, a context menu, a tooltip) is a window
    /// of its own and does not take the scale the window draws its content with, so the root of a popup asks for it here.
    /// </summary>
    internal static class UiScaled
        {
        private static readonly ConditionalWeakTable<FrameworkElement, Action> Handlers = new();

        /// <summary>Whether the element is drawn <see cref="UiMetrics.UiScale"/> times larger and follows the scale when it changes.</summary>
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(UiScaled), new PropertyMetadata(false, OnEnabledChanged));

        /// <summary>Gets whether the element follows the interface size.</summary>
        public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

        /// <summary>Sets whether the element follows the interface size.</summary>
        public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);

        private static void OnEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
            {
            if (sender is not FrameworkElement element) return;

            element.Loaded -= OnLoaded;
            element.Unloaded -= OnUnloaded;
            if ((bool)e.NewValue)
                {
                element.Loaded += OnLoaded;
                element.Unloaded += OnUnloaded;
                if (element.IsLoaded) Attach(element);
                }
            else
                {
                Detach(element);
                }
            }

        private static void OnLoaded(object sender, RoutedEventArgs e) => Attach((FrameworkElement)sender);

        private static void OnUnloaded(object sender, RoutedEventArgs e) => Detach((FrameworkElement)sender);

        private static void Attach(FrameworkElement element)
            {
            void Apply() => element.LayoutTransform = UiMetrics.UiScale == 1 ? Transform.Identity : new ScaleTransform(UiMetrics.UiScale, UiMetrics.UiScale);

            Detach(element);
            Apply();
            Handlers.Add(element, Apply);
            UiMetrics.UiScaleChanged += Apply;
            }

        private static void Detach(FrameworkElement element)
            {
            if (!Handlers.TryGetValue(element, out var apply)) return;
            UiMetrics.UiScaleChanged -= apply;
            Handlers.Remove(element);
            }
        }
    }

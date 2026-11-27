using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// Looks at everything written on a page, as the operator would, and reports what does not read: a text cut by its slot or by
    /// an ancestor, a text that runs out of the area it lives in, two texts on top of each other and a text another element lies over.
    /// It knows nothing about the page: it walks the visual tree of whatever is shown, so a new control is checked with no new test code.
    /// </summary>
    public static class LayoutAudit
        {
        /// <summary>Tolerance of the measurements in device-independent pixels (text metrics are not exact).</summary>
        private const double Tolerance = 0.75;

        /// <summary>The overlap of two texts below this area (square pixels) is the rounding of the glyph boxes, not an overlap.</summary>
        private const double MinOverlapArea = 6;

        /// <summary>An element that is this transparent or more is not seen, so its text is not checked.</summary>
        private const double InvisibleOpacity = 0.05;

        /// <summary>One visible text of the page with the space it takes in the coordinates of the audited root.</summary>
        private sealed record TextItem(TextBlock Block, string Text, Rect Rect, double NeededWidth, double NeededHeight, double SlotWidth, double SlotHeight);

        /// <summary>
        /// Audits the visible texts of a root element.
        /// </summary>
        /// <param name="root">The element the page lives in; it has to be laid out and shown.</param>
        /// <param name="culture">The culture the texts are measured in.</param>
        /// <param name="where">What is being audited (a language and a state of the page), the start of every message.</param>
        /// <param name="isAllowedToTrim">Tells which text the page trims with an ellipsis on purpose (a long free text).</param>
        /// <returns>One message per problem, empty when the page reads well.</returns>
        public static List<string> Run(FrameworkElement root, CultureInfo culture, string where, Func<TextBlock, bool>? isAllowedToTrim = null)
            {
            var problems = new List<string>();
            var items = Collect(root, culture);

            foreach (var item in items)
                {
                CheckSlot(item, isAllowedToTrim, problems);
                CheckAncestors(item, root, problems);
                CheckCovered(item, root, problems);
                }
            CheckOverlaps(items, problems);
            CheckTextBoxes(root, problems);

            return problems.Select(p => $"{where}: {p}").ToList();
            }

        /// <summary>Collects the texts that are shown: visible, opaque enough, with something written.</summary>
        private static List<TextItem> Collect(FrameworkElement root, CultureInfo culture)
            {
            var items = new List<TextItem>();
            foreach (var block in Descendants<TextBlock>(root))
                {
                if (!block.IsVisible || string.IsNullOrWhiteSpace(block.Text) || block.ActualWidth < 0.5 || block.ActualHeight < 0.5) continue;
                if (EffectiveOpacity(block, root) < InvisibleOpacity) continue;

                double slotWidth = block.ActualWidth - block.Padding.Left - block.Padding.Right;
                double slotHeight = block.ActualHeight - block.Padding.Top - block.Padding.Bottom;
                var text = new FormattedText(
                    block.Text, culture, FlowDirection.LeftToRight,
                    new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch),
                    block.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(block).PixelsPerDip);
                if (block.TextWrapping != TextWrapping.NoWrap) text.MaxTextWidth = Math.Max(1, slotWidth);

                double width = block.TextWrapping == TextWrapping.NoWrap ? text.WidthIncludingTrailingWhitespace : text.Width;
                double height = double.IsNaN(block.LineHeight) ? text.Height : text.Height * (block.LineHeight / (block.FontSize * block.FontFamily.LineSpacing));
                double shift = block.TextAlignment switch
                    {
                    TextAlignment.Center => (slotWidth - width) / 2,
                    TextAlignment.Right => slotWidth - width,
                    _ => 0
                    };
                var rect = block.TransformToAncestor(root).TransformBounds(new Rect(block.Padding.Left + shift, block.Padding.Top, width, height));
                if (IsScrolledOut(block, rect, root)) continue;
                items.Add(new TextItem(block, block.Text, rect, width, height, slotWidth, slotHeight));
                }
            return items;
            }

        /// <summary>
        /// Tells whether a text is not seen as a whole now because a scroll viewer around it shows another part: such a text is
        /// reached by scrolling, so it is checked when it is scrolled into view (the row tests do that).
        /// </summary>
        private static bool IsScrolledOut(DependencyObject block, Rect rect, FrameworkElement root)
            {
            for (var parent = VisualTreeHelper.GetParent(block); parent != null; parent = VisualTreeHelper.GetParent(parent))
                {
                if (parent is ScrollViewer scroll)
                    {
                    var viewport = scroll.TransformToAncestor(root).TransformBounds(new Rect(0, 0, scroll.ViewportWidth, scroll.ViewportHeight));
                    bool scrollsDown = scroll.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
                    bool scrollsAcross = scroll.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
                    if (scrollsDown && (rect.Top < viewport.Top - Tolerance || rect.Bottom > viewport.Bottom + Tolerance)) return true;
                    if (scrollsAcross && (rect.Left < viewport.Left - Tolerance || rect.Right > viewport.Right + Tolerance)) return true;
                    }
                if (ReferenceEquals(parent, root)) break;
                }
            return false;
            }

        /// <summary>A text has to fit the slot the layout gave it: otherwise the layout clips it or the text shows an ellipsis.</summary>
        private static void CheckSlot(TextItem item, Func<TextBlock, bool>? isAllowedToTrim, List<string> problems)
            {
            bool wraps = item.Block.TextWrapping != TextWrapping.NoWrap;
            bool tooWide = !wraps && item.NeededWidth > item.SlotWidth + Tolerance;
            bool tooHigh = item.NeededHeight > item.SlotHeight + 1.5;
            if (!tooWide && !tooHigh) return;
            if (isAllowedToTrim?.Invoke(item.Block) == true) return;

            string how = item.Block.TextTrimming != TextTrimming.None && tooWide ? "is trimmed with an ellipsis" : "is cut";
            problems.Add(tooWide
                ? $"{Describe(item)} {how}: it needs {item.NeededWidth:F1} px, its slot gives {item.SlotWidth:F1} px"
                : $"{Describe(item)} {how}: it needs {item.NeededHeight:F1} px of height, its slot gives {item.SlotHeight:F1} px");
            }

        /// <summary>
        /// A text has to lie inside every element it sits in. Along an axis a scroll viewer scrolls, the text may go beyond the viewer
        /// (it is reached by scrolling); along the other it must not.
        /// </summary>
        private static void CheckAncestors(TextItem item, FrameworkElement root, List<string> problems)
            {
            for (var parent = VisualTreeHelper.GetParent(item.Block); parent != null; parent = VisualTreeHelper.GetParent(parent))
                {
                if (parent is FrameworkElement element && element is not ScrollContentPresenter && element.ActualWidth > 0 && element.ActualHeight > 0 && element.IsVisible)
                    {
                    var toRoot = element.TransformToAncestor(root);
                    var bounds = toRoot.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                    bool scrollsAcross = false, scrollsDown = false;
                    if (element is ScrollViewer scroll)
                        {
                        bounds = toRoot.TransformBounds(new Rect(0, 0, scroll.ViewportWidth, scroll.ViewportHeight));
                        scrollsAcross = scroll.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
                        scrollsDown = scroll.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
                        }

                    var rect = item.Rect;
                    bool outsideAcross = !scrollsAcross && (rect.Left < bounds.Left - Tolerance || rect.Right > bounds.Right + Tolerance);
                    bool outsideDown = !scrollsDown && (rect.Top < bounds.Top - Tolerance || rect.Bottom > bounds.Bottom + Tolerance);
                    if (outsideAcross || outsideDown)
                        {
                        problems.Add($"{Describe(item)} lies outside {Describe(element)}: the text is {Format(rect)}, the element is {Format(bounds)}");
                        return;
                        }
                    }
                if (ReferenceEquals(parent, root)) return;
                }
            }

        /// <summary>No other element may lie over a text: the hit test at the left end, the middle and the right end must reach the text or the elements around it.</summary>
        private static void CheckCovered(TextItem item, FrameworkElement root, List<string> problems)
            {
            double y = item.Rect.Top + (item.Rect.Height / 2);
            foreach (double x in new[] { item.Rect.Left + 1, item.Rect.Left + (item.Rect.Width / 2), item.Rect.Right - 1 })
                {
                if (x < 0 || y < 0 || x > root.ActualWidth || y > root.ActualHeight) continue;
                var hit = VisualTreeHelper.HitTest(root, new Point(x, y))?.VisualHit;
                if (hit != null && IsShown(hit) && !AreRelated(hit, item.Block))
                    {
                    problems.Add($"{Describe(item)} is covered by {Chain(hit)} at {x:F0},{y:F0}");
                    return;
                    }
                }
            }

        /// <summary>
        /// A single-line text box shows its whole text: when the text is longer than the box the operator has to scroll inside the
        /// field to read it. A text box that wraps or scrolls on purpose (the log) is left alone.
        /// </summary>
        private static void CheckTextBoxes(FrameworkElement root, List<string> problems)
            {
            foreach (var box in Descendants<TextBox>(root))
                {
                if (!box.IsVisible || box.AcceptsReturn || box.TextWrapping != TextWrapping.NoWrap || string.IsNullOrEmpty(box.Text)) continue;
                if (box.ExtentWidth > box.ViewportWidth + Tolerance)
                    problems.Add($"text box {(string.IsNullOrEmpty(box.Name) ? PathOf(box) : box.Name)} \"{box.Text}\" does not fit: it needs {box.ExtentWidth:F1} px, the field shows {box.ViewportWidth:F1} px");
                }
            }

        /// <summary>Two texts must not take the same place.</summary>
        private static void CheckOverlaps(List<TextItem> items, List<string> problems)
            {
            for (int i = 0; i < items.Count; i++)
                for (int j = i + 1; j < items.Count; j++)
                    {
                    var common = Rect.Intersect(items[i].Rect, items[j].Rect);
                    if (common.IsEmpty || common.Width * common.Height < MinOverlapArea) continue;
                    problems.Add($"{Describe(items[i])} overlaps {Describe(items[j])} ({common.Width:F1} x {common.Height:F1} px)");
                    }
            }

        /// <summary>The hit test also reaches the visual of a hidden page (the page not switched to): what is not shown covers nothing.</summary>
        private static bool IsShown(DependencyObject element)
            {
            for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
                if (current is UIElement ui) return ui.IsVisible;
            return true;
            }

        /// <summary>
        /// A hit belongs to the text when it is the text, its parts or its surroundings, or a part of the control the text lives in
        /// (the toggle of a combo box lies over the text of the box on purpose: it takes the click).
        /// </summary>
        private static bool AreRelated(DependencyObject hit, DependencyObject block)
            {
            if (IsInside(hit, block) || IsInside(block, hit)) return true;
            for (var current = VisualTreeHelper.GetParent(block); current != null; current = VisualTreeHelper.GetParent(current))
                if (current is Control owner) return IsInside(hit, owner);
            return false;
            }

        private static bool IsInside(DependencyObject element, DependencyObject container)
            {
            for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
                if (ReferenceEquals(current, container)) return true;
            return false;
            }

        private static double EffectiveOpacity(DependencyObject element, DependencyObject root)
            {
            double opacity = 1;
            for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
                {
                if (current is UIElement ui) opacity *= ui.Opacity;
                if (ReferenceEquals(current, root)) break;
                }
            return opacity;
            }

        /// <summary>Names a text for a message: what is written and the elements it sits in.</summary>
        private static string Describe(TextItem item)
            {
            string text = item.Text.Length > 40 ? item.Text[..40] + "…" : item.Text;
            return $"\"{text}\" ({PathOf(item.Block)})";
            }

        private static string Describe(FrameworkElement element) => $"{element.GetType().Name}{(string.IsNullOrEmpty(element.Name) ? "" : " " + element.Name)}";

        /// <summary>The nearest named elements above a text, from the closest, to find it in the XAML.</summary>
        private static string PathOf(DependencyObject element)
            {
            var path = new List<string>();
            for (var current = VisualTreeHelper.GetParent(element); current != null && path.Count < 4; current = VisualTreeHelper.GetParent(current))
                if (current is FrameworkElement { Name.Length: > 0 } named) path.Add(named.Name);
                else if (current is Button or CheckBox or ComboBox or TabItem or MenuItem or TextBox) path.Add(current.GetType().Name);
            return path.Count == 0 ? "page" : string.Join(" < ", path);
            }

        /// <summary>The kinds of the elements from an element up to the root of the page, to tell what lies over a text.</summary>
        private static string Chain(DependencyObject element)
            {
            var kinds = new List<string>();
            for (var current = element; current != null && kinds.Count < 9; current = VisualTreeHelper.GetParent(current))
                kinds.Add(current is FrameworkElement { Name.Length: > 0 } named ? $"{current.GetType().Name} {named.Name}" : current.GetType().Name);
            return string.Join(" < ", kinds);
            }

        private static string Format(Rect rect) => $"{rect.Left:F1}..{rect.Right:F1} x {rect.Top:F1}..{rect.Bottom:F1}";

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
        }
    }

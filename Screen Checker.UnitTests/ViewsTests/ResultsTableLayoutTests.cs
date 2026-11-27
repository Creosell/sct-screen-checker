using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenChecker.UnitTests.ViewsTests.Layout;
using ScreenChecker.ViewModels.MeasurementPage;
using ScreenChecker.Views;

namespace ScreenChecker.UnitTests.ViewsTests
    {
    /// <summary>
    /// Draws the real measurement page in an off-screen window of the smallest size the application allows, fills every row of the
    /// Results table with the longest values the analyzer can give and checks what the operator sees in every language: no text is
    /// cut by its cell, no text runs into the next cell or out of the list, no text is covered by another element, and the header
    /// of a column sits above its values.
    /// </summary>
    [TestFixture]
    [Category("Ui")]
    public class ResultsTableLayoutTests : UiTestHost
        {
        /// <summary>Gap in device-independent pixels the texts of two neighbor cells must keep between each other.</summary>
        private const double MinGap = 1;

        /// <summary>Tolerance of the text measurement in device-independent pixels.</summary>
        private const double Tolerance = 0.5;

        /// <summary>Number of columns of the table.</summary>
        private const int ColumnCount = 7;

        /// <summary>
        /// Room in device-independent pixels a text keeps to the edge of the list. A layout that rounds the size of a column to whole
        /// device pixels sets it at most one pixel wider, so a row of <see cref="ColumnCount"/> columns can grow by that many pixels;
        /// a text that touches the edge of the list has no room for that and runs under the scroll bar.
        /// </summary>
        private const double RoundingAllowance = ColumnCount;

        /// <summary>Resource key of the header of the first column, a text of the language.</summary>
        private const string PointHeaderKey = "ResultsColumnPoint";

        /// <summary>One text of the table with what the checks need to know about it.</summary>
        private sealed record Cell(string Column, TextBlock Block, string Text, double Left, double Width, double Needed);

        [TestCase("", TestName = "ResultsTable_LongestRow_IsShownWhole_English")]
        [TestCase("ru", TestName = "ResultsTable_LongestRow_IsShownWhole_Russian")]
        [TestCase("zh-Hans", TestName = "ResultsTable_LongestRow_IsShownWhole_Chinese")]
        public void ResultsTable_LongestRow_IsShownWhole(string culture)
            {
            Ui(() =>
                {
                var problems = new List<string>();
                UseLanguage(culture);
                var cultureInfo = CultureOf(culture);

                using var page = CreatePage();
                FillResultsWithLongestValues(page.ViewModel);

                // The smallest window and windows stretched over larger screens, short ones (the list scrolls, a scroll bar takes room) and tall ones
                foreach (double widthFactor in new[] { 1, 1.25, 1.5, 2, 3 })
                    foreach (double heightFactor in new[] { 1, 2 })
                        CheckTable(page, cultureInfo, widthFactor, heightFactor, problems);

                Assert.That(problems, Is.Empty, $"[{LanguageName(culture)}]{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
                });
            }

        [TestCase("", TestName = "ResultsTable_HeaderColumns_MatchTheRows_InTheMainWindow_English")]
        [TestCase("ru", TestName = "ResultsTable_HeaderColumns_MatchTheRows_InTheMainWindow_Russian")]
        [TestCase("zh-Hans", TestName = "ResultsTable_HeaderColumns_MatchTheRows_InTheMainWindow_Chinese")]
        public void ResultsTable_HeaderColumns_MatchTheRows_InTheMainWindow(string culture)
            {
            Ui(() =>
                {
                UseLanguage(culture);
                using var page = CreatePage();
                var settings = CreateSettings(page.Analyzer);
                FillResultsWithLongestValues(page.ViewModel);
                var problems = new List<string>();

                foreach (var room in new[] { new Size(1100, 600), new Size(1536, 912) })
                    {
                    UiMetrics.WorkAreaOverride = room;
                    var window = ShowMainWindow(page, settings, sizeFromScreen: true);
                    try
                        {
                        Pump(100);
                        Flush(window);
                        var tables = Descendants<Grid>((FrameworkElement)window.Content).Where(g => g.ColumnDefinitions.Count == 7).ToList();
                        var headerGrid = tables.First();
                        var rowGrid = tables.Skip(1).First();
                        for (int c = 0; c < 7; c++)
                            {
                            double header = headerGrid.ColumnDefinitions[c].ActualWidth, row = rowGrid.ColumnDefinitions[c].ActualWidth;
                            if (Math.Abs(header - row) > 1)
                                problems.Add($"[{room.Width:F0}x{room.Height:F0}] column {c}: the header gives {header:F1} px, the rows {row:F1} px");
                            }
                        }
                    finally
                        {
                        window.Close();
                        UiMetrics.WorkAreaOverride = null;
                        }
                    }

                Assert.That(problems, Is.Empty, $"[{LanguageName(culture)}]{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
                });
            }

        /// <summary>Shows the page at a window size and checks every row of the table.</summary>
        private static void CheckTable(PageFixture page, CultureInfo cultureInfo, double widthFactor, double heightFactor, List<string> problems)
            {
            var view = new MeasurementView { DataContext = page.ViewModel };
            var window = ShowPage(view, widthFactor, heightFactor);
            var found = new List<string>();
            try
                {
                var scroll = (ScrollViewer)view.FindName("ResultsScroll");
                Assert.That(scroll, Is.Not.Null, "the scroll of the Results table is not found");
                var items = (ItemsControl)scroll.Content;
                Assert.That(items.Items.Count, Is.EqualTo(page.ViewModel.Results.Count));
                var root = (UIElement)window.Content;

                var header = FindHeaderCells(view, root, cultureInfo);
                CheckRowTexts("header", header, scroll, root, found);

                for (int i = 0; i < items.Items.Count; i++)
                    {
                    var row = (MeasurementResultRow)items.Items[i];
                    var container = (FrameworkElement)items.ItemContainerGenerator.ContainerFromIndex(i);
                    container.BringIntoView();
                    window.UpdateLayout();

                    var cells = FindRowCells(container, root, cultureInfo);
                    string name = $"row \"{row.Name}\"";
                    CheckRowTexts(name, cells, scroll, root, found);
                    CheckUnderHeader(name, header, cells, found);
                    CheckNotCovered(name, cells, container, root, found);
                    }

                scroll.ScrollToEnd();
                Flush(window);
                Shot(view, $"results scrolled to the end, window {widthFactor}x{heightFactor}");
                }
            finally
                {
                window.Close();
                }

            problems.AddRange(found.Select(problem => $"[window {widthFactor}x wide, {heightFactor}x tall] {problem}"));
            }

        /// <summary>Finds the texts of the header of the table: the grid that holds the localized name of the first column.</summary>
        private static List<Cell> FindHeaderCells(DependencyObject view, UIElement root, CultureInfo culture)
            {
            string pointHeader = (string)Application.Current.FindResource(PointHeaderKey);
            var title = Descendants<TextBlock>(view).FirstOrDefault(t => t.Text == pointHeader && t.Parent is Grid);
            Assert.That(title, Is.Not.Null, $"the header \"{pointHeader}\" of the table is not found");
            return CellsOf((Grid)title!.Parent, root, culture);
            }

        /// <summary>Finds the texts of a row of the table: the grid with the seven columns of its template.</summary>
        private static List<Cell> FindRowCells(DependencyObject container, UIElement root, CultureInfo culture)
            {
            var grid = Descendants<Grid>(container).FirstOrDefault(g => g.ColumnDefinitions.Count == 7);
            Assert.That(grid, Is.Not.Null, "the grid of a row is not found");
            return CellsOf(grid!, root, culture);
            }

        private static List<Cell> CellsOf(Grid grid, UIElement root, CultureInfo culture)
            {
            var cells = new List<Cell>();
            foreach (var block in grid.Children.OfType<TextBlock>().Where(b => b.Visibility == Visibility.Visible && Grid.GetColumn(b) >= 1))
                {
                double needed = new FormattedText(
                    block.Text, culture, FlowDirection.LeftToRight,
                    new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch),
                    block.FontSize, Brushes.White, VisualTreeHelper.GetDpi(block).PixelsPerDip).WidthIncludingTrailingWhitespace;
                double left = block.TranslatePoint(new Point(0, 0), root).X;
                // A centered text grows to both sides of its cell, any other one to the right
                double textLeft = block.TextAlignment == TextAlignment.Center ? left + ((block.ActualWidth - needed) / 2) : left;
                cells.Add(new Cell(ColumnName(grid, block), block, block.Text, textLeft, block.ActualWidth, needed));
                }
            return cells;
            }

        private static string ColumnName(Grid grid, TextBlock block) =>
            Grid.GetColumn(block) switch { 1 => "point", 2 => "x", 3 => "y", 4 => "Lv", 5 => "T", 6 => "tries", var other => $"column {other}" };

        /// <summary>
        /// Checks the texts of one row: each fits its cell, each stays inside the list, and two neighbors keep a gap between them.
        /// </summary>
        private static void CheckRowTexts(string where, List<Cell> cells, ScrollViewer scroll, UIElement root, List<string> problems)
            {
            double listLeft = scroll.TranslatePoint(new Point(0, 0), root).X;
            double listRight = listLeft + scroll.ViewportWidth;
            foreach (var cell in cells)
                {
                if (cell.Needed > cell.Width + Tolerance)
                    problems.Add($"{where}, {cell.Column}: \"{cell.Text}\" needs {cell.Needed:F1} px but its cell gives {cell.Width:F1} px (cut)");
                if (cell.Left < listLeft - Tolerance || cell.Left + cell.Needed + RoundingAllowance > listRight + Tolerance)
                    problems.Add($"{where}, {cell.Column}: \"{cell.Text}\" runs out of the list or leaves less than {RoundingAllowance:F0} px to its edge ({cell.Left:F1}..{cell.Left + cell.Needed:F1} px, the list is {listLeft:F1}..{listRight:F1} px)");
                }

            var ordered = cells.OrderBy(c => c.Left).ToList();
            for (int i = 1; i < ordered.Count; i++)
                {
                double gap = ordered[i].Left - (ordered[i - 1].Left + ordered[i - 1].Needed);
                if (gap < MinGap)
                    problems.Add($"{where}: \"{ordered[i - 1].Text}\" ({ordered[i - 1].Column}) and \"{ordered[i].Text}\" ({ordered[i].Column}) are {gap:F1} px apart (they touch or overlap)");
                }
            }

        /// <summary>Checks that the header of a column is above its values: the centers of both texts are close.</summary>
        private static void CheckUnderHeader(string where, List<Cell> header, List<Cell> cells, List<string> problems)
            {
            foreach (var cell in cells.Where(c => c.Column != "point"))
                {
                var title = header.FirstOrDefault(h => h.Column == cell.Column);
                if (title == null) continue;
                double shift = Math.Abs((title.Left + (title.Needed / 2)) - (cell.Left + (cell.Needed / 2)));
                if (shift > 3)
                    problems.Add($"{where}, {cell.Column}: the value \"{cell.Text}\" is {shift:F1} px off the center of its header \"{title.Text}\"");
                }
            }

        /// <summary>
        /// Checks that nothing lies over the texts of a row: the two ends of every text, taken at its vertical center, belong to the
        /// text itself or to the row, not to another element (a hint, a scroll bar, an overlay).
        /// </summary>
        private static void CheckNotCovered(string where, List<Cell> cells, DependencyObject container, UIElement root, List<string> problems)
            {
            foreach (var cell in cells)
                {
                var topLeft = cell.Block.TranslatePoint(new Point(0, 0), root);
                double y = topLeft.Y + (cell.Block.ActualHeight / 2);
                foreach (double x in new[] { cell.Left + 0.5, cell.Left + cell.Needed - 0.5 })
                    {
                    var hit = VisualTreeHelper.HitTest(root, new Point(x, y))?.VisualHit;
                    if (hit != null && !IsInside(hit, container))
                        problems.Add($"{where}, {cell.Column}: \"{cell.Text}\" is covered by {hit.GetType().Name} at {x:F1} px");
                    }
                }
            }

        private static bool IsInside(DependencyObject element, DependencyObject container)
            {
            for (var current = element; current != null; current = VisualTreeHelper.GetParent(current))
                if (ReferenceEquals(current, container)) return true;
            return false;
            }

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

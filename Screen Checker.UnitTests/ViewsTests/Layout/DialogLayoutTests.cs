using System.Windows;
using ScreenChecker.Views;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.UnitTests.ViewsTests.Layout
    {
    /// <summary>
    /// Shows the dialogs of the application with the longest texts it puts into them, in every language, and audits what is written
    /// on them. A dialog is also checked to stay within a screen of 1080 lines: it is not scrollable.
    /// </summary>
    [TestFixture]
    [Category("Ui")]
    public class DialogLayoutTests : UiTestHost
        {
        /// <summary>The height a dialog may take: a full HD screen without the taskbar and the title bar.</summary>
        private const double MaxDialogHeight = 1000;

        [TestCase("", TestName = "Dialogs_ReadWell_English")]
        [TestCase("ru", TestName = "Dialogs_ReadWell_Russian")]
        [TestCase("zh-Hans", TestName = "Dialogs_ReadWell_Chinese")]
        public void Dialogs_ReadWell(string culture)
            {
            Ui(() =>
                {
                UseLanguage(culture);
                var cultureInfo = CultureOf(culture);
                string language = LanguageName(culture);
                var problems = new List<string>();

                using var page = CreatePage();
                page.ViewModel.Spec.Show(WorstSpecEvaluation(), measuredPoints: 14, totalPoints: 14);
                string saveWarning = page.ViewModel.Spec.BuildSaveWarning(incomplete: true)!;
                string systemError = $"{SaveJSONErrForSN}: The process cannot access the file 'C:\\Users\\Operator\\Documents\\Screen Checker\\data\\12345678901234567890_20261126_123456.json' because it is being used by another process.";

                var dialogs = new List<(string Name, Window Window)>
                    {
                    ("confirmation of saving", new MessageDialogWindow(saveWarning, Warning, MessageBoxButton.YesNo)),
                    ("error with a system message", new MessageDialogWindow(systemError, Err, MessageBoxButton.OK)),
                    ("choice of the channel", new ChoiceDialogWindow(
                        string.Format(ChannelMismatchMessage, 99, 98), ChannelMismatchTitle, string.Format(UseProbeChannel, 99), string.Format(UseLastChannel, 98)))
                    };

                foreach (var (name, dialog) in dialogs)
                    {
                    dialog.WindowStartupLocation = WindowStartupLocation.Manual;
                    dialog.Left = -20000;
                    dialog.Top = -20000;
                    dialog.ShowInTaskbar = false;
                    dialog.Show();
                    try
                        {
                        Flush(dialog);
                        Shot((FrameworkElement)dialog.Content, name);
                        problems.AddRange(LayoutAudit.Run((FrameworkElement)dialog.Content, cultureInfo, $"[{language}/{name}]"));
                        if (dialog.ActualHeight > MaxDialogHeight)
                            problems.Add($"[{language}/{name}]: the dialog is {dialog.ActualHeight:F0} px high, a screen gives {MaxDialogHeight:F0} px and the dialog does not scroll");
                        }
                    finally
                        {
                        dialog.Close();
                        }
                    }

                Assert.That(problems, Is.Empty, Environment.NewLine + string.Join(Environment.NewLine, problems));
                });
            }
        }
    }

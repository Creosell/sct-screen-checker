using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class LogServiceTests
    {
        private Mock<ILocalizationService> _localization;
        private Mock<IDispatcher> _dispatcher;
        private LogService _log;

        [SetUp]
        public void SetUp()
        {
            _localization = new Mock<ILocalizationService>();
            _localization.Setup(l => l.GetString(It.IsAny<string>())).Returns((string key) => $"!{key}!");
            _dispatcher = new Mock<IDispatcher>();
            _dispatcher.Setup(d => d.CheckAccess()).Returns(true);
            _log = new LogService(_localization.Object, _dispatcher.Object);
        }

        [Test]
        public void Append_AddsTheMessageToTheTextAndSkipsEmptyOnes()
        {
            _log.Append("first");
            _log.Append(string.Empty);

            Assert.That(_log.Text.TrimEnd().Split(Environment.NewLine), Has.Length.EqualTo(1));
            Assert.That(_log.Text, Does.Contain("first"));
        }

        [Test]
        public void Append_OnlyWarningsAndErrorsGoToTheEntries()
        {
            _log.Append("info");
            _log.Append("warn", LogSeverity.Warning);
            _log.Append("err", LogSeverity.Error);

            Assert.That(_log.Entries.Select(e => e.Message), Is.EqualTo(new[] { "warn", "err" }));
        }

        [Test]
        public void Append_KeepsOnlyTheLast500LinesAndEntries()
        {
            for (int i = 0; i < 520; i++) _log.Append($"line {i}", LogSeverity.Warning);

            var lines = _log.Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(lines, Has.Length.EqualTo(500));
            Assert.That(lines[0], Does.EndWith("line 20"));
            Assert.That(_log.Entries, Has.Count.EqualTo(500));
            Assert.That(_log.Entries[0].Message, Is.EqualTo("line 20"));
        }

        [Test]
        public void Append_OffTheUiThread_ChangesTheEntriesOnlyThroughTheDispatcher()
        {
            _dispatcher.Setup(d => d.CheckAccess()).Returns(false);
            var queued = new List<Action>();
            _dispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>())).Callback<Action>(queued.Add);

            _log.Append("warn", LogSeverity.Warning);

            Assert.That(_log.Entries, Is.Empty);
            Assert.That(_log.Text, Does.Contain("warn"), "the text is updated at once");
            queued.ForEach(action => action());
            Assert.That(_log.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnreadCounters_FollowTheIssuesAddedWhileTheLogIsNotShown()
        {
            _log.Append("warn", LogSeverity.Warning);
            Assert.That((_log.UnreadIssues, _log.UnreadSeverity), Is.EqualTo((1, LogSeverity.Warning)));

            _log.Append("err", LogSeverity.Error);
            _log.Append("warn again", LogSeverity.Warning);
            Assert.That((_log.UnreadIssues, _log.UnreadSeverity), Is.EqualTo((3, LogSeverity.Error)), "the most severe level wins");

            _log.IsShown = true;
            Assert.That((_log.UnreadIssues, _log.UnreadSeverity), Is.EqualTo((0, LogSeverity.Info)));

            _log.Append("err", LogSeverity.Error);
            Assert.That(_log.UnreadIssues, Is.EqualTo(0), "nothing is unread while the log is shown");
        }

        [TestCase(99, "99")]
        [TestCase(100, "99+")]
        public void UnreadIssuesText_IsCappedAt99Plus(int issues, string expected)
        {
            for (int i = 0; i < issues; i++) _log.Append("err", LogSeverity.Error);

            Assert.That(_log.UnreadIssuesText, Is.EqualTo(expected));
        }

        [Test]
        public void Clear_EmptiesTheTextTheEntriesAndTheUnreadCounters()
        {
            _log.Append("err", LogSeverity.Error);

            _log.Clear();

            Assert.That(_log.Text, Is.Empty);
            Assert.That(_log.Entries, Is.Empty);
            Assert.That((_log.UnreadIssues, _log.UnreadSeverity), Is.EqualTo((0, LogSeverity.Info)));
        }

        [Test]
        public void Write_ResolvesTheResourceKeyOfTheArgumentExpression()
        {
            _localization.Setup(l => l.GetString("SomeKey")).Returns("Localized {0}");
            string SomeKey = "fallback";

            _log.Write(SomeKey, ["text"], LogSeverity.Warning);

            Assert.That(_log.Text, Does.Contain("Localized text"));
            Assert.That(_log.Entries.Single().Severity, Is.EqualTo(LogSeverity.Warning));
        }

        [Test]
        public void Write_TheSeverityAppliesToOneMessageOnly()
        {
            _log.Write("plain", severity: LogSeverity.Error);
            _log.Write("plain again");

            Assert.That(_log.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void Text_RaisesPropertyChanged()
        {
            var changed = new List<string?>();
            _log.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _log.Append("message");

            Assert.That(changed, Does.Contain(nameof(ILogService.Text)));
        }
    }
}

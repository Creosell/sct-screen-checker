using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using MvvmHelpers;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// In-memory application log bound to the Log tab.
    /// </summary>
    public class LogService : BaseViewModel, ILogService
        {
        private const int MaxLines = 500;
        private const int MaxEntries = 500;
        private const int MaxUnreadShown = 99;

        private readonly IDispatcher _dispatcher;
        private readonly LogHandler _logHandler;
        private string _text = string.Empty;
        private int _lineCount;
        private LogSeverity _pendingSeverity = LogSeverity.Info;
        private bool _isShown;
        private int _unreadIssues;
        private LogSeverity _unreadSeverity;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogService"/> class.
        /// </summary>
        /// <param name="localizationService">Service resolving the localized texts of the messages.</param>
        /// <param name="dispatcher">Dispatcher for the changes of the bound collection.</param>
        public LogService(ILocalizationService localizationService, IDispatcher dispatcher)
            {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logHandler = new LogHandler(localizationService, message => Append(message, _pendingSeverity));
            }

        /// <inheritdoc />
        public string Text
            {
            get => _text;
            private set => SetProperty(ref _text, value);
            }

        /// <inheritdoc />
        public ObservableCollection<LogEntry> Entries { get; } = [];

        /// <inheritdoc />
        public int UnreadIssues
            {
            get => _unreadIssues;
            private set
                {
                if (SetProperty(ref _unreadIssues, value))
                    OnPropertyChanged(nameof(UnreadIssuesText));
                }
            }

        /// <inheritdoc />
        public string UnreadIssuesText => _unreadIssues > MaxUnreadShown ? $"{MaxUnreadShown}+" : _unreadIssues.ToString();

        /// <inheritdoc />
        public LogSeverity UnreadSeverity
            {
            get => _unreadSeverity;
            private set => SetProperty(ref _unreadSeverity, value);
            }

        /// <inheritdoc />
        public bool IsShown
            {
            get => _isShown;
            set
                {
                if (SetProperty(ref _isShown, value) && value)
                    ResetUnread();
                }
            }

        /// <inheritdoc />
        public void Write(string messageOrKey, object[]? args = null, LogSeverity severity = LogSeverity.Info, [CallerArgumentExpression(nameof(messageOrKey))] string? resourceName = null)
            {
            _pendingSeverity = severity;
            try
                {
                _logHandler.Log(messageOrKey, args, resourceName);
                }
            finally
                {
                _pendingSeverity = LogSeverity.Info;
                }
            }

        /// <inheritdoc />
        public void Append(string message, LogSeverity severity = LogSeverity.Info)
            {
            if (string.IsNullOrEmpty(message)) return;

            _lineCount++;
            if (_lineCount > MaxLines)
                {
                var lines = Text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                var linesToKeep = lines.Skip(lines.Length - MaxLines + 1);
                Text = string.Join(Environment.NewLine, linesToKeep) + Environment.NewLine;
                _lineCount = MaxLines;
                }

            var entry = new LogEntry(DateTime.Now, message, severity);
            if (severity != LogSeverity.Info) RunOnUiThread(() => AddEntry(entry));

            Text += $"{entry.Time:HH:mm:ss} {message}{Environment.NewLine}";
            }

        /// <inheritdoc />
        public void Clear()
            {
            Text = string.Empty;
            Entries.Clear();
            ResetUnread();
            _lineCount = 0;
            }

        /// <summary>
        /// Adds an entry to the bound collection and counts it as unread unless the log is shown; runs on the UI thread.
        /// </summary>
        private void AddEntry(LogEntry entry)
            {
            if (Entries.Count >= MaxEntries) Entries.RemoveAt(0);
            Entries.Add(entry);
            if (IsShown) return;

            UnreadIssues++;
            if (entry.Severity > UnreadSeverity) UnreadSeverity = entry.Severity;
            }

        private void ResetUnread()
            {
            UnreadIssues = 0;
            UnreadSeverity = LogSeverity.Info;
            }

        private void RunOnUiThread(Action action)
            {
            if (_dispatcher.CheckAccess()) action();
            else _dispatcher.BeginInvoke(action);
            }
        }
    }

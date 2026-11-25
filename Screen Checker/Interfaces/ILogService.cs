using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ScreenChecker.Models;

namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// Application log: the text of every event, the warnings and errors shown in the Log tab and the counter of those
    /// the operator has not seen yet.
    /// </summary>
    public interface ILogService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the text of the last 500 events, one per line, ready to be copied.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Gets the warnings and errors shown in the Log tab (the last 500). The collection is bound to the UI and
        /// changes only on the UI thread.
        /// </summary>
        ObservableCollection<LogEntry> Entries { get; }

        /// <summary>
        /// Gets the number of warnings and errors added while the log was not shown.
        /// </summary>
        int UnreadIssues { get; }

        /// <summary>
        /// Gets the text of the unread counter: the number of unread issues, "99+" when there are more than 99.
        /// </summary>
        string UnreadIssuesText { get; }

        /// <summary>
        /// Gets the most severe level among the unread warnings and errors.
        /// </summary>
        LogSeverity UnreadSeverity { get; }

        /// <summary>
        /// Gets or sets whether the operator is looking at the log; showing it clears the unread counter and new
        /// issues are not counted while it is shown.
        /// </summary>
        bool IsShown { get; set; }

        /// <summary>
        /// Writes a message whose text or resource key is localized; a key is recognized by the expression the caller passed.
        /// </summary>
        /// <param name="messageOrKey">Message text or resource key.</param>
        /// <param name="args">Optional formatting arguments.</param>
        /// <param name="severity">Importance of the entry.</param>
        /// <param name="resourceName">Expression of the <paramref name="messageOrKey"/> argument, captured by the compiler.</param>
        void Write(string messageOrKey, object[]? args = null, LogSeverity severity = LogSeverity.Info, [CallerArgumentExpression(nameof(messageOrKey))] string? resourceName = null);

        /// <summary>
        /// Appends an already localized message.
        /// </summary>
        /// <param name="message">Text of the message; empty messages are ignored.</param>
        /// <param name="severity">Importance of the entry.</param>
        void Append(string message, LogSeverity severity = LogSeverity.Info);

        /// <summary>
        /// Clears the text, the entries and the unread counter.
        /// </summary>
        void Clear();
    }
}

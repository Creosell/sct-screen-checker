namespace ScreenChecker.Models
    {
    /// <summary>
    /// A failure reported by the CA-SDK2 (CA-410) as a status code, carrying the code for the user-facing translation.
    /// </summary>
    public sealed class AnalyzerException : InvalidOperationException
        {
        /// <summary>
        /// Gets the SDK status code.
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// Gets the name of the SDK operation that failed.
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// Gets the message the SDK gives for the code.
        /// </summary>
        public string SdkMessage { get; }

        /// <summary>
        /// Initializes the exception.
        /// </summary>
        /// <param name="code">SDK status code.</param>
        /// <param name="operation">Name of the SDK operation that failed.</param>
        /// <param name="sdkMessage">Localized message of the SDK for the code.</param>
        public AnalyzerException(int code, string operation, string sdkMessage)
            : base($"CA-SDK2 {operation} failed ({code}): {sdkMessage}")
            {
            Code = code;
            Operation = operation;
            SdkMessage = sdkMessage;
            }
        }
    }

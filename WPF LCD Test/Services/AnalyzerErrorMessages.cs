using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Turns the error codes of the color analyzer SDKs (CA-SDK2 for the CA-410, CA-SDK for the CA-310) into the localized texts
    /// of the operator. The two SDKs share the numbering of the error codes, so one table serves both analyzers.
    /// </summary>
    public static partial class AnalyzerErrorMessages
        {
        private const int ErrorObjectFacility = unchecked((int)0x80040000);
        private const int FacilityMask = unchecked((int)0xFFFF0000);
        private const int CodeMask = 0xFFFF;

        [GeneratedRegex(@"\s+")]
        private static partial Regex Whitespace();

        /// <summary>
        /// Gets the SDK error code of an exception.
        /// </summary>
        /// <param name="exception">Exception thrown by an analyzer call.</param>
        /// <param name="code">The SDK error code when the exception carries one.</param>
        /// <returns>True when the exception is an analyzer error with a code.</returns>
        public static bool TryGetCode(Exception exception, out int code)
            {
            switch (exception)
                {
                case AnalyzerException analyzer:
                    code = analyzer.Code;
                    return true;
                case COMException com when ( com.HResult & FacilityMask ) == ErrorObjectFacility:
                    code = com.HResult & CodeMask;
                    return true;
                default:
                    code = 0;
                    return false;
                }
            }

        /// <summary>
        /// Describes an exception of an analyzer call for the operator.
        /// </summary>
        /// <param name="exception">Exception thrown by an analyzer call.</param>
        /// <param name="model">Analyzer the call was made on, <see cref="ColorAnalyzerModel.Unknown"/> when not known yet.</param>
        /// <returns>The localized text with the model and the code when the exception carries an SDK code; otherwise its own message.</returns>
        public static string Describe(Exception exception, ColorAnalyzerModel model)
            {
            if (!TryGetCode(exception, out var code))
                return exception.Message;

            var sdkText = exception is AnalyzerException analyzer ? analyzer.SdkMessage : exception.Message;
            return Translate(code, model, sdkText);
            }

        /// <summary>
        /// Builds the localized text of an SDK error code.
        /// </summary>
        /// <param name="code">SDK error code.</param>
        /// <param name="model">Analyzer the code comes from; picks the wording that names its parts.</param>
        /// <param name="sdkText">Message of the SDK, shown for the codes without a text of their own.</param>
        /// <returns>The text followed by the model and the code, such as "[CA-410 · 523]".</returns>
        public static string Translate(int code, ColorAnalyzerModel model, string? sdkText = null)
            {
            var sdk = string.IsNullOrWhiteSpace(sdkText) ? Lookup("AnalyzerErrorUnknownText", model) : Whitespace().Replace(sdkText, " ").Trim();
            var text = string.Format(Lookup($"AnalyzerError_{KeyOf(code)}", model), Lookup("AnalyzerUsbCable", model), sdk);
            var tag = model == ColorAnalyzerModel.Unknown ? $"[{code}]" : $"[{model.ToDisplayName()} · {code}]";
            return $"{text} {tag}";
            }

        /// <summary>
        /// Reads a resource, preferring the wording written for the analyzer model.
        /// </summary>
        private static string Lookup(string name, ColorAnalyzerModel model)
            {
            var resources = WPF_LCD_Test.Resources.Resources.ResourceManager;
            return resources.GetString($"{name}_{model}") ?? resources.GetString(name) ?? name;
            }

        /// <summary>
        /// Maps an SDK error code to the name of its text in the resources.
        /// </summary>
        private static string KeyOf(int code) => code switch
            {
            200 or 208 or 209 => "PortState",
            201 => "InvalidTimeout",
            >= 202 and <= 206 => "UsbOpen",
            207 => "ComOpen",
            210 or 211 => "SendFailed",
            212 => "ConnectionClosed",
            213 or 214 => "ReceiveFailed",
            215 or 216 => "NoResponse",
            299 => "ConnectionMismatch",
            >= 300 and <= 303 => "NetworkConnect",
            >= 304 and <= 308 => "NetworkLost",
            401 => "NotNow",
            402 => "InvalidArgument",
            403 => "Duplicate",
            404 or 405 => "SdkInternal",
            406 => "CalFileOpen",
            407 => "InvalidAnalyzerNumber",
            408 => "InvalidPortId",
            409 => "InvalidBaudRate",
            410 => "NullPointer",
            411 or 412 => "ProbeNotConnected",
            >= 413 and <= 415 => "ColorValue",
            416 => "InvalidIndex",
            417 => "InvalidAnalyzerId",
            418 => "ChannelNumber",
            419 => "ChannelId",
            420 => "ChannelIdTooLong",
            421 => "InvalidProbeNumber",
            422 => "InvalidProbeId",
            423 or 424 => "SyncValueOutOfRange",
            425 => "NoObject",
            426 => "MeasurementFailed",
            427 => "PortInUse",
            428 => "CalChannelZero",
            429 => "NoOutputProbe",
            430 => "BufferTooSmall",
            431 => "ChannelComment",
            432 => "SamplingCombination",
            433 => "BodyOnly",
            434 => "ConfigType",
            435 => "SavingCancelled",
            436 => "MeasurementInProgress",
            437 => "DeviceNotFound",
            438 => "DataNotFound",
            439 => "DeviceNotSupported",
            440 => "ProbeOnly",
            441 => "HardwareSetup",
            442 => "SettingsMismatch",
            443 => "UserDataNotSet",
            444 => "FirmwareIncompatible",
            445 => "FrequencyOutOfRange",
            446 => "Ca500Only",
            447 => "CalTypeMismatch",
            503 => "CalData",
            504 => "AnalogRange",
            505 => "InputShortage",
            506 => "MatrixCal",
            510 => "CommandUnsupported",
            515 => "HoldError",
            516 => "MemoryData",
            520 => "NoSync",
            521 => "ZeroCalLight",
            522 => "OutOfRange",
            523 => "ZeroCalRequired",
            524 => "ColorTempFailed",
            531 or 532 => "MemoryAccess",
            550 => "FlickerRange",
            551 => "FlickerSync",
            552 => "FlickerDark",
            553 => "FlickerProbe",
            599 => "SystemError",
            >= 500 and <= 598 => "CommandFailed",
            _ => "Unknown"
            };
        }
    }

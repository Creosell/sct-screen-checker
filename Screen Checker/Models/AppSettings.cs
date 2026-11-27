using System.Text.Json.Serialization;

namespace ScreenChecker.Models
    {
    /// <summary>
    /// Represents application settings for persistence in JSON format.
    /// </summary>
    public class AppSettings
        {
        /// <summary>
        /// Gets or sets the application language culture code (e.g., "en", "zh-Hans").
        /// Empty string represents default culture.
        /// </summary>
        [JsonPropertyName("languageCultureCode")]
        public string LanguageCultureCode { get; set; } = "";

        /// <summary>
        /// Gets or sets the measurement speed applied to the color analyzer during zero calibration.
        /// </summary>
        [JsonPropertyName("measurementSpeed")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MeasurementSpeed MeasurementSpeed { get; set; } = MeasurementSpeed.Slow;

        /// <summary>
        /// Gets or sets the synchronization mode applied to the color analyzer during zero calibration.
        /// </summary>
        [JsonPropertyName("syncMode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SyncMode SyncMode { get; set; } = SyncMode.Universal;

        /// <summary>
        /// Gets or sets the internal synchronization frequency in Hz; null selects the default.
        /// </summary>
        [JsonPropertyName("theme")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ThemeMode Theme { get; set; } = ThemeMode.Dark;

        [JsonPropertyName("syncFrequency")]
        public double? SyncFrequency { get; set; }

        /// <summary>
        /// Gets or sets the manual integration time in ms; null selects the default.
        /// </summary>
        [JsonPropertyName("integrationTime")]
        public double? IntegrationTime { get; set; }

        /// <summary>
        /// Gets or sets how much larger than the system makes it the interface is drawn (1.25 is 125%); null follows the system.
        /// </summary>
        [JsonPropertyName("interfaceScale")]
        public double? InterfaceScale { get; set; }

        /// <summary>
        /// Gets the interface scale to apply: the stored one when it is within the supported range, 1 (as the system) otherwise.
        /// </summary>
        [JsonIgnore]
        public double EffectiveInterfaceScale => InterfaceScale is >= MinInterfaceScale and <= MaxInterfaceScale ? InterfaceScale.Value : 1;

        /// <summary>The smallest interface scale a settings file may hold.</summary>
        public const double MinInterfaceScale = 1;

        /// <summary>The largest interface scale a settings file may hold.</summary>
        public const double MaxInterfaceScale = 2;

        /// <summary>
        /// Builds the synchronization setting (mode with the parameter it needs) from the stored values.
        /// </summary>
        public SyncSetting ToSyncSetting() => SyncMode switch
            {
            SyncMode.Internal => new(SyncMode, SyncFrequency ?? AnalyzerCapabilities.DefaultFrequency),
            SyncMode.Manual => new(SyncMode, IntegrationTime ?? AnalyzerCapabilities.DefaultIntegrationTime),
            _ => new(SyncMode)
            };
        }
    }
using System.Text.Json.Serialization;
using ScreenChecker.Converters;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.Models
    {
    /// <summary>
    /// Represents a device under test with its measurements and configuration.
    /// </summary>
    public class DeviceUnderTest
        {
        /// <summary>
        /// Gets or sets the device serial number.
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the device configuration identifier.
        /// </summary>
        public string DeviceConfiguration { get; set; } = "DefaultConfig";

        /// <summary>
        /// Gets or sets the colorimeter and channel the measurements were taken with; omitted from JSON when unknown.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ColorimeterInfo? Colorimeter { get; set; }

        /// <summary>
        /// Gets or sets whether the device is a TV.
        /// </summary>
        public bool IsTV { get; set; }

        /// <summary>
        /// Gets or sets the measurement timestamp.
        /// </summary>
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime MeasurementDateTime { get; set; }

        /// <summary>
        /// Gets or sets the collection of measurements for this device.
        /// </summary>
        public List<Measurement> Measurements { get; set; }

        /// <summary>
        /// Initializes a new instance of DeviceUnderTest with specified serial number.
        /// </summary>
        /// <param name="serialNumber">Device serial number.</param>
        /// <exception cref="ArgumentException">Thrown when serial number is null or whitespace.</exception>
        public DeviceUnderTest(string serialNumber)
            {
            if (string.IsNullOrWhiteSpace(serialNumber))
                throw new ArgumentException($"{SnCantBeEmpty}", nameof(serialNumber));
            SerialNumber = serialNumber;
            MeasurementDateTime = DateTime.Now;
            Measurements = [];
            }

        private readonly HashSet<string> _seriesPoints = [];

        /// <summary>
        /// Records an accepted measurement of a point. Until the point is in a series (see <see cref="StartSeries"/>)
        /// the measurement replaces the earlier one; in a series the point keeps all its attempts and its
        /// measurement holds the latest one until the report generator (the only place that ranks the attempts)
        /// answers with the best one, see <see cref="SelectBestAttempt"/>.
        /// </summary>
        /// <param name="attempt">The accepted measurement.</param>
        /// <returns>The measurement entry of the point.</returns>
        public Measurement RecordAttempt(Measurement attempt)
            {
            ArgumentNullException.ThrowIfNull(attempt);

            var existing = Measurements.FirstOrDefault(m => m.Location == attempt.Location);
            List<Measurement> attempts = existing == null || !_seriesPoints.Contains(attempt.Location) ? [attempt]
                : existing.Attempts.Count > 0 ? [.. existing.Attempts, attempt]
                : [existing, attempt];

            return StoreAttempts(attempts, attempts.Count - 1);
            }

        /// <summary>
        /// Starts the series of a point: its next measurements are kept as attempts instead of replacing the held one.
        /// The series lasts as long as the device.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        public void StartSeries(string location) => _seriesPoints.Add(location);

        /// <summary>
        /// Makes another attempt of a point the one its measurement holds (the choice of the specification check).
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        /// <param name="index">Index of the attempt.</param>
        public void SelectBestAttempt(string location, int index)
            {
            var existing = Measurements.FirstOrDefault(m => m.Location == location);
            if (existing == null || index < 0 || index >= existing.Attempts.Count || index == existing.BestAttemptIndex) return;

            StoreAttempts(existing.Attempts, index);
            }

        /// <summary>
        /// Takes over the verdict of the specification check on the attempts: starts the series of every point that
        /// deviated from the specification, on its own or through a composite metric such as the gamut (its next
        /// measurements are kept as attempts), and makes the best attempt
        /// of every point the one its measurement holds. A point measured since the check is left alone.
        /// </summary>
        /// <param name="evaluation">The evaluation of this device.</param>
        public void ApplyAttemptChoices(SpecEvaluation evaluation)
            {
            ArgumentNullException.ThrowIfNull(evaluation);

            foreach (var (location, info) in evaluation.Attempts)
                {
                if (AttemptCount(location) != info.Count) continue;

                if (info.Deviated || evaluation.HasRemark(location)) StartSeries(location);
                if (info.Count > 1) SelectBestAttempt(location, info.Best);
                }
            }

        /// <summary>
        /// Gets the number of accepted attempts of a point.
        /// </summary>
        /// <param name="location">Measurement location identifier.</param>
        /// <returns>The number of attempts; 0 when the point has no measurement.</returns>
        public int AttemptCount(string location) =>
            Measurements.FirstOrDefault(m => m.Location == location) is { } measurement ? Math.Max(1, measurement.Attempts.Count) : 0;

        private Measurement StoreAttempts(List<Measurement> attempts, int best)
            {
            var chosen = attempts[best];
            var entry = new Measurement(chosen.Location, chosen.x, chosen.y, chosen.Lv, chosen.T)
                {
                IsValid = chosen.IsValid,
                Time = chosen.Time,
                Attempts = attempts,
                BestAttemptIndex = best
                };

            int at = Measurements.FindIndex(m => m.Location == chosen.Location);
            if (at >= 0) Measurements[at] = entry;
            else Measurements.Add(entry);
            return entry;
            }

        /// <summary>
        /// Returns formatted string representation of device and its measurements.
        /// </summary>
        /// <returns>Formatted device information string.</returns>
        public override string ToString()
            {
            var measurementsStr = ( Measurements.Count>0 ) == true
                ? string.Join("; ", Measurements.Select(m => m.ToString()))
                : "None";
            return $"Device SN: {SerialNumber}, Config: {DeviceConfiguration}, IsTV: {IsTV}, Measured at: {MeasurementDateTime}, Measurements: [{measurementsStr}]";
            }
        }
    }
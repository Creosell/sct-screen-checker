using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Plausibility rules of a measurement: the luminance of the point (a black point has to be dark, any other bright)
    /// and the coordinates around the primary the point is expected to show.
    /// </summary>
    public class MeasurementAcceptance : IMeasurementAcceptance
        {
        /// <summary>Allowed distance of the measured x and y from the expected primary.</summary>
        public const double ColorCoordinatesTolerance = 0.15;

        private const int MaxRejectedAttemptsBeforeConfirm = 1;
        private const double MinLuminance = 5;
        private const double MaxBlackLuminance = 5;

        private static readonly Dictionary<string, (double x, double y)> Primaries = new()
            {
            { MeasurementLocation.RedColor.ToString(), (0.67, 0.33) },
            { MeasurementLocation.GreenColor.ToString(), (0.21, 0.71) },
            { MeasurementLocation.BlueColor.ToString(), (0.14, 0.08) },
            { MeasurementLocation.WhiteColor.ToString(), (0.3127, 0.3290) }
            };

        private readonly IAttemptCounter _attemptCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasurementAcceptance"/> class.
        /// </summary>
        /// <param name="attemptCounter">Counter of the rejected attempts per point.</param>
        public MeasurementAcceptance(IAttemptCounter attemptCounter)
            {
            _attemptCounter = attemptCounter ?? throw new ArgumentNullException(nameof(attemptCounter));
            }

        /// <inheritdoc />
        public AcceptanceResult Assess(Measurement measurement)
            {
            ArgumentNullException.ThrowIfNull(measurement);

            var (isPlausible, reason) = Validate(measurement);
            int rejected = _attemptCounter.Get(measurement.Location);

            if (isPlausible)
                {
                _attemptCounter.Reset(measurement.Location);
                return new AcceptanceResult.Accepted();
                }

            if (rejected < MaxRejectedAttemptsBeforeConfirm)
                {
                _attemptCounter.Increment(measurement.Location);
                return new AcceptanceResult.Rejected(reason, rejected + 1);
                }

            _attemptCounter.Reset(measurement.Location);
            return new AcceptanceResult.ConfirmationNeeded(reason);
            }

        private static (bool IsPlausible, string Reason) Validate(Measurement measurement)
            {
            if (measurement.Location.Equals(MeasurementLocation.BlackColor.ToString()))
                {
                return measurement.Lv >= MaxBlackLuminance
                    ? (false, $"{LvIsTooHigh}. \n {Brightness}: {measurement.Lv:F1}")
                    : (true, MeasurementValidationPassed);
                }

            if (measurement.Lv <= MinLuminance)
                return (false, $"{LvIsTooLow}: {measurement.Lv:F1}. {CheckProbe}");

            if (!Primaries.TryGetValue(measurement.Location, out var target))
                target = Primaries[MeasurementLocation.WhiteColor.ToString()];

            bool inRange = measurement.x >= target.x - ColorCoordinatesTolerance &&
                           measurement.x <= target.x + ColorCoordinatesTolerance &&
                           measurement.y >= target.y - ColorCoordinatesTolerance &&
                           measurement.y <= target.y + ColorCoordinatesTolerance;

            return inRange
                ? (true, MeasurementValidationPassed)
                : (false, $"{ErrMeasurementOutOfRange}: '{measurement.Location}'.\n{Actual} x: {measurement.x:F3}, y: {measurement.y:F3}");
            }
        }
    }

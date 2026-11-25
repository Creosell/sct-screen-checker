using System.Globalization;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.Models
    {
    /// <summary>
    /// Formats the values of a measurement for the screen.
    /// </summary>
    public static class MeasurementFormatter
        {
        /// <summary>
        /// Formats a measurement for the Results table and the tooltips with the precision the report verdict is made at
        /// (coordinates 3 decimals, luminance 0, the luminance of the black point 4, temperature 0), so the number on the screen
        /// is the number the point is accepted by.
        /// </summary>
        /// <param name="measurement">The measurement to format.</param>
        /// <returns>The coordinates, luminance and temperature as text.</returns>
        public static (string x, string y, string Lv, string T) Format(Measurement measurement)
            {
            return (
                measurement.x.ToString("F3", CultureInfo.InvariantCulture),
                measurement.y.ToString("F3", CultureInfo.InvariantCulture),
                measurement.Location == MeasurementLocation.BlackColor.ToString()
                    ? measurement.Lv.ToString("F4", CultureInfo.InvariantCulture)
                    : measurement.Lv.ToString("F0", CultureInfo.InvariantCulture),
                double.IsFinite(measurement.T) ? measurement.T.ToString("F0", CultureInfo.InvariantCulture) : NotAvailable
            );
            }
        }
    }

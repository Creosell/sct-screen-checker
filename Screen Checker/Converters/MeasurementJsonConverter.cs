using System.Text.Json;
using System.Text.Json.Serialization;
using ScreenChecker.Models;

namespace ScreenChecker.Converters
{
    /// <summary>
    /// Custom JSON converter for Measurement objects: the values are written as the analyzer gave them, only cut at
    /// <see cref="MaxDecimals"/> fractional digits, so nothing the analyzer measured is lost before the report is built.
    /// </summary>
    public class MeasurementJsonConverter : JsonConverter<Measurement>
    {
        /// <summary>The most fractional digits of a written number.</summary>
        public const int MaxDecimals = 10;

        /// <summary>
        /// Reads and converts JSON to Measurement. Not implemented as deserialization is not required.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Measurement instance.</returns>
        /// <exception cref="NotImplementedException">Always thrown as deserialization is not implemented.</exception>
        public override Measurement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization not implemented for MeasurementJsonConverter.");
        }

        /// <summary>
        /// Writes Measurement value as JSON; every number keeps its digits up to <see cref="MaxDecimals"/> fractional ones.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The Measurement value to write.</param>
        /// <param name="options">Serialization options.</param>
        public override void Write(Utf8JsonWriter writer, Measurement value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Location", value.Location);
            WriteNumber(writer, "x", value.x);
            WriteNumber(writer, "y", value.y);
            WriteNumber(writer, "Lv", value.Lv);
            WriteNumber(writer, "T", value.T);

            if (value.Attempts.Count > 1)
            {
                WriteAttempts(writer, value);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes every attempt of the point with the flag of the one the measurement holds.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The measurement with the attempts.</param>
        private static void WriteAttempts(Utf8JsonWriter writer, Measurement value)
        {
            writer.WriteStartArray("Attempts");
            for (int i = 0; i < value.Attempts.Count; i++)
            {
                var attempt = value.Attempts[i];
                writer.WriteStartObject();
                WriteNumber(writer, "x", attempt.x);
                WriteNumber(writer, "y", attempt.y);
                WriteNumber(writer, "Lv", attempt.Lv);
                WriteNumber(writer, "T", attempt.T);
                writer.WriteBoolean("Best", i == value.BestAttemptIndex);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Writes a number property cut at <see cref="MaxDecimals"/> fractional digits; non-finite values (e.g. undefined color temperature reported as Infinity) are written as null, as JSON cannot represent them.
        /// </summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="name">Property name.</param>
        /// <param name="value">Value to write.</param>
        private static void WriteNumber(Utf8JsonWriter writer, string name, double value)
        {
            if (double.IsFinite(value))
                writer.WriteNumber(name, Math.Round(value, MaxDecimals, MidpointRounding.AwayFromZero));
            else
                writer.WriteNull(name);
        }
    }
}
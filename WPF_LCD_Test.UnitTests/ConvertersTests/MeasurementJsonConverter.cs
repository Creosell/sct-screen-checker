// В проекте WPF_LCD_Test.UnitTests (папка ConvertersTests)
// Файл MeasurementJsonConverterTests.cs

using System.Text.Json;
using WPF_LCD_Test.Converters;
using WPF_LCD_Test.Models; // Для класса Measurement

namespace WPF_LCD_Test.UnitTests.ConvertersTests
{
    [TestFixture]
    public class MeasurementJsonConverterTests
    {
        // Вспомогательный класс для тестирования конвертера
        private class TestObjectWithMeasurement
        {
            [System.Text.Json.Serialization.JsonConverter(typeof(MeasurementJsonConverter))]
            public Measurement TestMeasurement { get; set; }
        }


        [Test]
        public void Write_KeepsEveryDigitTheAnalyzerGave()
        {
            // Arrange
            var measurement = new Measurement("Center", 1.2345, 6.7891, 100.12345, 25.6);
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            string expectedJson = "{\"TestMeasurement\":{\"Location\":\"Center\",\"x\":1.2345,\"y\":6.7891,\"Lv\":100.12345,\"T\":25.6}}";
            Assert.That(jsonString, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Write_NonFiniteValues_AreWrittenAsNull()
        {
            // Arrange
            var measurement = new Measurement("RedColor", 0.66, double.NaN, 28.8, double.PositiveInfinity);
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject);

            // Assert
            string expectedJson = "{\"TestMeasurement\":{\"Location\":\"RedColor\",\"x\":0.66,\"y\":null,\"Lv\":28.8,\"T\":null}}";
            Assert.That(jsonString, Is.EqualTo(expectedJson));
        }

        [Test]
        public void Write_CutsNumbersAtTenFractionalDigits()
        {
            // Arrange
            var measurement = new Measurement(MeasurementLocation.BlackColor.ToString(), 0.123456789012345, 0.66, 0.0503, 20.9);
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            string expectedJson = "{\"TestMeasurement\":{\"Location\":\"BlackColor\",\"x\":0.123456789,\"y\":0.66,\"Lv\":0.0503,\"T\":20.9}}";
            Assert.That(jsonString, Is.EqualTo(expectedJson), "A black point keeps its small luminance; nothing is cut before the tenth fractional digit.");
        }

        [Test]
        public void Write_AveragedValues_KeepTheirDigits()
        {
            // Arrange: the application averages the samples of one measurement, so the numbers have many digits
            var measurement1 = new Measurement("Red", 0.655343434, 0.342542, 123.456, 2313.12312312);
            var measurement2 = new Measurement("White", 0.2636746346, 0.436643346, 123.891, 6500.8231230);
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString1 = JsonSerializer.Serialize(new TestObjectWithMeasurement { TestMeasurement = measurement1 }, options);
            string jsonString2 = JsonSerializer.Serialize(new TestObjectWithMeasurement { TestMeasurement = measurement2 }, options);

            // Assert
            Assert.That(jsonString1, Is.EqualTo("{\"TestMeasurement\":{\"Location\":\"Red\",\"x\":0.655343434,\"y\":0.342542,\"Lv\":123.456,\"T\":2313.12312312}}"));
            Assert.That(jsonString2, Is.EqualTo("{\"TestMeasurement\":{\"Location\":\"White\",\"x\":0.2636746346,\"y\":0.436643346,\"Lv\":123.891,\"T\":6500.823123}}"));
        }

        [Test]
        public void Write_IsValidProperty_IsNotSerialized()
        {
            // Arrange
            var measurement = new Measurement("Test", 1.0, 1.0, 1.0, 1.0);
            measurement.IsValid = false; // Устанавливаем, чтобы убедиться, что она не попадает в JSON
            var testObject = new TestObjectWithMeasurement { TestMeasurement = measurement };
            var options = new JsonSerializerOptions { WriteIndented = false };

            // Act
            string jsonString = JsonSerializer.Serialize(testObject, options);

            // Assert
            // Проверяем, что строка не содержит "IsValid"
            Assert.That(jsonString, Does.Not.Contain("\"IsValid\""), "IsValid property should not be serialized.");
            Assert.That(jsonString, Does.Not.Contain("true"), "Boolean 'true' from IsValid should not be in JSON.");
            Assert.That(jsonString, Does.Not.Contain("false"), "Boolean 'false' from IsValid should not be in JSON.");
        }

        [Test]
        public void Read_ThrowsNotImplementedException()
        {
            // Arrange
            // Любая строка JSON, так как Read все равно выбросит исключение
            string jsonString = "{\"TestMeasurement\":{\"Location\":\"Some\",\"x\":1,\"y\":1,\"Lv\":1,\"T\":1}}";
            var options = new JsonSerializerOptions();

            // Act & Assert
            Assert.Throws<NotImplementedException>(() =>
            {
                JsonSerializer.Deserialize<TestObjectWithMeasurement>(jsonString, options);
            }, "Read method should throw NotImplementedException as it is not implemented.");
        }
    }
}
using System.Globalization;
using System.Runtime.InteropServices;
using Moq;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    /// <summary>
    /// Checks the translation of the SDK error codes of the color analyzers into the texts of the operator.
    /// </summary>
    [TestFixture]
    public class AnalyzerErrorMessagesTests
        {
        private static readonly string[] Cultures = ["", "ru", "zh-Hans"];
        private static readonly ColorAnalyzerModel[] Models = [ColorAnalyzerModel.Unknown, ColorAnalyzerModel.CA310, ColorAnalyzerModel.CA410];
        private const int ErrorObjectFacility = unchecked((int)0x80040000);

        private CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        [SetUp]
        public void SetUp()
            {
            _uiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            }

        [TearDown]
        public void TearDown() => CultureInfo.CurrentUICulture = _uiCulture;

        [Test]
        public void TryGetCode_TakesTheCodeOfAnAnalyzerException()
            {
            var found = AnalyzerErrorMessages.TryGetCode(new AnalyzerException(437, "AutoConnect", "Device not found"), out var code);

            Assert.That(found, Is.True);
            Assert.That(code, Is.EqualTo(437));
            }

        [Test]
        public void TryGetCode_TakesTheLowWordOfAnErrorObjectHResult()
            {
            var found = AnalyzerErrorMessages.TryGetCode(new COMException("measurement fail", ErrorObjectFacility | 426), out var code);

            Assert.That(found, Is.True);
            Assert.That(code, Is.EqualTo(426));
            }

        [Test]
        public void TryGetCode_IgnoresTheHResultsOfTheSystem()
            {
            Assert.That(AnalyzerErrorMessages.TryGetCode(new COMException("access denied", unchecked((int)0x80070005)), out _), Is.False);
            Assert.That(AnalyzerErrorMessages.TryGetCode(new InvalidOperationException("no code"), out _), Is.False);
            }

        [Test]
        public void Describe_KeepsTheMessageOfAnExceptionWithoutCode()
            {
            Assert.That(AnalyzerErrorMessages.Describe(new InvalidOperationException("plain"), ColorAnalyzerModel.CA410), Is.EqualTo("plain"));
            }

        [Test]
        public void Describe_AddsTheModelAndTheCode()
            {
            var text = AnalyzerErrorMessages.Describe(new AnalyzerException(523, "CalZero", "x"), ColorAnalyzerModel.CA410);

            Assert.That(text, Is.EqualTo("Zero calibration is required. Perform it and repeat the measurement. [CA-410 · 523]"));
            }

        [Test]
        public void Describe_AddsOnlyTheCodeWhenTheModelIsUnknown()
            {
            var text = AnalyzerErrorMessages.Describe(new AnalyzerException(437, "AutoConnect", "x"), ColorAnalyzerModel.Unknown);

            Assert.That(text, Does.EndWith("[437]"));
            }

        [Test]
        public void Translate_NamesTheRingOfTheCa310AndNotTheCa410()
            {
            var ca310 = AnalyzerErrorMessages.Translate(521, ColorAnalyzerModel.CA310);
            var ca410 = AnalyzerErrorMessages.Translate(521, ColorAnalyzerModel.CA410);

            Assert.That(ca310, Does.Contain("0-CAL"));
            Assert.That(ca410, Does.Not.Contain("0-CAL"));
            }

        [Test]
        public void Translate_NamesTheCableOfTheAnalyzerConnection()
            {
            Assert.That(AnalyzerErrorMessages.Translate(210, ColorAnalyzerModel.CA410), Does.Contain("USB cable of the probe"));
            Assert.That(AnalyzerErrorMessages.Translate(210, ColorAnalyzerModel.CA310), Does.Contain("USB cable of the analyzer body"));
            Assert.That(AnalyzerErrorMessages.Translate(210, ColorAnalyzerModel.Unknown), Does.Contain("analyzer's USB cable"));
            }

        [Test]
        public void Translate_ShowsTheTextOfTheSdkForAnUnknownCode()
            {
            var text = AnalyzerErrorMessages.Translate(777, ColorAnalyzerModel.CA310, "--Something\r\n--new");

            Assert.That(text, Is.EqualTo("Analyzer error: --Something --new [CA-310 · 777]"));
            }

        [Test]
        public void Translate_SaysUnknownErrorWhenTheSdkGivesNoText()
            {
            Assert.That(AnalyzerErrorMessages.Translate(777, ColorAnalyzerModel.Unknown), Is.EqualTo("Analyzer error: unknown error [777]"));
            }

        [Test]
        public void Translate_GivesEveryCodeOfTheSdkAnAnswerInEveryLanguage(
            [ValueSource(nameof(Cultures))] string culture,
            [ValueSource(nameof(Models))] ColorAnalyzerModel model)
            {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);

            for (var code = 0; code <= 599; code++)
                {
                var text = AnalyzerErrorMessages.Translate(code, model, "sdk");

                Assert.That(text, Does.Not.Contain("AnalyzerError_").And.Not.Contain("AnalyzerUsbCable").And.Not.Contain("{"), $"code {code}, culture '{culture}'");
                Assert.That(text, Does.EndWith($"{code}]"), $"code {code}");
                }
            }

        [Test]
        public async Task ConnectAsync_ReportsTheTranslatedTextOfAnAnalyzerError()
            {
            var analyzer = new Mock<IColorAnalyzer200>();
            analyzer.Setup(a => a.AutoConnect()).Throws(new AnalyzerException(437, "AutoConnect", "Device not found"));
            var service = new ColorMeasurementService(analyzer.Object);
            string? message = null;
            service.StatusMessage += (_, text) => message = text;

            await service.ConnectAsync();

            Assert.That(message, Does.Contain("Analyzer not found. Check the USB cable and the power. [437]"));
            }
        }
    }

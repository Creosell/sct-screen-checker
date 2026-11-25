using System.IO.Abstractions.TestingHelpers;
using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class ReportGenerationServiceTests
    {
        private const string BaseDirectory = @"C:\app";
        private const string GeneratorPath = @"C:\app\Report Generator.exe";

        private MockFileSystem _fileSystem;
        private Mock<IProcessRunner> _processRunner;
        private ReportGenerationService _service;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new MockFileSystem();
            _processRunner = new Mock<IProcessRunner>();
            var pathProvider = new Mock<IPathProvider>();
            pathProvider.Setup(p => p.BaseDirectory).Returns(BaseDirectory);
            _service = new ReportGenerationService(_fileSystem, pathProvider.Object, _processRunner.Object);
        }

        private void SetupExitCode(int exitCode) =>
            _processRunner.Setup(r => r.RunAsync(GeneratorPath, It.IsAny<IReadOnlyList<string>>(), BaseDirectory, string.Empty, Timeout.InfiniteTimeSpan, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult(exitCode, string.Empty, string.Empty));

        [Test]
        public void ExecutablePath_IsTheGeneratorNextToTheApplication()
        {
            Assert.That(_service.ExecutablePath, Is.EqualTo(GeneratorPath));
        }

        [Test]
        public void IsAvailable_FollowsTheExistenceOfTheExecutable()
        {
            Assert.That(_service.IsAvailable, Is.False);

            _fileSystem.AddFile(GeneratorPath, new MockFileData(string.Empty));

            Assert.That(_service.IsAvailable, Is.True);
        }

        [TestCase(0, ReportGenerationStatus.Success)]
        [TestCase(1, ReportGenerationStatus.Failed)]
        [TestCase(2, ReportGenerationStatus.NoDataFound)]
        [TestCase(3, ReportGenerationStatus.ConfigError)]
        [TestCase(42, ReportGenerationStatus.Failed)]
        public async Task GenerateAsync_MapsTheExitCodeOfTheGenerator(int exitCode, ReportGenerationStatus expected)
        {
            SetupExitCode(exitCode);

            var result = await _service.GenerateAsync(CancellationToken.None);

            Assert.That(result, Is.EqualTo(new ReportGenerationResult(expected, exitCode)));
        }

        [Test]
        public async Task GenerateAsync_WhenCanceled_ReturnsCanceled()
        {
            using var cts = new CancellationTokenSource();
            _processRunner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns<string, IReadOnlyList<string>, string, string, TimeSpan, CancellationToken>((_, _, _, _, _, token) =>
                {
                    cts.Cancel();
                    token.ThrowIfCancellationRequested();
                    return Task.FromResult(new ProcessResult(0, string.Empty, string.Empty));
                });

            var result = await _service.GenerateAsync(cts.Token);

            Assert.That(result.Status, Is.EqualTo(ReportGenerationStatus.Canceled));
        }
    }
}

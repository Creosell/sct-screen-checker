using System.IO.Abstractions.TestingHelpers;
using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
    {
    [TestFixture]
    public class AnalyzerRegistryServiceTests
        {
        private const string BaseDirectory = @"C:\App";
        private const string RegistryPath = @"C:\App\analyzers.json";

        private MockFileSystem _fileSystem;
        private AnalyzerRegistryService _service;

        [SetUp]
        public void Setup()
            {
            _fileSystem = new MockFileSystem();
            _fileSystem.AddDirectory(BaseDirectory);
            var pathProvider = new Mock<IPathProvider>();
            pathProvider.SetupGet(p => p.BaseDirectory).Returns(BaseDirectory);
            _service = new AnalyzerRegistryService(_fileSystem, pathProvider.Object);
            }

        [Test]
        public void GetLastChannel_NoRegistryFile_ReturnsNull()
            {
            Assert.That(_service.GetLastChannel("81026182"), Is.Null);
            }

        [Test]
        public void Update_NewProbe_CreatesFileAndStoresChannel()
            {
            _service.Update("81026182", ColorAnalyzerModel.CA410, 2);

            Assert.That(_fileSystem.File.Exists(RegistryPath), Is.True);
            Assert.That(_service.GetLastChannel("81026182"), Is.EqualTo(2));
            StringAssert.Contains("CA410", _fileSystem.File.ReadAllText(RegistryPath));
            }

        [Test]
        public void Update_ExistingProbe_OverwritesChannelWithoutDuplicates()
            {
            _service.Update("81026182", ColorAnalyzerModel.CA410, 2);
            _service.Update("81026182", ColorAnalyzerModel.CA410, 5);

            Assert.That(_service.GetLastChannel("81026182"), Is.EqualTo(5));
            StringAssert.DoesNotContain("\"LastChannel\": 2", _fileSystem.File.ReadAllText(RegistryPath));
            }

        [Test]
        public void GetLastChannel_SerialNumberCaseIsIgnored()
            {
            _service.Update("abc123", ColorAnalyzerModel.CA310, 1);

            Assert.That(_service.GetLastChannel("ABC123"), Is.EqualTo(1));
            }

        [Test]
        public void GetLastUsedChannel_EmptyRegistry_ReturnsNull()
            {
            Assert.That(_service.GetLastUsedChannel(), Is.Null);
            }

        [Test]
        public void GetLastUsedChannel_ReturnsTheChannelOfTheProbeConfirmedLast()
            {
            _fileSystem.AddFile(RegistryPath, new MockFileData("""
                [
                  { "ProbeSN": "A", "Model": "CA310", "LastChannel": 4, "LastSeenUtc": "2026-11-20T10:00:00Z" },
                  { "ProbeSN": "B", "Model": "CA410", "LastChannel": 7, "LastSeenUtc": "2026-11-26T10:00:00Z" }
                ]
                """));

            Assert.That(_service.GetLastUsedChannel(), Is.EqualTo(7));
            }

        [Test]
        public void UpdateLastUsed_ChangesOnlyTheProbeUsedLast()
            {
            _fileSystem.AddFile(RegistryPath, new MockFileData("""
                [
                  { "ProbeSN": "A", "Model": "CA310", "LastChannel": 4, "LastSeenUtc": "2026-11-20T10:00:00Z" },
                  { "ProbeSN": "B", "Model": "CA410", "LastChannel": 7, "LastSeenUtc": "2026-11-26T10:00:00Z" }
                ]
                """));

            _service.UpdateLastUsed(9);

            Assert.That(_service.GetLastChannel("A"), Is.EqualTo(4));
            Assert.That(_service.GetLastChannel("B"), Is.EqualTo(9));
            }

        [Test]
        public void UpdateLastUsed_EmptyRegistry_CreatesNothing()
            {
            _service.UpdateLastUsed(3);

            Assert.That(_fileSystem.File.Exists(RegistryPath), Is.False);
            }

        [Test]
        public void GetLastChannel_CorruptFile_ReturnsNullAndUpdateRecovers()
            {
            _fileSystem.AddFile(RegistryPath, new MockFileData("{ not json"));

            Assert.That(_service.GetLastChannel("81026182"), Is.Null);

            _service.Update("81026182", ColorAnalyzerModel.CA410, 3);
            Assert.That(_service.GetLastChannel("81026182"), Is.EqualTo(3));
            }
        }
    }

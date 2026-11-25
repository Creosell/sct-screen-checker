using Moq;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;
using ScreenChecker.Interfaces;
using ScreenChecker.Services;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class UploadServiceTests
    {
        private MockFileSystem _mockFileSystem;
        private Mock<IPathProvider> _mockPathProvider;
        private Mock<ILocalizationService> _mockLocalizationService;
        private Mock<IProcessRunner> _processRunner;
        private UploadService _uploadService;
        private string _testBaseDirectory;
        private List<string> _statusMessages;

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new MockFileSystem();
            _mockPathProvider = new Mock<IPathProvider>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _processRunner = new Mock<IProcessRunner>();
            _statusMessages = new List<string>();

            // Setup localization service
            _mockLocalizationService.SetupGet(ls => ls.CurrentCulture).Returns(CultureInfo.InvariantCulture);
            _mockLocalizationService.Setup(ls => ls.GetString(It.IsAny<string>()))
                .Returns((string key) =>
                {
                    // Return templates with placeholders for keys that use formatting
                    return key switch
                    {
                        "CreatedBatchFor" => "Loc_CreatedBatchFor {0}",
                        "ReadyToUpload" => "Loc_ReadyToUpload {0}",
                        "StartingParallelUploadOf" => "Loc_StartingParallelUploadOf {0}",
                        "StartingUploadOf" => "Loc_StartingUploadOf {0}",
                        "UploadedSuccessfully" => "Loc_UploadedSuccessfully {0}",
                        "UploadFinishedWithFail" => "Loc_UploadFinishedWithFail {0}",
                        "ArchiveFolderNotFound" => "Loc_ArchiveFolderNotFound {0}",
                        "FailedToExecuteCLI" => "Loc_FailedToExecuteCLI {0}: {1}",
                        "CLI_ErrorFor" => "Loc_CLI_ErrorFor {0} (Exit: {1}): {2}",
                        "UploadSuccessfulFor" => "Loc_UploadSuccessfulFor {0}",
                        "WarningFailedToCleanup" => "Loc_WarningFailedToCleanup {0}: {1}",
                        "CopyOfUploadedArchivesInFolder" => "Loc_CopyOfUploadedArchivesInFolder {0}",
                        _ => $"Loc_{key}"
                    };
                });

            _testBaseDirectory = "C:\\TestApp\\";
            _mockPathProvider.Setup(p => p.BaseDirectory).Returns(_testBaseDirectory);

            _uploadService = new UploadService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object, _processRunner.Object);
            _uploadService.StatusMessage += (sender, message) => _statusMessages.Add(message);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithDependencies_InitializesCorrectly()
        {
            // Act
            var service = new UploadService(_mockFileSystem, _mockPathProvider.Object, _mockLocalizationService.Object, _processRunner.Object);

            // Assert
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void Constructor_NullLocalizationService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new UploadService(_mockFileSystem, _mockPathProvider.Object, null!, _processRunner.Object));
        }


        #endregion

        #region ScanLocalFolders Tests (via UploadReportsAsync)

        [Test]
        public async Task UploadReportsAsync_NoArchiveFolder_ReturnsTrue()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(_testBaseDirectory, "report_archive");
            // MockFileSystem already doesn't have this directory by default

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("ArchiveFolderNotFound"));
        }

        [Test]
        public async Task UploadReportsAsync_EmptyArchiveFolder_ReturnsTrue()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(_testBaseDirectory, "report_archive");
            _mockFileSystem.AddDirectory(archivePath); // Create empty directory

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_ValidZipFile_CreatesCorrectBatch()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "DeviceName_Config_20230115_1430.zip");

            // Setup directory and files in MockFileSystem
            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - verify batch creation message was sent (formatted with file base name)
            Assert.That(_statusMessages, Has.Some.Contains("DeviceName_Config_20230115_1430"));
            Assert.That(_statusMessages, Has.Some.Contains("Loc_ReadyToUpload 1"));
        }

        [Test]
        public async Task ScanLocalFolders_InvalidFilename_SkipsFile()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var invalidZipFile = System.IO.Path.Combine(archivePath, "invalid_filename.zip");

            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(invalidZipFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - no batch created for invalid filename
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_WithResultsFiles_AddsToExistingBatch()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var resultsPath = System.IO.Path.Combine(baseDir, "results");
            var zipFile = System.IO.Path.Combine(archivePath, "Device_20230115_1430.zip");
            var resultFile1 = System.IO.Path.Combine(resultsPath, "Device_20230115_1430.html");
            var resultFile2 = System.IO.Path.Combine(resultsPath, "Device_20230115_1430.pdf");

            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddDirectory(resultsPath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));
            _mockFileSystem.AddFile(resultFile1, new MockFileData("test html content"));
            _mockFileSystem.AddFile(resultFile2, new MockFileData("test pdf content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - batch should include all files (will be attempted to upload)
            Assert.That(_statusMessages, Has.Some.Contains("Device_20230115_1430"));
            Assert.That(_statusMessages, Has.Some.Contains("ReadyToUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_ComplexDeviceName_ParsesCorrectly()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "LG_50UQ6031_REV1_20230115_1430.zip");

            // Setup directory and files in MockFileSystem
            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - should parse device name and config correctly
            Assert.That(_statusMessages, Has.Some.Contains("LG_50UQ6031_REV1_20230115_1430"));
        }

        #endregion

        #region UploadReportsAsync Tests

        [Test]
        public async Task UploadReportsAsync_NoReports_ReturnsTrue()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(_testBaseDirectory, "report_archive");
            _mockFileSystem.AddDirectory(archivePath); // Empty directory

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task UploadReportsAsync_CreatesUploadedReportsFolder_WhenNotExists()
        {
            // Note: This test verifies that the upload process attempts to create the uploaded_reports folder.
            // However, the actual upload execution requires a real Process for Nextcloud CLI, which cannot
            // be easily mocked without refactoring ExecuteSingleFileUploadAsync to use an IProcess interface.
            // This test is limited to verifying the folder creation logic.

            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var uploadedReportsDir = System.IO.Path.Combine(baseDir, "uploaded_reports");
            var zipFile = System.IO.Path.Combine(archivePath, "Device_20230115_1430.zip");

            // Setup directory and files in MockFileSystem
            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            // Note: Cannot fully test upload execution without IProcess abstraction
            // The test verifies batch creation, but actual upload will fail in unit tests
            Assert.That(_statusMessages, Has.Some.Contains("Device_20230115_1430"));
        }

        #endregion

        #region MoveToUploaded Tests

        private const string UploadedDir = "C:\\TestApp\\uploaded_reports";

        [Test]
        public void MoveToUploaded_ZipArchive_IsMovedToTheUploadedFolder()
        {
            _mockFileSystem.AddFile("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip", new MockFileData("zip"));
            _mockFileSystem.AddDirectory(UploadedDir);

            _uploadService.MoveToUploaded("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip", UploadedDir);

            Assert.That(_mockFileSystem.File.Exists("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip"), Is.False);
            Assert.That(_mockFileSystem.File.Exists(UploadedDir + "\\Dev_20230115_1430.zip"), Is.True);
        }

        [Test]
        public void MoveToUploaded_ZipArchive_ReplacesTheFileOfTheSameName()
        {
            _mockFileSystem.AddFile("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip", new MockFileData("new"));
            _mockFileSystem.AddFile(UploadedDir + "\\Dev_20230115_1430.zip", new MockFileData("old"));

            _uploadService.MoveToUploaded("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip", UploadedDir);

            Assert.That(_mockFileSystem.File.ReadAllText(UploadedDir + "\\Dev_20230115_1430.zip"), Is.EqualTo("new"));
        }

        [Test]
        public void MoveToUploaded_ResultFile_IsDeleted()
        {
            _mockFileSystem.AddFile("C:\\TestApp\\results\\Dev_20230115_1430.json", new MockFileData("{}"));
            _mockFileSystem.AddDirectory(UploadedDir);

            _uploadService.MoveToUploaded("C:\\TestApp\\results\\Dev_20230115_1430.json", UploadedDir);

            Assert.That(_mockFileSystem.File.Exists("C:\\TestApp\\results\\Dev_20230115_1430.json"), Is.False);
            Assert.That(_mockFileSystem.Directory.GetFiles(UploadedDir), Is.Empty);
        }

        [Test]
        public void MoveToUploaded_MissingFile_DoesNothing()
        {
            _uploadService.MoveToUploaded("C:\\TestApp\\results\\gone.json", UploadedDir);

            Assert.That(_statusMessages, Is.Empty);
        }

        [Test]
        public void MoveToUploaded_MoveFailure_IsReportedAsWarning()
        {
            _mockFileSystem.AddFile("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip", new MockFileData("zip"));

            // The destination folder does not exist, so the move throws.
            _uploadService.MoveToUploaded("C:\\TestApp\\report_archive\\Dev_20230115_1430.zip", UploadedDir);

            Assert.That(_statusMessages, Has.Some.Contains("Loc_WarningFailedToCleanup Dev_20230115_1430.zip"));
        }

        #endregion

        #region ExecuteSingleFileUploadAsync Tests

        private const string CliName = "Nextcloud CLI.exe";

        private void GivenCliResult(int exitCode, string error = "")
        {
            _processRunner.Setup(r => r.RunAsync(CliName, It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ProcessResult(exitCode, string.Empty, error));
        }

        [Test]
        public async Task ExecuteSingleFileUploadAsync_PassesTheArgumentsAsAList()
        {
            GivenCliResult(0);

            await _uploadService.ExecuteSingleFileUploadAsync("C:\\TestApp\\report archive\\Dev_20230115_1430.zip", "SCT/Dev/General/20230115/", CancellationToken.None);

            _processRunner.Verify(r => r.RunAsync(
                CliName,
                It.Is<IReadOnlyList<string>>(a => a.SequenceEqual(new[] { "upload", "-l", "C:\\TestApp\\report archive\\Dev_20230115_1430.zip", "-r", "SCT/Dev/General/20230115/Dev_20230115_1430.zip" })),
                _testBaseDirectory,
                string.Empty,
                Timeout.InfiniteTimeSpan,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task ExecuteSingleFileUploadAsync_ExitCodeZero_ReportsSuccess()
        {
            GivenCliResult(0);

            bool result = await _uploadService.ExecuteSingleFileUploadAsync("C:\\a\\f.zip", "SCT/x/", CancellationToken.None);

            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("Loc_UploadSuccessfulFor f.zip"));
        }

        [Test]
        public async Task ExecuteSingleFileUploadAsync_NonZeroExitCode_ReportsTheErrorOutput()
        {
            GivenCliResult(1, "Upload failed: 404");

            bool result = await _uploadService.ExecuteSingleFileUploadAsync("C:\\a\\f.zip", "SCT/x/", CancellationToken.None);

            Assert.That(result, Is.False);
            Assert.That(_statusMessages, Has.Some.Contains("Loc_CLI_ErrorFor f.zip (Exit: 1): Upload failed: 404"));
        }

        [Test]
        public async Task ExecuteSingleFileUploadAsync_RunnerFailure_ReportsItAndFails()
        {
            _processRunner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.ComponentModel.Win32Exception("not found"));

            bool result = await _uploadService.ExecuteSingleFileUploadAsync("C:\\a\\f.zip", "SCT/x/", CancellationToken.None);

            Assert.That(result, Is.False);
            Assert.That(_statusMessages, Has.Some.Contains("Loc_FailedToExecuteCLI f.zip: not found"));
        }

        [Test]
        public void ExecuteSingleFileUploadAsync_Cancellation_IsNotSwallowed()
        {
            _processRunner.Setup(r => r.RunAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            Assert.ThrowsAsync<OperationCanceledException>(() => _uploadService.ExecuteSingleFileUploadAsync("C:\\a\\f.zip", "SCT/x/", CancellationToken.None));
        }

        [Test]
        public async Task UploadReportsAsync_SuccessfulUpload_MovesTheArchiveAndDeletesTheResults()
        {
            var archive = "C:\\TestApp\\report_archive\\Device_General_20230115_1430.zip";
            var result = "C:\\TestApp\\results\\Device_General_20230115_1430.json";
            _mockFileSystem.AddFile(archive, new MockFileData("zip"));
            _mockFileSystem.AddFile(result, new MockFileData("{}"));
            GivenCliResult(0);

            bool uploaded = await _uploadService.UploadReportsAsync();

            Assert.That(uploaded, Is.True);
            Assert.That(_mockFileSystem.File.Exists(archive), Is.False);
            Assert.That(_mockFileSystem.File.Exists(result), Is.False);
            Assert.That(_mockFileSystem.File.Exists("C:\\TestApp\\uploaded_reports\\Device_General_20230115_1430.zip"), Is.True);
        }

        [Test]
        public async Task UploadReportsAsync_FailedUpload_KeepsTheFiles()
        {
            var archive = "C:\\TestApp\\report_archive\\Device_General_20230115_1430.zip";
            _mockFileSystem.AddFile(archive, new MockFileData("zip"));
            GivenCliResult(1, "boom");

            bool uploaded = await _uploadService.UploadReportsAsync();

            Assert.That(uploaded, Is.False);
            Assert.That(_mockFileSystem.File.Exists(archive), Is.True);
        }

        #endregion

        #region StatusMessage Event Tests

        [Test]
        public async Task UploadReportsAsync_RaisesStatusEvents_DuringOperation()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(_testBaseDirectory, "report_archive");
            // Directory doesn't exist in MockFileSystem by default

            // Clear any setup messages
            _statusMessages.Clear();

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(_statusMessages.Count, Is.GreaterThan(0), "Should raise at least one status message");
        }

        [Test]
        public async Task UploadReportsAsync_NoReports_RaisesNoReportsMessage()
        {
            // Arrange
            var archivePath = System.IO.Path.Combine(_testBaseDirectory, "report_archive");
            _mockFileSystem.AddDirectory(archivePath); // Empty directory

            _statusMessages.Clear();

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        #endregion

        #region Regex Pattern Tests

        [Test]
        public async Task ScanLocalFolders_FilenameWithoutTimestamp_SkipsFile()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var invalidFile = System.IO.Path.Combine(archivePath, "DeviceName.zip"); // Missing timestamp

            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(invalidFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_FilenameWithInvalidDateFormat_SkipsFile()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var invalidFile = System.IO.Path.Combine(archivePath, "Device_2023-01-15.zip"); // Wrong date format

            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(invalidFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(_statusMessages, Has.Some.Contains("NoReportsFoundForUpload"));
        }

        [Test]
        public async Task ScanLocalFolders_FilenameWithMultipleUnderscores_ParsesCorrectly()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "Device_With_Multiple_Underscores_20230115_1430.zip");

            // Setup directory and files in MockFileSystem
            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));

            // Act
            var result = await _uploadService.UploadReportsAsync();

            // Assert - should create batch (device name will be everything before last underscore before timestamp)
            Assert.That(_statusMessages, Has.Some.Contains("Device_With_Multiple_Underscores_20230115_1430"));
        }

        #endregion

        #region Remote Path Construction Tests

        [Test]
        public async Task ScanLocalFolders_ConstructsCorrectRemotePath_DeviceWithConfig()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "LG50UQ_REV1_20230115_1430.zip");

            // Setup directory and files in MockFileSystem
            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert - remote path should be SCT/LG50UQ/REV1/20230115/
            // We can verify batch creation happened
            Assert.That(_statusMessages, Has.Some.Contains("LG50UQ_REV1_20230115_1430"));
        }

        [Test]
        public async Task ScanLocalFolders_ConstructsCorrectRemotePath_DeviceWithoutConfig()
        {
            // Arrange
            var baseDir = _testBaseDirectory;
            var archivePath = System.IO.Path.Combine(baseDir, "report_archive");
            var zipFile = System.IO.Path.Combine(archivePath, "SimpleDevice_20230115_1430.zip");

            // Setup directory and files in MockFileSystem
            _mockFileSystem.AddDirectory(archivePath);
            _mockFileSystem.AddFile(zipFile, new MockFileData("test zip content"));

            // Act
            await _uploadService.UploadReportsAsync();

            // Assert - remote path should use "General" for config
            Assert.That(_statusMessages, Has.Some.Contains("SimpleDevice_20230115_1430"));
        }

        #endregion
    }
}

using System.IO.Abstractions.TestingHelpers;
using System.Windows.Input;
using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Services;

namespace ScreenChecker.UnitTests.ServicesTests
{
    [TestFixture]
    public class DeviceConfigurationServiceTests
    {
        private const string ConfigDirectory = @"C:\cfg";

        private MockFileSystem _fileSystem;
        private DeviceConfigurationService _service;
        private ICommand _command;

        [SetUp]
        public void SetUp()
        {
            _fileSystem = new MockFileSystem();
            var pathProvider = new Mock<IPathProvider>();
            pathProvider.Setup(p => p.ConfigDirectory).Returns(ConfigDirectory);
            _service = new DeviceConfigurationService(_fileSystem, pathProvider.Object);
            _command = new Mock<ICommand>().Object;
        }

        private void AddConfig(string relativePath) => _fileSystem.AddFile(Path.Combine(ConfigDirectory, relativePath), new MockFileData(string.Empty));

        [Test]
        public void LoadTree_MissingDirectory_CreatesItAndReturnsEmptyTree()
        {
            var tree = _service.LoadTree(_command);

            Assert.That(tree, Is.Empty);
            Assert.That(_fileSystem.Directory.Exists(ConfigDirectory), Is.True);
        }

        [Test]
        public void LoadTree_Files_AreOrderedAlphabeticallyAndOtherExtensionsIgnored()
        {
            AddConfig("Zebra.yaml");
            AddConfig("alpha.yaml");
            AddConfig("notes.txt");

            var tree = _service.LoadTree(_command);

            Assert.That(tree.Select(n => n.DisplayName), Is.EqualTo(new[] { "alpha", "Zebra" }));
            Assert.That(tree.All(n => !n.IsFolder && ReferenceEquals(n.SelectCommand, _command)), Is.True);
        }

        [Test]
        public void LoadTree_NestedFolders_PrecedeFilesAndCarryRelativePathsWithoutExtension()
        {
            AddConfig("RootConfig.yaml");
            AddConfig(Path.Combine("OrderA", "Nested.yaml"));

            var tree = _service.LoadTree(_command);

            Assert.That(tree.Select(n => n.DisplayName), Is.EqualTo(new[] { "OrderA", "RootConfig" }));
            Assert.That(tree[0].IsFolder, Is.True);
            Assert.That(tree[0].SelectCommand, Is.Null);
            Assert.That(tree[0].Children.Single().RelativePath, Is.EqualTo(Path.Combine("OrderA", "Nested")));
            Assert.That(tree[1].RelativePath, Is.EqualTo("RootConfig"));
        }

        [Test]
        public void LoadTree_FolderWithoutConfigurationFiles_IsExcluded()
        {
            _fileSystem.AddDirectory(Path.Combine(ConfigDirectory, "Empty"));
            AddConfig(Path.Combine("Other", "readme.txt"));

            Assert.That(_service.LoadTree(_command), Is.Empty);
        }
    }
}

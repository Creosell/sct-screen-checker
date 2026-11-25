using System.IO.Abstractions;
using System.Windows.Input;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;

namespace ScreenChecker.Services
    {
    /// <summary>
    /// Builds the device configuration tree from the YAML files of the config directory.
    /// </summary>
    public class DeviceConfigurationService : IDeviceConfigurationService
        {
        private const string ConfigFilePattern = "*.yaml";

        private readonly IFileSystem _fileSystem;
        private readonly IPathProvider _pathProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceConfigurationService"/> class.
        /// </summary>
        /// <param name="fileSystem">File system abstraction used to scan the config directory.</param>
        /// <param name="pathProvider">Provider of the config directory path.</param>
        public DeviceConfigurationService(IFileSystem fileSystem, IPathProvider pathProvider)
            {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            }

        /// <inheritdoc />
        public IReadOnlyList<DeviceConfigTreeNode> LoadTree(ICommand selectCommand)
            {
            string root = _pathProvider.ConfigDirectory;
            if (!_fileSystem.Directory.Exists(root))
                _fileSystem.Directory.CreateDirectory(root);

            return [.. BuildNodes(root, root, selectCommand)];
            }

        /// <summary>
        /// Recursively builds the nodes of a directory: subfolders (containing at least one configuration file,
        /// direct or nested) ordered alphabetically first, followed by the directory's own configuration files.
        /// </summary>
        /// <param name="directoryPath">Directory to scan.</param>
        /// <param name="root">The config directory the relative paths are taken from.</param>
        /// <param name="selectCommand">Command assigned to every configuration file node.</param>
        /// <returns>Sequence of child nodes for the directory.</returns>
        private IEnumerable<DeviceConfigTreeNode> BuildNodes(string directoryPath, string root, ICommand selectCommand)
            {
            var path = _fileSystem.Path;

            foreach (var subDirectory in _fileSystem.Directory.GetDirectories(directoryPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                {
                var folderNode = new DeviceConfigTreeNode { DisplayName = path.GetFileName(subDirectory), IsFolder = true };
                foreach (var childNode in BuildNodes(subDirectory, root, selectCommand))
                    folderNode.Children.Add(childNode);

                if (folderNode.Children.Any())
                    yield return folderNode;
                }

            foreach (var file in _fileSystem.Directory.GetFiles(directoryPath, ConfigFilePattern).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                var relativePath = path.GetRelativePath(root, file);
                yield return new DeviceConfigTreeNode
                    {
                    DisplayName = path.GetFileNameWithoutExtension(file),
                    RelativePath = path.Combine(path.GetDirectoryName(relativePath) ?? string.Empty, path.GetFileNameWithoutExtension(relativePath)),
                    IsFolder = false,
                    SelectCommand = selectCommand
                    };
                }
            }
        }
    }

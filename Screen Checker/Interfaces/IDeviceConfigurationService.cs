using System.Windows.Input;
using ScreenChecker.Models;

namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// Discovers the device configuration files of the config directory.
    /// </summary>
    public interface IDeviceConfigurationService
    {
        /// <summary>
        /// Scans the config directory (created when missing) and builds the hierarchical selection tree:
        /// subfolders that contain at least one configuration file come first, followed by the files of the
        /// directory, both ordered alphabetically.
        /// </summary>
        /// <param name="selectCommand">Command assigned to every configuration file node.</param>
        /// <returns>The top level nodes of the tree; empty when no configuration file exists.</returns>
        IReadOnlyList<DeviceConfigTreeNode> LoadTree(ICommand selectCommand);
    }
}

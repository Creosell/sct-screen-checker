using ScreenChecker.Models;

namespace ScreenChecker.Interfaces
{
    /// <summary>
    /// Persistent registry of known color analyzer probes and the channel last used with each of them.
    /// </summary>
    public interface IAnalyzerRegistryService
    {
        /// <summary>
        /// Gets the channel last confirmed for the probe.
        /// </summary>
        /// <param name="probeSN">Probe serial number.</param>
        /// <returns>The last used channel, or null when the probe has not been seen before.</returns>
        int? GetLastChannel(string probeSN);

        /// <summary>
        /// Creates or updates the registry entry for the probe and saves the registry.
        /// </summary>
        /// <param name="probeSN">Probe serial number.</param>
        /// <param name="model">Analyzer family the probe is connected through.</param>
        /// <param name="channel">Channel confirmed by the inspector.</param>
        void Update(string probeSN, ColorAnalyzerModel model, int channel);

        /// <summary>
        /// Gets the channel of the probe that was used last (the most recently confirmed one).
        /// </summary>
        /// <returns>The channel, or null when no probe has been registered yet.</returns>
        int? GetLastUsedChannel();

        /// <summary>
        /// Stores a channel the inspector chose in the settings for the probe that was used last, so the registry keeps following
        /// the channel the probe is set to. Does nothing while no probe is registered.
        /// </summary>
        /// <param name="channel">The chosen channel.</param>
        void UpdateLastUsed(int channel);
    }
}

#if DEBUG
using ScreenChecker.Models;

namespace ScreenChecker.DebugTools
    {
    /// <summary>
    /// How the simulated color analyzer behaves (a point of the measurement sequence that fails, is repeated and so on).
    /// </summary>
    public enum SimulationScenario
        {
        /// <summary>Every point is measured within the specification.</summary>
        Ok,

        /// <summary>White, black and red are bad at the first attempt and get better with every repeat.</summary>
        Retry,

        /// <summary>White, black and red stay outside the specification at every attempt (the contrast fails with them).</summary>
        Fail,

        /// <summary>The top right point is too dim for the brightness uniformity at the first attempt and gets brighter with every repeat.</summary>
        Uniformity,

        /// <summary>The first attempt of every point is rejected by the validation (the probe is not on the screen); the repeat is good.</summary>
        Reject
        }

    /// <summary>
    /// Options of the simulated color analyzer, read from the command line of a Debug build:
    /// <c>--simulate[=ca410|ca310] [--scenario=ok|retry|fail|reject|uniformity] [--probe-sn=NUMBER]</c>.
    /// </summary>
    /// <param name="Model">Analyzer family the simulator reports.</param>
    /// <param name="Scenario">Values the simulator measures.</param>
    /// <param name="ProbeSerialNumber">Serial number of the simulated probe.</param>
    public sealed record SimulationOptions(ColorAnalyzerModel Model, SimulationScenario Scenario, string ProbeSerialNumber)
        {
        private const string SimulateFlag = "--simulate";
        private const string ScenarioFlag = "--scenario=";
        private const string ProbeFlag = "--probe-sn=";

        /// <summary>
        /// Reads the simulation options from the command line.
        /// </summary>
        /// <param name="args">Command line arguments of the application.</param>
        /// <returns>The options, or null when the command line does not ask for the simulation.</returns>
        public static SimulationOptions? Parse(IEnumerable<string> args)
            {
            var list = args.ToList();
            string? simulate = list.FirstOrDefault(a => a.StartsWith(SimulateFlag, StringComparison.OrdinalIgnoreCase));
            if (simulate is null) return null;

            var model = simulate.EndsWith("=ca310", StringComparison.OrdinalIgnoreCase) ? ColorAnalyzerModel.CA310 : ColorAnalyzerModel.CA410;
            var scenario = Enum.TryParse(Value(list, ScenarioFlag), ignoreCase: true, out SimulationScenario parsed) ? parsed : SimulationScenario.Ok;
            string probe = Value(list, ProbeFlag) ?? "12345678";
            return new SimulationOptions(model, scenario, probe);
            }

        private static string? Value(List<string> args, string flag) =>
            args.FirstOrDefault(a => a.StartsWith(flag, StringComparison.OrdinalIgnoreCase))?[flag.Length..];
        }
    }
#endif

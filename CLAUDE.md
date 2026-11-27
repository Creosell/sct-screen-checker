# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WPF desktop application for LCD color measurement testing using Konica Minolta CA-200/CA-310 (COM) and CA-410 (CA-SDK2) color analyzers. Built with .NET 8.0, WPF, and Material Design themes.

## Build & Test Commands

**Build (Visual Studio required due to COM interop):**
```
msbuild "Screen Checker.sln" /p:Configuration=Debug
```

**Run Tests:**
```
dotnet test "Screen Checker.UnitTests\Screen Checker.UnitTests.csproj"
```

**Run Single Test:**
```
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

**Note:** The project uses COM interop for the Konica Minolta color analyzer device. Building via `dotnet build` may fail with ResolveComReference errors - use Visual Studio or MSBuild directly.

## Architecture

**Pattern:** MVVM (Model-View-ViewModel)

**Entry Point:** `App.xaml.cs` configures `Microsoft.Extensions.DependencyInjection` container and resolves `MainWindowViewModel` as the application root.

**Key Layers:**

- **Views/** - XAML UI (MainWindow, MeasurementView, SettingsView)
- **ViewModels/** - Presentation logic with RelayCommand for UI binding; the files of a page live in its subfolder with a matching namespace (`MeasurementPage/` → `ScreenChecker.ViewModels.MeasurementPage`, `SettingsPage/` → `ScreenChecker.ViewModels.SettingsPage`; the folder is not called `Measurement` because the namespace would hide the `Measurement` model), `MainWindowViewModel` stays in the root; the tests mirror this in `ViewModelsTests/`
- **Models/** - Data classes (Measurement, DeviceUnderTest, AppSettings)
- **Services/** - Business logic, all interface-based for testability
- **Wrappers/** - Abstractions over system dependencies (COM device, file system, dispatcher)
- **Interfaces/** - Service contracts for dependency injection

**Core Services:**

| Service | Purpose |
|---------|---------|
| IColorMeasurementService | Color analyzer device access: CA-410 via CA-SDK2, CA-310 via COM |
| IAnalyzerSession | The work with the analyzer for the measurement page (singleton): connection state, zero calibration with the channel confirmation (`CalibrateAsync` reports a typed `CalibrationOutcome` while the run is still in progress), measuring; wraps `IColorMeasurementService`, `IAnalyzerRegistryService` and the channel dialog |
| IAnalyzerRegistryService | Last confirmed channel per probe serial number (`analyzers.json` next to the exe): the only place the channel is stored (not in `appsettings.json`); the channel of the probe used last is applied at the start, a channel chosen in the settings is written back, the calibration warns when the probe holds another one |
| ISettingsService | App configuration (singleton) |
| ILocalizationService | Runtime language switching (singleton) |
| IFileService | File I/O abstraction, JSON/CSV export |
| IUploadService | Parallel report upload to Nextcloud: runs `Nextcloud CLI.exe` through `IProcessRunner`, moves uploaded archives through `IFileSystem` |
| IDialogService | File dialogs, message boxes |
| IMeasurementStatusService | Measurement state tracking |
| IAttemptCounter | Rejected measurement attempts per point (instance state, no statics) |
| IMeasurementAcceptance | Plausibility of a measurement (luminance, coordinates) and the retry policy: returns `Accepted`, `Rejected(reason, attempt)` or `ConfirmationNeeded(reason)`; dialogs and log stay in the view model |
| IDeviceConfigurationService | Scans the config directory and builds the device configuration tree (`IFileSystem`) |
| IReportGenerationService | Runs `Report Generator.exe` through `IProcessRunner`: exit code to status, cancellation kills the process tree |
| ISpecCheckCoordinator | Specification check of the measurement page (transient): builds the request, runs `ISpecEvaluationService`, answers only the latest request and delivers a typed `SpecCheckResult` on the UI thread |
| ILogService | Application log (singleton, registered without `TracingProxy`): text, warning/error entries of the Log tab, unread counter; the view model exposes it as `LogService` |
| IPathProvider | Centralized path resolution (BaseDirectory, ConfigDirectory, DataDirectory) |

**Testability:** All system dependencies (file system, COM device, MessageBox, WPF Dispatcher) are wrapped in interfaces injected via DI. File system uses `System.IO.Abstractions` (`IFileSystem` / `MockFileSystem`). COM device wrapped in `Ca310MemoryWrapper` / `Ca310ProbeWrapper` with proper `Marshal.ReleaseComObject` cleanup.

**Dependency Injection:** `Microsoft.Extensions.DependencyInjection` container configured in `App.xaml.cs` → `ConfigureServices()`. Singletons: settings, localization, path provider, measurement status. Transients: ViewModels. All services use constructor injection — no static singletons.

**Path Management:** `IPathProvider` / `PathProvider` — single source of truth for `BaseDirectory`, `ConfigDirectory`, `DataDirectory`. No hardcoded `AppDomain.CurrentDomain.BaseDirectory`.

**Constants:** `AppConstants` class. `ColorAnalyzer.MinChannel` = 0, `MaxChannel` = 99. `MeasurementAcceptance.ColorCoordinatesTolerance` = 0.15.

**Feedback animations:** `ActionFeedbackSignal` (Models) is raised by a ViewModel when an action finished; `ActionFeedbackBehavior` (Views/Behaviors) plays the green/red breathing on any element via `behaviors:ActionFeedbackBehavior.Signal="{Binding ...}"` (modes Fill: the background breathes, a glow pulses and the label names the outcome; Underline, Overlay: a line; Glow: the glow only, used by the measurement buttons). No code-behind is needed in a page.

**Themes:** `IThemeService` (singleton) resolves `ThemeMode` (Dark, Light, System), applies it through `IThemeApplier` (`WpfThemeApplier`: Material Design base theme plus the dictionary `Resources/Themes/Theme.{Dark|Light}.xaml`) and stores it via `ISettingsService.UpdateTheme`. Views use `{DynamicResource App...|Status...|Log...}` keys, never hardcoded page colors; both theme files must define the same keys (a unit test checks it). The colors of the feedback animations are theme keys too (`FeedbackOk`, `FeedbackErr`, `FeedbackGlowOk`, `FeedbackGlowErr`, read by `FeedbackPalette`), kept at a contrast of 3:1 on the light page and 2:1 on the dark one (a unit test checks it).

**Shared control styles:** `Resources/Styles/FieldStyles.xaml` (merged in `App.xaml`) holds the look of the input controls of every page: `UnderlinedTextBoxStyle`, `UnderlinedComboBoxStyle`, `UnderlinedDropdownButtonStyle` (a one pixel underline of the same thickness in every state, only the hover recolors it) and `ActionButtonStyle` (rounded action button); the text size of the pages is the resource `AppFontSize`. Pages take these styles and do not restyle the controls on their own.

**Typography:** the font and the type scale are resources read with `DynamicResource` (`AppFontFamily`, `AppFontSize`, `FontSizeCaption` 12, `FontSizeBody` 13, `FontSizeBodyLarge` 14, `FontSizeEmphasis` 15; defaults in `FieldStyles.xaml`); a language overrides them in `Resources/Typography.<language>.xaml`, which `App.LocalizationService_LanguageChanged` merges on top (today only `zh-Hans`: Microsoft YaHei UI and +2 points for the scale, `AppFontSize` unchanged). Pages use these keys instead of literal sizes; the numeric cells of the Results table stay at a literal 13 because their columns have a fixed width.

**Debug trace:** in the Debug configuration only (`#if DEBUG`, nothing of it is in a Release build) `DebugTools/` writes one timestamped line per event to `logs/debug_yyyyMMdd_HHmmss.log` next to the exe (the last 10 runs are kept) and to the debugger output: user input and interface events (`UiEventTracer`: mouse, keys, clicks, selections, typed text, opened lists and menus, windows, failed bindings, unhandled exceptions), every `RelayCommand` run (`RelayCommandTrace`), every call of a service interface with arguments, result and time (`TracingProxy`, wrapped in `App.AddService`) and every property change of a view model (`ViewModelTracer`, in `App.AddViewModel`). New code needs no log calls of its own; register services through `AddService` and view models through `AddViewModel`.

**Simulated analyzer:** in the Debug configuration only, `Screen Checker.exe --simulate[=ca410|ca310] [--scenario=ok|retry|fail|reject|uniformity] [--probe-sn=NUMBER] [--channel-name=NAME]` replaces the color analyzer with `DebugTools/SimulatedColorMeasurementService`: it connects, calibrates and measures with the delays, statuses and events of the real one, the point being measured is read from the measurement button that is in the measuring state, and the values are those of a real TV batch (`retry`: white, black and red are bad at the first attempt and better at every repeat; `fail`: they stay bad; `reject`: the first attempt of every point is refused by the validation, the repeat is good; `uniformity`: the top right point is too dim for the brightness uniformity at the first attempt and brighter at every repeat). It writes `appsettings.json`, `analyzers.json` and the data folder like the real one does.

**Commands:** ViewModels use `RelayCommand` (in `Commands/`) for ICommand implementation binding to UI actions.

## Project Map (graphify)

Knowledge graph of the codebase is in `graphify-out/`. Only `graph.json` is tracked in git (rebuilt and added to the commit by the `pre-commit` hook, merged by the `graphify` merge driver); `GRAPH_REPORT.md`, `graph.html` and the rest are untracked and rebuilt locally. Use it to navigate before broad grep searches.

- Start with `graphify-out/GRAPH_REPORT.md` (community hubs, most connected nodes).
- CLI: `C:\Users\Pavel\.claude\tools\graphify\venv\Scripts\graphify.exe` (not in PATH):
  - `graphify explain "MeasurementViewModel"` — node and its neighbors
  - `graphify path "A" "B"` — shortest path between two nodes
  - `graphify update .` — rebuild the graph after code changes (AST only, no LLM)
- Freshness: compare `Built from commit` in `GRAPH_REPORT.md` with `git rev-parse HEAD`; run `graphify update .` if stale.

## Configuration

Device test specifications are YAML files in `Screen Checker/config/device_configs/` defining min/max/typical values for brightness, contrast, color gamut (RGB/NTSC area), RGB primaries (x/y coordinates), white point, and color temperature. Each config specifies acceptable ranges under `main_tests`.

## Version

Current version: **1.4.5** (set in `Screen Checker\Screen Checker.csproj` — `AssemblyVersion` / `FileVersion`). See [CHANGELOG.md](CHANGELOG.md) for history.

## Localization

Supports English (default), Chinese Simplified and Russian. Resources in `Screen Checker/Resources/StringResources.xaml` and `StringResources.zh-Hans.xaml`. Language can be switched at runtime.

## Testing

- Framework: NUnit 3.14 with Moq 4.20, `System.IO.Abstractions.TestingHelpers` for file system mocking
- Tests mirror source structure in `Screen Checker.UnitTests/`
- Coverage: ViewModels, all Services (including COM lifecycle), Models, Converters, Commands
- Do **not** use `dotnet build` to build before running tests — use MSBuild due to COM interop
- The daily run (`default.runsettings`, set in the test project) skips the slow tests: `Category("Ui")` (`FieldStylesTests`, WPF style checks, ~27 s; the layout tests in `ViewsTests/Layout` and `ResultsTableLayoutTests`, ~10 s together) and `Category("Simulator")` (`SimulatedColorMeasurementServiceTests`, the Debug analyzer simulator with its real delays, ~19 s). Run everything before a release build and after changes to `FieldStyles.xaml` or `DebugTools/`: `dotnet test "Screen Checker.UnitTests\Screen Checker.UnitTests.csproj" --no-build --settings "Screen Checker.UnitTests\all.runsettings"`

**Text layout rule:** a text never gets a width of its own. It takes a star column and wraps (`TextWrapping="Wrap"`), or a column that is as wide as it needs (`Auto`, `SharedSizeGroup` for a table), so a longer translation or a longer value pushes the neighbors instead of running over them (the Device info name used to keep `MinWidth="120"` and was cut by the value in Chinese). A text that is cut with an ellipsis has a `ToolTip` with the whole text. `TextStyleRulesTests` keeps this on every run (`TextBlocks_AreNotSizedByAFixedWidth`, `FixedWidthColumns_AreTheReviewedOnes`, `TrimmedTexts_CanBeReadInAToolTip`); a deliberate exception is added to its lists, and the layout tests have to show that every language fits it.

**Display scale:** a screen with a large display scale leaves little room in layout pixels (full HD at 200%: 960x520, at 300%: 640x347). `MainWindow` takes `min(design size 1100x600, work area of the monitor it is on)` as its smallest and starting size (`UiMetrics.WorkAreaOf`, `WindowMonitor`: the work area of the monitor of the window in device-independent pixels, asked again when the window moves or the scale changes) and holds its content in `MinimumSizeViewbox`, which lays the page out at the design size at least and shrinks it in proportion when the room is less, never enlarging it. A maximized window takes the work area of its monitor: `MainWindow` answers `WM_GETMINMAXINFO` itself (position, size, smallest size in physical pixels, message handled), because left to the framework a window without a frame covers the taskbar and goes beyond the monitor. `app.manifest` makes the application Per-Monitor DPI aware (v2). The Debug flag `--ui-scale=N` (0.5 to 4) draws the interface N times larger to see another scale on the monitor at hand. `DpiLayoutTests` takes the resolutions from the connected displays (`Displays.Connected`, no list written into the tests), tries every display at every scale from 100% to 400% (`UiMetrics.WorkAreaOverride`), shows the window at its starting size and stretched over the whole work area, and audits the pages with the longest data.

**Layout tests:** `LayoutAudit` walks the visual tree of whatever is shown and reports every text that is cut (needs more room than its slot, shown with an ellipsis), lies outside an element it sits in (scroll viewers excepted along their scroll axis), overlaps another text or is covered by another element, and every single-line text box that does not show its text. `UiTestHost` is the base class (the first load of the resources in a process does not resolve the implicit button style the shared styles are based on, so it loads them once as a warm-up): the UI thread, the resources of a language (like `App.xaml` and the language switch), the view models with mocked dependencies, the main window and `Flush` (waits for the idle dispatcher, no fixed sleeps). `PageLayoutTests`, `WindowLayoutTests` (the real main window, both pages) and `DialogLayoutTests` fill the pages with the longest data (`WorstSpecEvaluation`, the widest values, a channel name of 10 wide characters, the longest configuration name) in English, Russian and Chinese at the smallest window (1100x600). A new view needs no new audit code: show it through `UiTestHost` and call `LayoutAudit.Run`; a text that the page trims on purpose is passed to `isAllowedToTrim`. `MeasurementView.TransitionSpeed` (internal) makes the analyzer card change at once, so the audit does not catch it half way. With the environment variable `SCT_UI_SHOTS` set to a folder every audited state is saved as a PNG (`Shot`): run the tests on a checkout of the old code and on the new one to get before/after pictures of what the audit found.

## Release

Released with the shared tool in the SCT Updater repository (`tools/release/app_release.py app <this repo> --upload`); `sct_app.toml` here describes the project (`kind = "dotnet"`, `FolderProfile` publish profile, `obsolete = ["WPF LCD Test.exe"]`: old exe names the Updater deletes on install; add the old name there whenever the exe is renamed). The tool builds with MSBuild (the COM reference of the analyzer needs it, `dotnet` cannot build this project), takes the version from the built `Screen Checker.exe` (`AssemblyVersion` in `Screen Checker.csproj`) and ships only the build output (never `appsettings.json`, `analyzers.json` or `data`, which the application writes itself). The former `tools/release/release_manager.py` is removed. Full process: `docs/module-release.md` in the SCT Updater repository.

## Communication Style & Code Standards

**Response Format:**
1. **CONCISENESS**: Minimal responses. No pleasantries ("Конечно", "Я могу помочь").
2. **FORMAT**: Use Markdown. Code blocks must specify language.
3. **SCOPE**: Return only changed code blocks with minimal context (3 lines above/below). DO NOT return full files unless explicitly requested.
4. **EXPLANATIONS**: Skip explanations for trivial changes. Use short bullet lists for complex logic.
5. **THINKING**: Do not output thinking process unless requested.

**Documentation & Comments:**
- Do not add explanatory comments from your perspective (e.g., 'I changed this because...').
- Use only final descriptive code comments.
- Every method must have detailed XML documentation in English.
- Comments must be concise, describing code behavior, without suggestions to user.

**Code Style & Logic:**
- Prefer elegant, concise, and clean code.
- Follow Separation of Concerns and Single Source of Truth principles.
- Avoid bad programming practices.

**Tone:**
- Communication in Russian, technical documentation strictly in English.
- Respond concisely. No fluff. No "Конечно", "Вот здесь". Output only code or direct answers. If explanation needed, use bullet lists.

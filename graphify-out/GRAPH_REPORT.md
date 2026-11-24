# Graph Report - WPF-LCD-Test  (2026-11-24)

## Corpus Check
- 135 files · ~78,474 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 8 file(s) not represented in the graph (top: .resx 3, (none) 2, .xml 1)

## Summary
- 2511 nodes · 5441 edges · 139 communities (98 shown, 41 thin omitted)
- Extraction: 88% EXTRACTED · 12% INFERRED · 0% AMBIGUOUS · INFERRED: 662 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `d9eecc17`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Resources
- Ca310ProbeWrapper
- Changelog
- DebugTrace.Format
- Window
- UserControl
- MeasurementViewModel
- DeviceUnderTest
- Task
- AnalyzerRegistryService
- SettingsViewModel
- release_manager.py
- MeasurementStatus
- SettingsViewModelTests
- SpecEvaluation
- FieldStylesTests
- ColorMeasurementService
- MainWindowViewModelTests
- SyncSetting
- Screen Checker.UnitTests.csproj
- RelayCommand
- WPF_LCD_Test.Interfaces
- ActionFeedbackBehavior
- DeviceUnderTest
- Measurement
- WPF_LCD_Test.ViewModels
- Ca410Wrapper
- IDisposable
- AppSettings
- LogHandlerTests
- MeasurementStatusService
- SimulatedColorMeasurementService
- Ca410ProbeWrapper
- UserControl
- ActionFeedbackBehavior.Play
- Task
- DeviceCardTransition
- ThemeDictionariesTests
- Screen Checker
- Button
- main_tests
- MessageDialogWindow
- system_windows
- ReportExitCode
- UiEventTracer.Install
- SimulatedColorMeasurementServiceTests
- Ca310MemoryWrapper
- UploadServiceTests
- DeviceCard
- Releasing
- MeasurementView
- Blue_x
- Blue_y
- Brightness
- Brightness_uniformity
- Cg_ntsc_area
- Cg_ntsc
- Cg_rgb_area
- Cg_rgb
- Contrast
- Delta_e
- Green_x
- Green_y
- Red_x
- Red_y
- Temperature
- White_y
- ChoiceDialogWindow
- Resources.Designer.cs
- SpecSummaryViewModel
- PathProvider
- MeasurementLocation
- Theme.Dark.xaml
- MainWindow
- Mock
- TextStyleRulesTests
- StringResources.xaml
- StringResources.zh-Hans.xaml
- WPF_LCD_Test.DebugTools
- SubmenuArrow
- Theme.Light.xaml
- IColorMeasurementService
- MeasurementView
- BreathingStoryboard
- DeviceCardContent
- Header
- MockMeasurementViewModel
- IUploadService
- Action
- AppConstants
- MeasurementViewModel.Log
- Window
- Ca310Wrapper
- CustomDateTimeConverterTests
- WPF_LCD_Test.Models
- App
- [1.4.0] - 2026-11-17
- ISettingsService
- Window
- BaseViewModel
- MainWindowViewModel
- UploadService
- ISettingsService
- MeasurementViewModel.UpdateUIState
- CLAUDE.md
- MeasurementJsonConverterTests
- Building the Project
- BindingErrorListener
- ResourceDictionary
- State
- Development
- Dot
- StringResources.ru.xaml
- Features
- Testing
- SettingsView
- Added
- Localization
- system_text_json
- [1.4.1] - 2026-11-23
- System Requirements
- Running the Application
- CheckGlyph
- ColorAnalyzerModel
- Architecture
- Ca410SdkLoader
- MeasurementSpeed
- Line
- Usage
- Installation
- ColorimeterInfo.cs

## God Nodes (most connected - your core abstractions)
1. `Resources` - 204 edges
2. `MeasurementViewModel` - 167 edges
3. `UserControl` - 141 edges
4. `MeasurementViewModelTests` - 120 edges
5. `SettingsViewModel` - 68 edges
6. `WPF_LCD_Test.Models` - 55 edges
7. `SpecSummaryViewModel` - 51 edges
8. `SettingsViewModelTests` - 50 edges
9. `MeasurementStatus` - 43 edges
10. `FieldStylesTests` - 40 edges

## Surprising Connections (you probably didn't know these)
- `Added` --references--> `LocalizationResourcesTests`  [INFERRED]
  CHANGELOG.md → WPF_LCD_Test.UnitTests/ServicesTests/LocalizationResourcesTests.cs
- `Added` --references--> `UploadService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Services/UploadService.cs
- `Changed` --references--> `ILocalizationService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Interfaces/ILocalizationService.cs
- `Changed` --references--> `MeasurementStatusService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Services/MeasurementStatusService.cs
- `Fixed` --references--> `ActionFeedbackSignal`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Models/ActionFeedbackSignal.cs

## Import Cycles
- None detected.

## Communities (139 total, 41 thin omitted)

### Community 0 - "Resources"
Cohesion: 0.01
Nodes (201): Resources, ActiveChannelStatus, Actual, AllowedRange, AlreadyActivated, ArchiveFolderNotFound, Attempt, AvailableChannels (+193 more)

### Community 1 - "Ca310ProbeWrapper"
Cohesion: 0.12
Nodes (15): IColorAnalyzerProbe, Lv, SerialNO, sx, sy, T, TypeName, Ca310ProbeWrapper (+7 more)

### Community 2 - "Changelog"
Cohesion: 0.14
Nodes (14): [1.0.0] - 2025-04-28, [1.2.0] - 2025-11-19, [1.2.3] - 2026-02-03, [1.2.4] - 2026-05-07, [1.3.0] - 2026-08-11, [1.4.2] - 2026-11-24, Added, Added (+6 more)

### Community 3 - "DebugTrace.Format"
Cohesion: 0.13
Nodes (5): DebugTrace, FilePath, RelayCommandTrace, TracingProxy, ViewModelTracer

### Community 4 - "Window"
Cohesion: 0.12
Nodes (17): IsChecked, IsMeasurementPageVisible, IsSettingsPageVisible, MeasurementPage, NavigateCommand, SettingsPage, Title, MainWindow (+9 more)

### Community 5 - "UserControl"
Cohesion: 0.02
Nodes (117): Children, ClearLogCommand, ConfigFeedback, BooleanToVisibilityConverter, DataContext.ApplySerialNumberCommand, DataContext.IsReportGenerating, DataContext.IsUploadingReports, DataContext.MeasureCommand (+109 more)

### Community 7 - "MeasurementViewModel"
Cohesion: 0.04
Nodes (44): MeasurementViewModel, ApplySerialNumberCommand, ClearFeedback, ClearFieldsCommand, ClearLogCommand, ConfigFeedback, CurrentDevice, DeviceActiveChannelStatusText (+36 more)

### Community 8 - "DeviceUnderTest"
Cohesion: 0.06
Nodes (21): CustomDateTimeConverter, MeasurementJsonConverter, DeviceUnderTest, Colorimeter, DeviceConfiguration, IsTV, MeasurementDateTime, Measurements (+13 more)

### Community 10 - "AnalyzerRegistryService"
Cohesion: 0.09
Nodes (13): IAnalyzerRegistryService, AnalyzerRegistryEntry, LastChannel, LastSeenUtc, Model, ProbeSN, UploadReportItem, LocalFilesToUpload (+5 more)

### Community 11 - "SettingsViewModel"
Cohesion: 0.07
Nodes (23): SettingsViewModel, AvailableLanguages, AvailableMeasurementSpeeds, AvailableSyncModes, AvailableThemes, ColorAnalyzerChannel, IntegrationTime, IntegrationTimeHint (+15 more)

### Community 12 - "release_manager.py"
Cohesion: 0.11
Nodes (8): ensure_remote_dir(), get_nc_client(), get_sha256(), main(), prepare_package(), upload_all(), worker(), upload_chunked()

### Community 13 - "MeasurementStatus"
Cohesion: 0.13
Nodes (12): MeasurementStatus, Feedback, IsPassed, Location, MeasuredValuesString, MeasuredValuesToolTip, SpecBadge, SpecRemark (+4 more)

### Community 14 - "SettingsViewModelTests"
Cohesion: 0.06
Nodes (5): ActionFeedbackSignalTests, SettingsViewModelTests, LanguageOption, CultureCode, DisplayName

### Community 15 - "SpecEvaluation"
Cohesion: 0.07
Nodes (19): IProcessRunner, ProcessResult, SpecAttemptInfo, SpecEvaluation, Attempts, SpecStatus, Fail, Pass (+11 more)

### Community 16 - "FieldStylesTests"
Cohesion: 0.08
Nodes (12): ActionFeedback, Canceled, Failure, None, Success, ActionFeedbackSignal, Feedback, Sequence (+4 more)

### Community 17 - "ColorMeasurementService"
Cohesion: 0.12
Nodes (6): ColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN

### Community 19 - "SyncSetting"
Cohesion: 0.15
Nodes (13): AnalyzerCapabilities, SyncMode, Ext, Internal, Manual, Ntsc, Pal, Universal (+5 more)

### Community 20 - "Screen Checker.UnitTests.csproj"
Cohesion: 0.14
Nodes (14): net8.0-windows, net8.0-windows7.0, coverlet.collector (6.0.0), MaterialDesignThemes (5.2.2-ci976), Microsoft.Extensions.DependencyInjection (10.0.2), Microsoft.NET.Test.Sdk (17.8.0), Moq (4.20.72), NUnit (3.14.0) (+6 more)

### Community 21 - "RelayCommand"
Cohesion: 0.08
Nodes (10): WPF_LCD_Test.Commands, WPF_LCD_Test.UnitTests.Commands, RelayCommand, RelayCommandTests, Calculator, Count, DebugTraceTests, ICalculator (+2 more)

### Community 22 - "WPF_LCD_Test.Interfaces"
Cohesion: 0.26
Nodes (3): WPF_LCD_Test.Services, WPF_LCD_Test.UnitTests.ServicesTests, WPF_LCD_Test.Tests

### Community 23 - "ActionFeedbackBehavior"
Cohesion: 0.14
Nodes (6): ActionFeedbackBehavior, ActionFeedbackMode, Fill, Glow, Overlay, Underline

### Community 24 - "DeviceUnderTest"
Cohesion: 0.32
Nodes (3): ISpecEvaluationService, SpecEvaluationOutcome, SpecCheck

### Community 25 - "Measurement"
Cohesion: 0.17
Nodes (11): Measurement, Attempts, BestAttemptIndex, IsValid, Location, Lv, T, Time (+3 more)

### Community 26 - "WPF_LCD_Test.ViewModels"
Cohesion: 0.13
Nodes (4): WPF_LCD_Test.ViewModels, WPF_LCD_Test.UnitTests.ViewModelsTests, WPF_LCD_Test.UnitTests.Mocks, WPF_LCD_Test.UnitTests.ViewModels

### Community 27 - "Ca410Wrapper"
Cohesion: 0.10
Nodes (18): IColorAnalyzerMemory, ChannelNO, Ca410AnalyzerWrapper, Model, Root, SingleCa, Ca410MemoryWrapper, ChannelNO (+10 more)

### Community 28 - "IDisposable"
Cohesion: 0.14
Nodes (5): IColorAnalyzer200, Model, SingleCa, Ca310AnalyzerWrapper, Model

### Community 29 - "AppSettings"
Cohesion: 0.18
Nodes (9): AppSettings, ColorAnalyzerChannel, IntegrationTime, LanguageCultureCode, MeasurementSpeed, SyncFrequency, SyncMode, Theme (+1 more)

### Community 30 - "LogHandlerTests"
Cohesion: 0.05
Nodes (14): ILocalizationService, CurrentCulture, LogHandler, LocalizationService, CurrentCulture, MockSettingsViewModel, IsDisposed, LocalizationService (+6 more)

### Community 31 - "MeasurementStatusService"
Cohesion: 0.18
Nodes (7): IMeasurementStatusService, AllMeasurementButtonStatuses, Statuses, MeasurementStatusService, AllMeasurementButtonStatuses, Statuses, MeasurementStatusServiceTests

### Community 32 - "SimulatedColorMeasurementService"
Cohesion: 0.17
Nodes (6): SimulatedColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN

### Community 33 - "Ca410ProbeWrapper"
Cohesion: 0.14
Nodes (11): Ca410ProbeWrapper, Lv, Probe, SerialNO, sx, sy, T, TypeName (+3 more)

### Community 34 - "UserControl"
Cohesion: 0.08
Nodes (25): AvailableLanguages, AvailableMeasurementSpeeds, AvailableSyncModes, AvailableThemes, ColorAnalyzerChannel, IntegrationTime, IntegrationTimeHint, IsIntegrationTimeVisible (+17 more)

### Community 35 - "ActionFeedbackBehavior.Play"
Cohesion: 0.23
Nodes (4): ActionFeedbackBreathing, GlowBreathing, UnderlineBreathing, FeedbackPalette

### Community 39 - "Screen Checker"
Cohesion: 0.18
Nodes (11): Application Settings, Architecture, Changelog, Configuration, Device Configurations, License, Pattern: MVVM (Model-View-ViewModel), Planned Features (+3 more)

### Community 40 - "Button"
Cohesion: 0.13
Nodes (14): ClearFeedback, DataContext.ClearFieldsCommand, DataContext.NewDeviceUnderTestCommand, DataContext.ReportGenerateCommand, DataContext.UploadReportsCommand, ReportFeedback, SaveFeedback, UploadFeedback (+6 more)

### Community 41 - "main_tests"
Cohesion: 0.33
Nodes (5): main_tests, White_x, max, min, typ

### Community 44 - "system_windows"
Cohesion: 0.12
Nodes (4): WPF_LCD_Test.Views.Behaviors, WPF_LCD_Test.UnitTests.ViewsTests, WPF_LCD_Test.Views, IMessageBox

### Community 45 - "ReportExitCode"
Cohesion: 0.40
Nodes (5): ReportExitCode, ConfigError, GeneralError, NoDataFound, Success

### Community 47 - "SimulatedColorMeasurementServiceTests"
Cohesion: 0.14
Nodes (8): SimulationOptions, SimulationScenario, Fail, Ok, Reject, Retry, Uniformity, SimulatedColorMeasurementServiceTests

### Community 48 - "Ca310MemoryWrapper"
Cohesion: 0.25
Nodes (7): Dependency Injection, Key Components, Services, Wrappers, Ca310MemoryWrapper, ChannelNO, Memory

### Community 50 - "DeviceCard"
Cohesion: 0.33
Nodes (6): DeviceStateText, SerialFeedback, Bg, DeviceCard, SerialNumberFeedbackLine, Border

### Community 51 - "Releasing"
Cohesion: 0.50
Nodes (4): 1. Configure credentials, 2. Publish the application, 3. Run the release manager, Releasing

### Community 53 - "Blue_x"
Cohesion: 0.50
Nodes (4): max, min, typ, Blue_x

### Community 54 - "Blue_y"
Cohesion: 0.50
Nodes (4): max, min, typ, Blue_y

### Community 55 - "Brightness"
Cohesion: 0.50
Nodes (4): max, min, typ, Brightness

### Community 56 - "Brightness_uniformity"
Cohesion: 0.50
Nodes (4): max, min, typ, Brightness_uniformity

### Community 57 - "Cg_ntsc_area"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_ntsc_area

### Community 58 - "Cg_ntsc"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_ntsc

### Community 59 - "Cg_rgb_area"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_rgb_area

### Community 60 - "Cg_rgb"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_rgb

### Community 61 - "Contrast"
Cohesion: 0.50
Nodes (4): max, min, typ, Contrast

### Community 62 - "Delta_e"
Cohesion: 0.50
Nodes (4): max, min, typ, Delta_e

### Community 63 - "Green_x"
Cohesion: 0.50
Nodes (4): max, min, typ, Green_x

### Community 64 - "Green_y"
Cohesion: 0.50
Nodes (4): max, min, typ, Green_y

### Community 65 - "Red_x"
Cohesion: 0.50
Nodes (4): Red_x, max, min, typ

### Community 66 - "Red_y"
Cohesion: 0.50
Nodes (4): Red_y, max, min, typ

### Community 67 - "Temperature"
Cohesion: 0.50
Nodes (4): Temperature, max, min, typ

### Community 68 - "White_y"
Cohesion: 0.50
Nodes (4): White_y, max, min, typ

### Community 71 - "SpecSummaryViewModel"
Cohesion: 0.08
Nodes (10): SpecSummaryViewModelTests, SpecRowViewModel, SpecProblem, SpecSummaryViewModel, Evaluation, HintText, ProgressText, Rows (+2 more)

### Community 72 - "PathProvider"
Cohesion: 0.17
Nodes (11): [1.2.1] - 2026-01-26, Added, Changed, IPathProvider, BaseDirectory, ConfigDirectory, DataDirectory, PathProvider (+3 more)

### Community 73 - "MeasurementLocation"
Cohesion: 0.12
Nodes (15): MeasurementLocation, BlackColor, BlueColor, BottomCenter, BottomLeft, BottomRight, Center, GreenColor (+7 more)

### Community 76 - "Mock"
Cohesion: 0.24
Nodes (3): DialogService, DialogServiceTests, MessageBoxWrapper

### Community 77 - "TextStyleRulesTests"
Cohesion: 0.06
Nodes (17): LocalizationResourcesTests, TextStyleRulesTests, MeasurementResultRow, IsAvailable, Location, Lv, Name, Status (+9 more)

### Community 83 - "IColorMeasurementService"
Cohesion: 0.15
Nodes (6): IColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN

### Community 84 - "MeasurementView"
Cohesion: 0.22
Nodes (4): SerialNumber, MeasurementView, SerialNumberTextBox, TextBox

### Community 88 - "MockMeasurementViewModel"
Cohesion: 0.12
Nodes (9): IFileService, BaseFolderPath, WorkFolderName, MockMeasurementViewModel, ColorMeasurementService, DialogService, FileService, IsDisposed (+1 more)

### Community 91 - "AppConstants"
Cohesion: 0.29
Nodes (6): [1.1.0] - 2025-10-15, Added, Changed, WPF_LCD_Test, AppConstants, ColorAnalyzer

### Community 93 - "Window"
Cohesion: 0.27
Nodes (7): ChoiceDialogWindow, FirstButton, MessageText, SecondButton, Window, Button, TextBlock

### Community 94 - "Ca310Wrapper"
Cohesion: 0.09
Nodes (16): IColorAnalyzer, AveragingMode, DisplayMode, ID, Memory, Number, PortID, SingleProbe (+8 more)

### Community 95 - "CustomDateTimeConverterTests"
Cohesion: 0.33
Nodes (3): CustomDateTimeConverterTests, TestObjectWithDateTime, TestDateTime

### Community 96 - "WPF_LCD_Test.Models"
Cohesion: 0.13
Nodes (3): WPF_LCD_Test.Interfaces, WPF_LCD_Test.Models, WPF_LCD_Test.Wrappers

### Community 98 - "[1.4.0] - 2026-11-17"
Cohesion: 0.40
Nodes (5): [1.4.0] - 2026-11-17, Added, Changed, Fixed, Removed

### Community 99 - "ISettingsService"
Cohesion: 0.15
Nodes (7): SettingsService, CurrentSettings, SettingsServiceTests, TestPathProvider, BaseDirectory, ConfigDirectory, DataDirectory

### Community 100 - "Window"
Cohesion: 0.21
Nodes (10): MessageDialogWindow, MessageText, PrimaryButton, QuestionSeparator, QuestionText, SecondaryButton, Window, Button (+2 more)

### Community 101 - "BaseViewModel"
Cohesion: 0.11
Nodes (10): DeviceConfigTreeNode, Children, DisplayName, IsFolder, IsSelected, RelativePath, SelectCommand, ThemeOption (+2 more)

### Community 102 - "MainWindowViewModel"
Cohesion: 0.16
Nodes (8): MainWindowViewModel, CurrentPageIdentifier, CurrentPageViewModel, IsMeasurementPageVisible, IsSettingsPageVisible, MeasurementPage, NavigateCommand, SettingsPage

### Community 104 - "ISettingsService"
Cohesion: 0.20
Nodes (3): ISettingsService, CurrentSettings, SelectedLanguage

### Community 105 - "MeasurementViewModel.UpdateUIState"
Cohesion: 0.15
Nodes (5): IsDeviceCalibrated, IsDeviceConnected, IsSerialNumberConfirmed, IsTvCheckboxChecked, SerialNumber

### Community 107 - "CLAUDE.md"
Cohesion: 0.17
Nodes (8): Build & Test Commands, Communication Style & Code Standards, Configuration, Localization, Project Map (graphify), Project Overview, Testing, Version

### Community 109 - "MeasurementJsonConverterTests"
Cohesion: 0.27
Nodes (3): MeasurementJsonConverterTests, TestObjectWithMeasurement, TestMeasurement

### Community 110 - "Building the Project"
Cohesion: 0.50
Nodes (4): Build Output, Building the Project, Using MSBuild (Recommended), Using Visual Studio

### Community 112 - "ResourceDictionary"
Cohesion: 0.13
Nodes (15): ActualWidth, ContextMenu.IsOpen, IsDropDownOpen, IsSubmenuOpen, FocusAccent, HoverAccent, Line, PART_ContentHost (+7 more)

### Community 113 - "State"
Cohesion: 0.50
Nodes (4): State, Calibrating, NotConnected, Ready

### Community 114 - "Development"
Cohesion: 0.50
Nodes (4): Adding New Device Configurations, Coding Conventions, Development, Project Structure

### Community 117 - "Features"
Cohesion: 0.40
Nodes (5): Data Management, Device Support, Features, Measurement Capabilities, User Interface

### Community 118 - "Testing"
Cohesion: 0.40
Nodes (5): Running All Tests, Running Single Test Method, Running Specific Test Class, Test Coverage, Testing

### Community 121 - "Added"
Cohesion: 0.67
Nodes (3): [1.2.2] - 2026-01-28, Added, Fixed

### Community 122 - "Localization"
Cohesion: 0.50
Nodes (4): Adding New Languages, Language Switching, Localization, Supported Languages

### Community 123 - "system_text_json"
Cohesion: 0.11
Nodes (3): WPF_LCD_Test.Converters, WPF_LCD_Test.UnitTests.ConvertersTests, WPF_LCD_Test.UnitTests.ModelsTests

### Community 124 - "[1.4.1] - 2026-11-23"
Cohesion: 0.67
Nodes (3): [1.4.1] - 2026-11-23, Added, Changed

### Community 125 - "System Requirements"
Cohesion: 0.67
Nodes (3): Development Requirements, Runtime Requirements, System Requirements

### Community 127 - "Running the Application"
Cohesion: 0.67
Nodes (3): From Build Output, From Visual Studio, Running the Application

### Community 128 - "CheckGlyph"
Cohesion: 0.67
Nodes (3): CheckGlyph, Glyph, TextBlock

### Community 129 - "ColorAnalyzerModel"
Cohesion: 0.33
Nodes (5): ColorAnalyzerModel, CA310, CA410, Unknown, ColorAnalyzerModelExtensions

### Community 132 - "Architecture"
Cohesion: 0.10
Nodes (20): Architecture, IThemeService, EffectiveTheme, Mode, ISystemThemeProvider, IThemeApplier, AppTheme, Dark (+12 more)

### Community 137 - "MeasurementSpeed"
Cohesion: 0.40
Nodes (4): MeasurementSpeed, Auto, Fast, Slow

### Community 141 - "Usage"
Cohesion: 0.50
Nodes (4): Configuration Files, Initial Setup, Performing Measurements, Usage

### Community 143 - "Installation"
Cohesion: 0.67
Nodes (3): For Developers, For End Users, Installation

## Knowledge Gaps
- **668 isolated node(s):** `ResourceManager`, `Culture`, `ActiveChannelStatus`, `Actual`, `AllowedRange` (+663 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 961 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **41 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MeasurementViewModel` connect `MeasurementViewModel` to `DebugTrace.Format`, `App.ConfigureServices`, `DeviceUnderTest`, `Task`, `AnalyzerRegistryService`, `SettingsViewModelTests`, `SpecEvaluation`, `MeasurementViewModel.BuildDeviceConfigurationTree`, `FieldStylesTests`, `MainWindowViewModelTests`, `ActionFeedbackBehavior`, `DeviceUnderTest`, `WPF_LCD_Test.ViewModels`, `IDisposable`, `LogHandlerTests`, `MeasurementStatusService`, `Task`, `DeviceCardTransition`, `MeasurementViewModel.ExecuteThreadInUI`, `ReportExitCode`, `SpecSummaryViewModel`, `PathProvider`, `TextStyleRulesTests`, `IColorMeasurementService`, `IUploadService`, `AppConstants`, `MeasurementViewModel.Log`, `App`, `BaseViewModel`, `MainWindowViewModel`, `UploadService`, `MeasurementViewModel.UpdateUIState`, `SetUp`?**
  _High betweenness centrality (0.139) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `MeasurementViewModel` (e.g. with `Added` and `Changed`) actually correct?**
  _`MeasurementViewModel` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `ResourceManager`, `Culture`, `ActiveChannelStatus` to the rest of the system?**
  _668 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Resources` be split into smaller, more focused modules?**
  _Cohesion score 0.009852216748768473 - nodes in this community are weakly interconnected._
- **Why does `Resources` connect `Resources` to `Resources.Designer.cs`, `LogHandlerTests`?**
  _High betweenness centrality (0.136) - this node is a cross-community bridge._
- **Should `Ca310ProbeWrapper` be split into smaller, more focused modules?**
  _Cohesion score 0.125 - nodes in this community are weakly interconnected._
- **Why does `SpecSummaryViewModelTests` connect `SpecSummaryViewModel` to `App.ConfigureServices`, `AnalyzerRegistryService`, `MeasurementViewModel.ExecuteThreadInUI`, `WPF_LCD_Test.ViewModels`, `LogHandlerTests`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
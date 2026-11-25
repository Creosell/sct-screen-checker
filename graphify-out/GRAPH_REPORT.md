# Graph Report - sct-screen-checker  (2026-11-25)

## Corpus Check
- 138 files · ~79,792 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: .resx 3, (none) 2, .xml 1)

## Summary
- 2508 nodes · 5709 edges · 202 communities (92 shown, 110 thin omitted)
- Extraction: 85% EXTRACTED · 15% INFERRED · 0% AMBIGUOUS · INFERRED: 868 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `46c12d0b`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Resources
- DeviceUnderTest
- Changelog
- App.Application_Startup
- Window
- UserControl
- App.ConfigureServices
- MeasurementViewModel
- AnalyzerRegistryService
- Measurement
- ThemeMode
- SettingsViewModel
- MeasurementStatus
- Test
- SpecSummaryViewModel
- FieldStylesTests
- ColorMeasurementService
- Screen Checker.UnitTests.csproj
- RelayCommand
- ScreenChecker.Interfaces
- ActionFeedbackBehavior
- Ca310ProbeWrapper
- ScreenChecker.ViewModels
- Ca410Wrapper
- Ca310Wrapper
- ILocalizationService
- ScreenChecker.Models
- Task
- Ca410ProbeWrapper
- UserControl
- ActionFeedback
- TestCase
- DeviceCardTransition
- ColorAnalyzerModel
- Screen Checker
- Button
- SettingsViewModelTests
- DialogService
- system_windows
- AppSettings
- UiEventTracer.Install
- MeasurementViewModel.UpdateUIState
- SimulatedColorMeasurementService
- MeasurementViewModel.ExecuteThreadInUI
- DeviceCard
- MeasurementView
- MainWindowViewModelTests
- CancellationToken
- IDialogService
- ActionFeedbackSignal
- FieldStylesTests.NavigationTab_GrowsTheLineOnHoverExceptWhenSelected
- BaseViewModel
- main_tests
- ISettingsService
- LiveOption
- MeasurementSpeed
- ReportExitCode
- Blue_x
- Blue_y
- Brightness
- Resources.Designer.cs
- Brightness_uniformity
- Architecture
- Cg_ntsc_area
- MainWindow
- Cg_ntsc
- TextStyleRulesTests
- Cg_rgb_area
- Cg_rgb
- IReadOnlyList
- SubmenuArrow
- Contrast
- Delta_e
- Green_x
- BreathingStoryboard
- DeviceCardContent
- Header
- Green_y
- Red_x
- Action
- Red_y
- Temperature
- White_y
- MeasurementViewModel.cs
- ColorAnalyzerChannelTextBox
- [1.4.0] - 2026-11-17
- SettingsService
- Window
- DeviceConfigTreeNode
- LogHandlerTests
- PART_Popup
- [1.2.0] - 2025-11-19
- CLAUDE.md
- [1.2.2] - 2026-01-28
- MeasurementJsonConverterTests
- Building the Project
- BindingErrorListener
- ResourceDictionary
- IColorMeasurementService
- Development
- Dot
- [1.4.1] - 2026-11-23
- [1.4.2] - 2026-11-24
- Testing
- UploadService.cs
- CheckGlyph
- Localization
- StringResources.xaml
- System Requirements
- Running the Application
- StringResources.ru.xaml
- StringResources.zh-Hans.xaml
- MessageDialogWindow
- Theme.Dark.xaml
- Ca410SdkLoader
- MainWindowViewModel
- Theme.Light.xaml
- LogScrollViewer
- Line
- Usage
- ChoiceDialogWindow
- SettingsView
- Installation

## God Nodes (most connected - your core abstractions)
1. `Resources` - 204 edges
2. `MeasurementViewModel` - 168 edges
3. `UserControl` - 142 edges
4. `MeasurementViewModelTests` - 121 edges
5. `Measurement` - 80 edges
6. `ScreenChecker.Models` - 77 edges
7. `SettingsViewModel` - 73 edges
8. `DeviceUnderTest` - 58 edges
9. `ScreenChecker.Interfaces` - 55 edges
10. `SpecSummaryViewModel` - 53 edges

## Surprising Connections (you probably didn't know these)
- `Architecture` --references--> `RelayCommand`  [INFERRED]
  CLAUDE.md → Screen Checker/Commands/RelayCommand.cs
- `Architecture` --references--> `RelayCommandTrace`  [INFERRED]
  CLAUDE.md → Screen Checker/DebugTools/RelayCommandTrace.cs
- `Architecture` --references--> `TracingProxy`  [INFERRED]
  CLAUDE.md → Screen Checker/DebugTools/TracingProxy.cs
- `Architecture` --references--> `UiEventTracer`  [INFERRED]
  CLAUDE.md → Screen Checker/DebugTools/UiEventTracer.cs
- `Architecture` --references--> `IPathProvider`  [INFERRED]
  CLAUDE.md → Screen Checker/Interfaces/IPathProvider.cs

## Import Cycles
- None detected.

## Communities (202 total, 110 thin omitted)

### Community 0 - "Resources"
Cohesion: 0.01
Nodes (201): Resources, ActiveChannelStatus, Actual, AllowedRange, AlreadyActivated, ArchiveFolderNotFound, Attempt, AvailableChannels (+193 more)

### Community 1 - "DeviceUnderTest"
Cohesion: 0.08
Nodes (17): IFileService, BaseFolderPath, WorkFolderName, ColorimeterInfo, DeviceUnderTest, Colorimeter, DeviceConfiguration, IsTV (+9 more)

### Community 2 - "Changelog"
Cohesion: 0.11
Nodes (18): [1.0.0] - 2025-04-28, [1.1.0] - 2025-10-15, [1.2.1] - 2026-01-26, [1.2.3] - 2026-02-03, [1.2.4] - 2026-05-07, [1.3.0] - 2026-08-11, [1.4.3] - 2026-11-25, Added (+10 more)

### Community 3 - "App.Application_Startup"
Cohesion: 0.14
Nodes (4): DebugTrace, FilePath, RelayCommandTrace, TracingProxy

### Community 4 - "Window"
Cohesion: 0.16
Nodes (13): IsChecked, IsMeasurementPageVisible, IsSettingsPageVisible, MeasurementPage, NavigateCommand, SettingsPage, Title, CloseImage (+5 more)

### Community 5 - "UserControl"
Cohesion: 0.02
Nodes (117): Children, ClearLogCommand, ConfigFeedback, DataContext.ApplySerialNumberCommand, DataContext.IsReportGenerating, DataContext.IsUploadingReports, DataContext.MeasureCommand, DataContext.MeasurementStatusService.Statuses[BlackColor] (+109 more)

### Community 6 - "App.ConfigureServices"
Cohesion: 0.13
Nodes (12): IPathProvider, BaseDirectory, ConfigDirectory, DataDirectory, IProcessRunner, ISpecEvaluationService, PathProvider, BaseDirectory (+4 more)

### Community 7 - "MeasurementViewModel"
Cohesion: 0.04
Nodes (41): MeasurementViewModel, ApplySerialNumberCommand, ClearFeedback, ClearFieldsCommand, ClearLogCommand, ConfigFeedback, CurrentDevice, DeviceActiveChannelStatusText (+33 more)

### Community 8 - "AnalyzerRegistryService"
Cohesion: 0.06
Nodes (18): CustomDateTimeConverter, MeasurementJsonConverter, IAnalyzerRegistryService, AnalyzerRegistryEntry, LastChannel, LastSeenUtc, Model, ProbeSN (+10 more)

### Community 9 - "Measurement"
Cohesion: 0.10
Nodes (11): Measurement, Attempts, BestAttemptIndex, IsValid, Location, Lv, T, Time (+3 more)

### Community 10 - "ThemeMode"
Cohesion: 0.09
Nodes (19): IThemeService, EffectiveTheme, Mode, ISystemThemeProvider, IThemeApplier, AppTheme, Dark, Light (+11 more)

### Community 11 - "SettingsViewModel"
Cohesion: 0.07
Nodes (23): SettingsViewModel, AvailableLanguages, AvailableMeasurementSpeeds, AvailableSyncModes, AvailableThemes, ColorAnalyzerChannel, IntegrationTime, IntegrationTimeHint (+15 more)

### Community 13 - "MeasurementStatus"
Cohesion: 0.05
Nodes (34): IMeasurementStatusService, AllMeasurementButtonStatuses, Statuses, MeasurementLocation, BlackColor, BlueColor, BottomCenter, BottomLeft (+26 more)

### Community 15 - "SpecSummaryViewModel"
Cohesion: 0.05
Nodes (25): SpecAttemptInfo, SpecCheck, SpecEvaluation, Attempts, SpecStatus, Fail, Pass, Pending (+17 more)

### Community 17 - "ColorMeasurementService"
Cohesion: 0.09
Nodes (8): ColorMeasurementService, ChannelName, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN, ColorMeasurementServiceTests

### Community 20 - "Screen Checker.UnitTests.csproj"
Cohesion: 0.11
Nodes (17): net8.0-windows, net8.0-windows7.0, coverlet.collector (6.0.0), MaterialDesignThemes (5.2.2-ci976), Microsoft.NET.Test.Sdk (17.8.0), Moq (4.20.72), NUnit (3.14.0), NUnit3TestAdapter (4.5.0) (+9 more)

### Community 21 - "RelayCommand"
Cohesion: 0.08
Nodes (9): ScreenChecker.UnitTests.Commands, RelayCommand, RelayCommandTests, Calculator, Count, DebugTraceTests, ICalculator, Count (+1 more)

### Community 22 - "ScreenChecker.Interfaces"
Cohesion: 0.13
Nodes (5): ScreenChecker.Services, ScreenChecker.UnitTests.ServicesTests, ScreenChecker.Interfaces, ScreenChecker.Tests, ExternalApps

### Community 23 - "ActionFeedbackBehavior"
Cohesion: 0.15
Nodes (6): ActionFeedbackBehavior, ActionFeedbackMode, Fill, Glow, Overlay, Underline

### Community 25 - "Ca310ProbeWrapper"
Cohesion: 0.09
Nodes (21): Architecture, Dependency Injection, Key Components, Pattern: MVVM (Model-View-ViewModel), Services, Wrappers, IColorAnalyzerProbe, Lv (+13 more)

### Community 26 - "ScreenChecker.ViewModels"
Cohesion: 0.15
Nodes (4): ScreenChecker.ViewModels, ScreenChecker.UnitTests.ViewModels, ScreenChecker.UnitTests.ViewModelsTests, ScreenChecker.UnitTests.Mocks

### Community 27 - "Ca410Wrapper"
Cohesion: 0.10
Nodes (16): SingleCa, Ca410MemoryWrapper, ChannelID, ChannelNO, Memory, Ca410Sdk, Ca410Wrapper, AveragingMode (+8 more)

### Community 28 - "Ca310Wrapper"
Cohesion: 0.06
Nodes (25): IColorAnalyzer, AveragingMode, DisplayMode, ID, Memory, Number, PortID, SingleProbe (+17 more)

### Community 30 - "ILocalizationService"
Cohesion: 0.07
Nodes (18): ILocalizationService, CurrentCulture, LocalizationService, CurrentCulture, MockMeasurementViewModel, ColorMeasurementService, DialogService, FileService (+10 more)

### Community 31 - "ScreenChecker.Models"
Cohesion: 0.14
Nodes (4): ScreenChecker.Converters, ScreenChecker.UnitTests.ModelsTests, ScreenChecker.Models, ScreenChecker.UnitTests.ConvertersTests

### Community 33 - "Ca410ProbeWrapper"
Cohesion: 0.16
Nodes (10): Ca410ProbeWrapper, Lv, Probe, SerialNO, sx, sy, T, TypeName (+2 more)

### Community 34 - "UserControl"
Cohesion: 0.09
Nodes (21): AvailableLanguages, AvailableMeasurementSpeeds, AvailableSyncModes, AvailableThemes, IntegrationTime, IntegrationTimeHint, IsIntegrationTimeVisible, IsSyncFrequencyVisible (+13 more)

### Community 35 - "ActionFeedback"
Cohesion: 0.16
Nodes (11): ActionFeedback, Canceled, Failure, None, Success, GlowBreathing, UnderlineBreathing, FeedbackPalette (+3 more)

### Community 36 - "TestCase"
Cohesion: 0.11
Nodes (12): AnalyzerCapabilities, SyncMode, Ext, Internal, Manual, Ntsc, Pal, Universal (+4 more)

### Community 37 - "DeviceCardTransition"
Cohesion: 0.20
Nodes (5): DeviceCardTransition, State, Calibrating, NotConnected, Ready

### Community 38 - "ColorAnalyzerModel"
Cohesion: 0.11
Nodes (11): AnalyzerException, Code, Operation, SdkMessage, ColorAnalyzerModel, CA310, CA410, Unknown (+3 more)

### Community 39 - "Screen Checker"
Cohesion: 0.13
Nodes (15): Application Settings, Changelog, Configuration, Data Management, Device Configurations, Device Support, Features, License (+7 more)

### Community 40 - "Button"
Cohesion: 0.14
Nodes (14): ClearFeedback, DataContext.ClearFieldsCommand, DataContext.NewDeviceUnderTestCommand, DataContext.ReportGenerateCommand, DataContext.UploadReportsCommand, ReportFeedback, UploadFeedback, ClearResultsButton (+6 more)

### Community 41 - "SettingsViewModelTests"
Cohesion: 0.08
Nodes (4): SettingsViewModelTests, LanguageOption, CultureCode, DisplayName

### Community 43 - "DialogService"
Cohesion: 0.23
Nodes (4): IMessageBox, DialogService, DialogServiceTests, MessageBoxWrapper

### Community 44 - "system_windows"
Cohesion: 0.14
Nodes (3): ScreenChecker.Views, ScreenChecker.Wrappers, ScreenChecker.Views.Behaviors

### Community 45 - "AppSettings"
Cohesion: 0.11
Nodes (9): AppSettings, ColorAnalyzerChannel, IntegrationTime, LanguageCultureCode, MeasurementSpeed, SyncFrequency, SyncMode, Theme (+1 more)

### Community 47 - "MeasurementViewModel.UpdateUIState"
Cohesion: 0.12
Nodes (8): IsDeviceCalibrated, IsDeviceConnected, IsReportGenerating, IsSerialNumberConfirmed, IsTvCheckboxChecked, IsUploadingReports, SelectedDeviceConfiguration, SerialNumber

### Community 48 - "SimulatedColorMeasurementService"
Cohesion: 0.07
Nodes (15): SimulatedColorMeasurementService, ChannelName, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN, SimulationOptions (+7 more)

### Community 49 - "MeasurementViewModel.ExecuteThreadInUI"
Cohesion: 0.19
Nodes (6): LogEntry, TimeText, LogSeverity, Error, Info, Warning

### Community 50 - "DeviceCard"
Cohesion: 0.33
Nodes (6): DeviceStateText, SerialFeedback, Bg, DeviceCard, SerialNumberFeedbackLine, Border

### Community 52 - "MeasurementView"
Cohesion: 0.21
Nodes (4): SerialNumber, SerialNumberTextBox, MeasurementView, TextBox

### Community 56 - "ActionFeedbackSignal"
Cohesion: 0.32
Nodes (4): ActionFeedbackSignal, Feedback, Sequence, ActionFeedbackSignalTests

### Community 58 - "BaseViewModel"
Cohesion: 0.29
Nodes (3): ThemeOption, DisplayName, Mode

### Community 60 - "main_tests"
Cohesion: 0.33
Nodes (5): main_tests, White_x, max, min, typ

### Community 61 - "ISettingsService"
Cohesion: 0.33
Nodes (3): ISettingsService, CurrentSettings, SelectedLanguage

### Community 63 - "MeasurementSpeed"
Cohesion: 0.40
Nodes (4): MeasurementSpeed, Auto, Fast, Slow

### Community 64 - "ReportExitCode"
Cohesion: 0.40
Nodes (5): ReportExitCode, ConfigError, GeneralError, NoDataFound, Success

### Community 67 - "Blue_x"
Cohesion: 0.50
Nodes (4): max, min, typ, Blue_x

### Community 68 - "Blue_y"
Cohesion: 0.50
Nodes (4): max, min, typ, Blue_y

### Community 69 - "Brightness"
Cohesion: 0.50
Nodes (4): max, min, typ, Brightness

### Community 72 - "Brightness_uniformity"
Cohesion: 0.50
Nodes (4): max, min, typ, Brightness_uniformity

### Community 73 - "Architecture"
Cohesion: 0.15
Nodes (7): Architecture, ScreenChecker, Application, App, AppConstants, ColorAnalyzer, ViewModelTracer

### Community 74 - "Cg_ntsc_area"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_ntsc_area

### Community 76 - "Cg_ntsc"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_ntsc

### Community 77 - "TextStyleRulesTests"
Cohesion: 0.09
Nodes (3): ThemeDictionariesTests, LocalizationResourcesTests, TextStyleRulesTests

### Community 78 - "Cg_rgb_area"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_rgb_area

### Community 79 - "Cg_rgb"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_rgb

### Community 82 - "Contrast"
Cohesion: 0.50
Nodes (4): max, min, typ, Contrast

### Community 83 - "Delta_e"
Cohesion: 0.50
Nodes (4): max, min, typ, Delta_e

### Community 84 - "Green_x"
Cohesion: 0.50
Nodes (4): max, min, typ, Green_x

### Community 88 - "Green_y"
Cohesion: 0.50
Nodes (4): max, min, typ, Green_y

### Community 89 - "Red_x"
Cohesion: 0.50
Nodes (4): Red_x, max, min, typ

### Community 91 - "Red_y"
Cohesion: 0.50
Nodes (4): Red_y, max, min, typ

### Community 93 - "Temperature"
Cohesion: 0.50
Nodes (4): Temperature, max, min, typ

### Community 95 - "White_y"
Cohesion: 0.50
Nodes (4): White_y, max, min, typ

### Community 97 - "ColorAnalyzerChannelTextBox"
Cohesion: 0.67
Nodes (3): ColorAnalyzerChannel, ColorAnalyzerChannelTextBox, TextBox

### Community 98 - "[1.4.0] - 2026-11-17"
Cohesion: 0.40
Nodes (5): [1.4.0] - 2026-11-17, Added, Changed, Fixed, Removed

### Community 99 - "SettingsService"
Cohesion: 0.16
Nodes (7): SettingsService, CurrentSettings, SettingsServiceTests, TestPathProvider, BaseDirectory, ConfigDirectory, DataDirectory

### Community 100 - "Window"
Cohesion: 0.24
Nodes (9): MessageText, PrimaryButton, QuestionSeparator, QuestionText, SecondaryButton, Window, Button, TextBlock (+1 more)

### Community 101 - "DeviceConfigTreeNode"
Cohesion: 0.20
Nodes (7): DeviceConfigTreeNode, Children, DisplayName, IsFolder, IsSelected, RelativePath, SelectCommand

### Community 103 - "LogHandlerTests"
Cohesion: 0.06
Nodes (19): IUploadService, LogHandler, UploadService, LogHandlerTests, MeasurementResultRow, IsAvailable, Location, Lv (+11 more)

### Community 104 - "PART_Popup"
Cohesion: 0.67
Nodes (3): IsSubmenuOpen, PART_Popup, Popup

### Community 105 - "[1.2.0] - 2025-11-19"
Cohesion: 0.67
Nodes (3): [1.2.0] - 2025-11-19, Added, Changed

### Community 107 - "CLAUDE.md"
Cohesion: 0.15
Nodes (9): Build & Test Commands, Communication Style & Code Standards, Configuration, Localization, Project Map (graphify), Project Overview, Release, Testing (+1 more)

### Community 108 - "[1.2.2] - 2026-01-28"
Cohesion: 0.67
Nodes (3): [1.2.2] - 2026-01-28, Added, Fixed

### Community 109 - "MeasurementJsonConverterTests"
Cohesion: 0.27
Nodes (3): MeasurementJsonConverterTests, TestObjectWithMeasurement, TestMeasurement

### Community 110 - "Building the Project"
Cohesion: 0.50
Nodes (4): Build Output, Building the Project, Using MSBuild (Recommended), Using Visual Studio

### Community 112 - "ResourceDictionary"
Cohesion: 0.19
Nodes (13): ActualWidth, ContextMenu.IsOpen, IsDropDownOpen, FocusAccent, HoverAccent, Line, PART_ContentHost, PART_Popup (+5 more)

### Community 113 - "IColorMeasurementService"
Cohesion: 0.10
Nodes (13): IColorAnalyzer200, Model, SingleCa, IColorMeasurementService, ChannelName, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected (+5 more)

### Community 114 - "Development"
Cohesion: 0.50
Nodes (4): Adding New Device Configurations, Coding Conventions, Development, Project Structure

### Community 116 - "[1.4.1] - 2026-11-23"
Cohesion: 0.67
Nodes (3): [1.4.1] - 2026-11-23, Added, Changed

### Community 117 - "[1.4.2] - 2026-11-24"
Cohesion: 0.67
Nodes (3): [1.4.2] - 2026-11-24, Changed, Fixed

### Community 118 - "Testing"
Cohesion: 0.40
Nodes (5): Running All Tests, Running Single Test Method, Running Specific Test Class, Test Coverage, Testing

### Community 120 - "CheckGlyph"
Cohesion: 0.67
Nodes (3): CheckGlyph, Glyph, TextBlock

### Community 122 - "Localization"
Cohesion: 0.50
Nodes (4): Adding New Languages, Language Switching, Localization, Supported Languages

### Community 125 - "System Requirements"
Cohesion: 0.67
Nodes (3): Development Requirements, Runtime Requirements, System Requirements

### Community 127 - "Running the Application"
Cohesion: 0.67
Nodes (3): From Build Output, From Visual Studio, Running the Application

### Community 134 - "MainWindowViewModel"
Cohesion: 0.14
Nodes (8): MainWindowViewModel, CurrentPageIdentifier, CurrentPageViewModel, IsMeasurementPageVisible, IsSettingsPageVisible, MeasurementPage, NavigateCommand, SettingsPage

### Community 141 - "Usage"
Cohesion: 0.50
Nodes (4): Configuration Files, Initial Setup, Performing Measurements, Usage

### Community 143 - "ChoiceDialogWindow"
Cohesion: 0.17
Nodes (8): FirstButton, MessageText, SecondButton, Window, ChoiceDialogWindow, SelectedOption, Button, TextBlock

### Community 152 - "Installation"
Cohesion: 0.67
Nodes (3): For Developers, For End Users, Installation

## Knowledge Gaps
- **683 isolated node(s):** `TestDateTime`, `TestMeasurement`, `IsDisposed`, `ColorMeasurementService`, `FileService` (+678 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1004 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **110 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MeasurementViewModel` connect `MeasurementViewModel` to `DeviceUnderTest`, `UserControl`, `MainWindowViewModel`, `App.ConfigureServices`, `AnalyzerRegistryService`, `Measurement`, `MeasurementStatus`, `Test`, `SpecSummaryViewModel`, `ILocalizationService`, `Task`, `DeviceCardTransition`, `MeasurementViewModel.Log`, `MeasurementViewModel.UpdateUIState`, `MeasurementViewModel.ExecuteThreadInUI`, `MainWindowViewModelTests`, `IDialogService`, `ActionFeedbackSignal`, `BaseViewModel`, `ISettingsService`, `ReportExitCode`, `MeasurementViewModel.ConfirmClose`, `TextStyleRulesTests`, `IReadOnlyList`, `Action`, `MeasurementViewModel.cs`, `DeviceConfigTreeNode`, `LogHandlerTests`, `IColorMeasurementService`?**
  _High betweenness centrality (0.187) - this node is a cross-community bridge._
- **What connects `TestDateTime`, `TestMeasurement`, `IsDisposed` to the rest of the system?**
  _683 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Resources` be split into smaller, more focused modules?**
  _Cohesion score 0.009852216748768473 - nodes in this community are weakly interconnected._
- **Why does `Resources` connect `Resources` to `Resources.Designer.cs`, `ILocalizationService`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **Should `DeviceUnderTest` be split into smaller, more focused modules?**
  _Cohesion score 0.07927565392354124 - nodes in this community are weakly interconnected._
- **Why does `UserControl` connect `UserControl` to `MeasurementViewModel`, `Button`, `LogScrollViewer`, `PART_Popup`, `Line`, `MainWindow`, `SubmenuArrow`, `DeviceCard`, `Dot`, `MeasurementView`, `BreathingStoryboard`, `DeviceCardContent`, `Header`, `CheckGlyph`?**
  _High betweenness centrality (0.091) - this node is a cross-community bridge._
- **Should `Changelog` be split into smaller, more focused modules?**
  _Cohesion score 0.1111111111111111 - nodes in this community are weakly interconnected._
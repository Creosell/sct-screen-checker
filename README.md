# Screen Checker

Desktop application for automated testing of LCD display color characteristics using Konica Minolta CA-200/CA-310 colorimeters. The application provides comprehensive measurement capabilities for brightness, contrast, color gamut, RGB primaries, and white point coordinates with automatic validation against device specifications.

## Table of Contents

- [Features](#features)
- [System Requirements](#system-requirements)
- [Installation](#installation)
- [Building the Project](#building-the-project)
- [Running the Application](#running-the-application)
- [Usage](#usage)
- [Configuration](#configuration)
- [Architecture](#architecture)
- [Development](#development)
- [Testing](#testing)
- [Localization](#localization)
- [Releasing](#releasing)
- [Roadmap](#roadmap)
- [Changelog](#changelog)
- [License](#license)

## Features

### Measurement Capabilities
- **Color Coordinates**: Measure CIE 1931 x, y chromaticity coordinates
- **Brightness**: Luminance measurement in cd/m²
- **Contrast Ratio**: Dynamic contrast measurements
- **Color Temperature**: Correlated color temperature (CCT) in Kelvin
- **Color Gamut**: RGB and NTSC color space coverage calculations
- **RGB Primaries**: Individual red, green, and blue primary color coordinates
- **White Point**: D65 white point measurement and validation

### Device Support
- **Konica Minolta CA-200** colorimeter
- **Konica Minolta CA-310** colorimeter
- Multi-channel measurement support (channels 0-99)
- Automatic device calibration

### Data Management
- Device configuration loading from YAML files
- Automatic validation against min/max/typical specifications
- Measurement results export
- Parallel report upload to server
- History and result tracking

### User Interface
- Modern Material Design interface
- Real-time measurement display
- Visual pass/fail indicators
- Multiple language support (English, 中文)
- Dark/Light theme support

## System Requirements

### Runtime Requirements
- **Operating System**: Windows 10/11 (x64)
- **.NET Runtime**: .NET 8.0 Desktop Runtime
- **Hardware**: Konica Minolta CA-200 or CA-310 colorimeter with USB interface
- **Drivers**: Konica Minolta CA-SDK2 (COM interface)

### Development Requirements
- **.NET SDK**: .NET 8.0 SDK or later
- **Build Tools**:
  - MSBuild 17.0+ (included with Visual Studio or Build Tools)
  - OR Visual Studio 2022+ with .NET desktop development workload
- **COM Interop**: Windows SDK for COM reference resolution
- **Testing**: NUnit 3.14+ test runner

## Installation

### For End Users

1. Download the latest release from the releases page
2. Extract the archive to your desired location
3. Install the Konica Minolta CA-S40 setup (includes CA-SDK2 for the CA-410 and the USB driver) and, for the CA-310, the CA-SDK; the application locates the CA-SDK2 libraries in the CA-S40 install folder and works with the CA-310 only when it is absent
4. Run `Screen Checker.exe`

### For Developers

1. Clone the repository:
```bash
git clone <repository-url>
cd sct-screen-checker
```

2. Ensure .NET 8.0 SDK is installed:
```bash
dotnet --version
# Should output 8.0.x or higher
```

3. Restore dependencies:
```bash
dotnet restore "Screen Checker.sln"
```

## Building the Project

The project uses COM interop for the Konica Minolta colorimeter, which requires MSBuild for proper COM reference resolution.

### Using MSBuild (Recommended)

**Debug Build:**
```bash
msbuild "Screen Checker.sln" /p:Configuration=Debug
```

**Release Build:**
```bash
msbuild "Screen Checker.sln" /p:Configuration=Release
```

### Using Visual Studio

1. Open `Screen Checker.sln`
2. Select Debug or Release configuration
3. Build → Build Solution (Ctrl+Shift+B)

### Build Output

Executable and dependencies will be located in:
- Debug: `Screen Checker\bin\Debug\net8.0-windows\`
- Release: `Screen Checker\bin\Release\net8.0-windows\`

**Note:** Building with `dotnet build` may fail due to ResolveComReference tasks. Always use MSBuild or Visual Studio for building.

## Running the Application

### From Build Output
```bash
cd "Screen Checker\bin\Debug\net8.0-windows"
.\"Screen Checker.exe"
```

### From Visual Studio
Press F5 (Start Debugging) or Ctrl+F5 (Start Without Debugging)

## Usage

### Initial Setup

1. **Connect Colorimeter**: Connect your Konica Minolta CA-200/CA-310 via USB
2. **Select Channel**: Choose the measurement channel (0-99) in Settings
3. **Calibrate Device**: Run calibration before first measurement
4. **Load Device Config**: Select the appropriate YAML configuration for your display model

### Performing Measurements

1. **Select Measurement Point**: Choose the location on screen to measure
2. **Display Test Pattern**: The application will display the appropriate color pattern
3. **Position Probe**: Place the colorimeter probe on the measurement point
4. **Measure**: Click the Measure button or press hotkey
5. **Validate Results**: Check pass/fail status against specifications
6. **Export**: Save results to file or upload to server

### Configuration Files

Device configurations are stored in `config/device_configs/` as YAML files with the following structure:

```yaml
main_tests:
  Brightness:
    min: 330
    typ: 350
    max: 500
  Contrast:
    min: 3500.0
    typ: 5000.0
    max: 1000000.0
  Red_x:
    min: 0.629
    typ: 0.659
    max: 0.689
  # ... additional parameters
```

## Configuration

### Application Settings

Settings are stored in `%AppData%\WPF_LCD_Test\settings.json`:

- **ColorAnalyzerChannel**: Measurement channel (0-99)
- **LanguageCultureCode**: UI language ("" for default / "zh-Hans" for Chinese Simplified)
- **MeasurementSpeed**: Analyzer measurement speed ("Slow" by default / "Fast" / "Auto")
- **SyncMode**: Synchronization mode ("Universal" by default / "Ntsc" / "Pal" / "Ext" / "Internal" / "Manual" (CA-410 only))
- **SyncFrequency**: Frequency in Hz for the internal synchronization (40-200 for the CA-310, 0.5-240 for the CA-410)
- **IntegrationTime**: Integration time in ms for the manual synchronization (CA-410 only, 4-4000)

### Device Configurations

Place YAML device configuration files in `config/device_configs/`. Each configuration defines acceptable ranges for:
- Brightness and uniformity
- Contrast ratio
- Color gamut (RGB and NTSC)
- RGB primary coordinates (x, y)
- White point coordinates (x, y)
- Color temperature
- Delta E color difference

## Architecture

### Pattern: MVVM (Model-View-ViewModel)

```
┌─────────────────┐
│     Views/      │  XAML UI (MainWindow, MeasurementView, SettingsView)
└────────┬────────┘
         │ Data Binding
┌────────▼────────┐
│  ViewModels/    │  Presentation logic with RelayCommand
└────────┬────────┘
         │ Business Logic
┌────────▼────────┐
│   Services/     │  Core functionality (interface-based)
└────────┬────────┘
         │ Data Access
┌────────▼────────┐
│    Models/      │  Data classes (Measurement, DeviceUnderTest, AppSettings)
└─────────────────┘
```

### Key Components

#### Services
- **ColorMeasurementService**: COM interop with Konica Minolta device
- **SettingsService**: Application configuration management (singleton)
- **LocalizationService**: Runtime language switching (singleton)
- **FileService**: File I/O, JSON serialization, CSV export
- **UploadService**: Parallel report upload to Nextcloud server
- **DialogService**: File dialogs and message boxes
- **MeasurementStatusService**: Tracks measurement status and state
- **PathProvider**: Centralized path management (`BaseDirectory`, `ConfigDirectory`, `DataDirectory`)

#### Wrappers
Abstractions over system dependencies for testability:
- **Ca310Wrappers / Ca410Wrappers**: device wrappers (CA-310 via COM: `Ca310MemoryWrapper`, `Ca310ProbeWrapper`; CA-410 via CA-SDK2: `Ca410AnalyzerWrapper`) with proper `Marshal.ReleaseComObject` disposal
- **IFileSystem** (System.IO.Abstractions): File system abstraction with `MockFileSystem` support in tests
- **WpfDispatcher**: `IDispatcher` implementation wrapping WPF `Dispatcher` for UI thread access
- **MessageBoxWrapper**: `IMessageBox` abstraction over `System.Windows.MessageBox`

#### Dependency Injection
`Microsoft.Extensions.DependencyInjection` container configured in `App.xaml.cs`. Services registered with appropriate lifetimes (Singleton/Transient) and resolved via `IServiceProvider`. All services use constructor injection — no static singletons.

## Development

### Project Structure

```
Screen Checker/
├── Commands/           # RelayCommand implementation
├── Converters/         # WPF value converters
├── Interfaces/         # Service contracts
├── Models/             # Data models
├── Services/           # Business logic
├── ViewModels/         # Presentation logic
├── Views/              # XAML UI
├── Wrappers/           # System dependency abstractions
├── Resources/          # Localization resources
└── config/             # Device configurations

Screen Checker.UnitTests/
├── Models/             # Model tests
├── Services/           # Service tests
└── ViewModels/         # ViewModel tests
```

### Adding New Device Configurations

1. Create a new YAML file in `config/device_configs/`
2. Name it after the device model (e.g., `MODEL-123_REV1.yaml`)
3. Define all required test parameters with min/typ/max values
4. Place file in the config directory
5. Select it from the UI Settings page

### Coding Conventions

- All services implement interfaces for dependency injection
- System dependencies (COM, file system, UI) are wrapped in testable abstractions
- ViewModels expose ICommand properties using RelayCommand
- All business logic is tested with NUnit and Moq

## Testing

### Running All Tests

Build the solution first (MSBuild required due to COM interop), then:

```bash
dotnet test "Screen Checker.UnitTests\Screen Checker.UnitTests.csproj" --no-build
```

### Running Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~MeasurementViewModelTests" --no-build
```

### Running Single Test Method

```bash
dotnet test --filter "FullyQualifiedName~MeasurementViewModelTests.TestMethodName" --no-build
```

### Test Coverage

The test suite includes:
- **ViewModel Tests**: UI logic, commands, property changes
- **Service Tests**: Business logic with mocked dependencies
- **Model Tests**: Data validation and calculations
- **Integration Tests**: Service interaction scenarios

All tests use Moq for mocking interfaces (IColorMeasurementService, IFileService, etc.).

## Localization

### Supported Languages

- English (default)
- Chinese Simplified (zh-Hans)

### Language Switching

Change language at runtime in Settings. Localization resources are in:
- `Resources/StringResources.xaml` (English)
- `Resources/StringResources.zh-Hans.xaml` (Chinese)

### Adding New Languages

1. Duplicate `StringResources.xaml`
2. Rename to `StringResources.[culture-code].xaml`
3. Translate all string values
4. Update `LocalizationService.cs` to include new culture
5. Add culture option to Settings UI

## Releasing

Screen Checker is released with the shared tool `tools/release/app_release.py` of the SCT Updater repository. `sct_app.toml` in this repository describes the project (`kind = "dotnet"`, the project file, the exe name and the publish profile). The tool builds the application itself, so no manual publish step is needed.

Requirements: [uv](https://docs.astral.sh/uv/), **MSBuild** (Visual Studio or the free Visual Studio Build Tools with the ".NET desktop build tools" workload; the tool takes it from the `MSBUILD` environment variable, then from `PATH`, then finds it with vswhere) and the credentials in `tools/release/.env` of the SCT Updater repository.

From the SCT Updater repository:

```bash
uv run tools/release/app_release.py app <path of this repository> --upload
```

Without `--upload` it is a dry run (nothing is sent). The tool publishes the project with MSBuild (Release, the `FolderProfile` publish profile) into a clean folder, reads the version from the built `Screen Checker.exe` (`AssemblyVersion` in `Screen Checker.csproj`; a version given on the command line must match it), zips the result, uploads it to `SCT/Updater/versions/screen_checker/<version>/` and publishes the manifest last. The package holds only the build output: files that the application writes itself (`appsettings.json`, `analyzers.json`, `data`) are never shipped. Then put the version into the `dev` list of `suite_manifest.json` by hand (into `products` when it is verified).

Full process: `docs/module-release.md` in the SCT Updater repository.

## Roadmap

### Planned Features

- **Results Management**: Dedicated page for browsing historical measurement results
- **Enhanced Data Model**: Split location parameter into `MeasurementPoint` with Position and Color properties
- **PDF Reports**: PDF export in addition to existing JSON/CSV formats
- **Batch Testing**: Automated multi-point measurement sequences
- **Statistical Analysis**: Trend analysis and statistical reporting

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for the full version history.

## License

[Add your license information here]

---

**Konica Minolta CA-200/CA-310** are trademarks of Konica Minolta, Inc.

using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using ScreenChecker.Commands;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.Services;
using static ScreenChecker.Resources.Resources;

namespace ScreenChecker.ViewModels
    {
    public partial class MeasurementViewModel : BaseViewModel, IDisposable
        {
        #region Fields & Constants

        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IFileService _fileService;
        private readonly IDialogService _dialogService;
        private readonly ILocalizationService _localizationService;
        private readonly IDispatcher _dispatcher;
        private readonly IUploadService _uploadService;
        private readonly ISettingsService _settingsService;
        private readonly IPathProvider _pathProvider;
        private readonly IMeasurementStatusService _measurementStatusService;
        private readonly ISpecCheckCoordinator _specCheck;
        private readonly ILogService _logService;

        private DeviceUnderTest? _currentDevice;
        private string _serialNumber = string.Empty;
        private bool _isSerialNumberConfirmed;
        private bool _isChannelConfirmed;
        private ColorimeterInfo? _colorimeter;
        private readonly IAnalyzerRegistryService _analyzerRegistryService;
        private readonly IMeasurementAcceptance _acceptance;
        private readonly IDeviceConfigurationService _deviceConfigurationService;
        private readonly IReportGenerationService _reportGenerationService;
        private int _measurementTime = 2;
        private string _selectedDeviceConfiguration;
        private bool _isMeasurementButtonsEnabled;
        private bool _isDeviceConnected;
        private bool _isDeviceCalibrated;
        private bool _isDeviceConnecting;
        private bool _isDeviceCalibrating;
        private bool _isTvCheckboxChecked;
        private bool _isReportGenerating;
        private bool _isUploadingReports;
        private int _selectedTabIndex;

        private readonly Dictionary<string, DeviceConfigTreeNode> _configNodesByPath = [];
        private DeviceConfigTreeNode? _selectedConfigNode;

        private const string SERIAL_NUMBER_PATTERN = "^[a-zA-Z0-9]*$";
        private const string PreviewSerialNumber = "preview";
        private const int LogTabIndex = 1;

        public event EventHandler RequestClearInputFocus;

        #endregion

        #region Properties

        public DeviceUnderTest? CurrentDevice
            {
            get => _currentDevice;
            set => SetProperty(ref _currentDevice, value);
            }

        /// <summary>
        /// Gets the device info: the specification check of the measurements taken so far.
        /// </summary>
        public SpecSummaryViewModel Spec { get; }

        /// <summary>
        /// Gets the signal the Save results button breathes on (green: saved, red: failed).
        /// </summary>
        public ActionFeedbackSignal SaveFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Clear results button breathes on.
        /// </summary>
        public ActionFeedbackSignal ClearFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Create report button breathes on.
        /// </summary>
        public ActionFeedbackSignal ReportFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Upload reports button breathes on.
        /// </summary>
        public ActionFeedbackSignal UploadFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the serial number field breathes on (green: applied, red: rejected).
        /// </summary>
        public ActionFeedbackSignal SerialFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the Zero Calibration button breathes on when the calibration ends: green when the probe is calibrated and its
        /// channel confirmed, red when the connection or the calibration failed, canceled when the inspector dismissed the channel dialog.
        /// </summary>
        public ActionFeedbackSignal ZeroCalibrationFeedback { get; } = new();

        /// <summary>
        /// Gets the signal the device configuration selector breathes on when a configuration is chosen.
        /// </summary>
        public ActionFeedbackSignal ConfigFeedback { get; } = new();

        /// <summary>
        /// Gets the suffix of the Upload reports button label with the number of reports waiting for upload (empty when none).
        /// </summary>
        public string PendingReportsSuffix => _pendingReportsCount > 0 ? $" ({_pendingReportsCount})" : string.Empty;

        public bool IsTvCheckboxChecked
            {
            get => _isTvCheckboxChecked;
            set
                {
                if (SetProperty(ref _isTvCheckboxChecked, value))
                    {
                    RefreshResults();
                    RequestSpecCheck();
                    }
                }
            }

        public string SelectedDeviceConfiguration
            {
            get => _selectedDeviceConfiguration;
            set
                {
                if (SetProperty(ref _selectedDeviceConfiguration, value))
                    {
                    OnPropertyChanged(nameof(SelectedDeviceConfigurationDisplayName));
                    OnPropertyChanged(nameof(HasSelectedDeviceConfiguration));
                    UpdateCommandsCanExecute();
                    UpdateSelectedConfigNodeHighlight();
                    RequestSpecCheck();
                    }
                }
            }

        /// <summary>
        /// Gets the file name (without folder path) of the currently selected device configuration,
        /// shown on the closed cascading configuration menu button, or a placeholder when none is selected.
        /// </summary>
        public string SelectedDeviceConfigurationDisplayName =>
            string.IsNullOrEmpty(_selectedDeviceConfiguration) ? ChooseConfiguration : Path.GetFileName(_selectedDeviceConfiguration);

        /// <summary>
        /// Gets every text the configuration button can show: the placeholder and the name of every configuration.
        /// The button is as wide as the widest of them, so it does not change its width when another configuration is chosen.
        /// </summary>
        public IReadOnlyList<string> DeviceConfigurationCaptions =>
            [ChooseConfiguration, .. _configNodesByPath.Values.Select(node => node.DisplayName).Distinct()];

        /// <summary>
        /// Gets whether a device configuration is currently selected, used to dim the
        /// configuration button text while it shows the "choose configuration" placeholder.
        /// </summary>
        public bool HasSelectedDeviceConfiguration => !string.IsNullOrEmpty(_selectedDeviceConfiguration);

        public string SerialNumber
            {
            get => _serialNumber;
            set
                {
                if (SetProperty(ref _serialNumber, value))
                    {
                    UpdateUIState();
                    }
                }
            }

        public bool IsSerialNumberConfirmed
            {
            get => _isSerialNumberConfirmed;
            set
                {
                if (SetProperty(ref _isSerialNumberConfirmed, value))
                    {
                    UpdateUIState();
                    }
                }
            }

        private bool _isSavingDevice;
        private bool _hasUnsavedMeasurements;
        private int _pendingReportsCount;
        private CancellationTokenSource? _reportCts;

        public bool IsReportGenerating
            {
            get => _isReportGenerating;
            set
                {
                if (SetProperty(ref _isReportGenerating, value))
                    UpdateCommandsCanExecute();
                }
            }

        public bool IsUploadingReports
            {
            get => _isUploadingReports;
            set
                {
                if (SetProperty(ref _isUploadingReports, value))
                    UpdateCommandsCanExecute();
                }
            }

        public int MeasurementTime
            {
            get => _measurementTime;
            set
                {
                if (value > 0)
                    SetProperty(ref _measurementTime, value);
                else
                    OnPropertyChanged();
                }
            }

        /// <summary>
        /// Gets the application log: its text, the warnings and errors of the Log tab and the unread counter.
        /// </summary>
        public ILogService LogService => _logService;

        /// <summary>
        /// Gets the per-point values of the Results tab.
        /// </summary>
        public ObservableCollection<MeasurementResultRow> Results { get; }

        /// <summary>
        /// Gets or sets the selected tab of the left block: 0 - Results, 1 - Log. Opening the log clears the unread counter.
        /// </summary>
        public int SelectedTabIndex
            {
            get => _selectedTabIndex;
            set
                {
                if (SetProperty(ref _selectedTabIndex, value))
                    _logService.IsShown = value == LogTabIndex;
                }
            }

        public bool IsMeasurementButtonsEnabled
            {
            get => _isMeasurementButtonsEnabled;
            set => SetProperty(ref _isMeasurementButtonsEnabled, value);
            }

        public bool IsDeviceConnected
            {
            get => _isDeviceConnected;
            set
                {
                if (SetProperty(ref _isDeviceConnected, value))
                    {
                    UpdateUIState();
                    OnPropertyChanged(nameof(DeviceStateText));
                    }
                }
            }

        public bool IsDeviceCalibrated
            {
            get => _isDeviceCalibrated;
            set
                {
                if (SetProperty(ref _isDeviceCalibrated, value))
                    {
                    if (!value) IsChannelConfirmed = false;
                    UpdateUIState();
                    OnPropertyChanged(nameof(DeviceStateText));
                    }
                }
            }

        /// <summary>
        /// Gets the full description of the analyzer state shown as the tooltip of the state indicator.
        /// </summary>
        public string DeviceStateText => !_isDeviceConnected ? DeviceStateNotConnected
            : _isDeviceCalibrated ? DeviceStateConnectedCalibrated
            : DeviceStateConnectedNotCalibrated;

        /// <summary>
        /// Gets the analyzer model name: the live value while the channel is confirmed, the last confirmed one
        /// while it is being re-confirmed, empty before the first confirmation (the card does not show it then).
        /// </summary>
        public string DeviceModelName =>
            _isChannelConfirmed ? _colorMeasurementService.Model.ToDisplayName() : _colorimeter?.Model ?? string.Empty;

        /// <summary>
        /// Gets the active measurement channel label: the live value while the channel is confirmed, the last confirmed one
        /// while it is being re-confirmed, empty before the first confirmation (the card does not show it then).
        /// </summary>
        public string DeviceActiveChannelStatusText =>
            _isChannelConfirmed ? FormatChannel(_colorMeasurementService.CurrentChannel, _colorMeasurementService.ChannelName)
            : _colorimeter != null ? FormatChannel(_colorimeter.Channel, _colorimeter.ChannelName)
            : string.Empty;

        /// <summary>
        /// Formats the channel label, appending the user-defined channel name in parentheses when it is set.
        /// </summary>
        /// <param name="channel">Channel number.</param>
        /// <param name="channelName">User-defined channel name; may be null or empty.</param>
        /// <returns>The label, e.g. "Channel 2 (Lab TV)".</returns>
        private static string FormatChannel(int channel, string? channelName) =>
            string.IsNullOrWhiteSpace(channelName)
                ? string.Format(ActiveChannelStatus, channel)
                : $"{string.Format(ActiveChannelStatus, channel)} ({channelName})";

        /// <summary>
        /// Gets the probe serial number label: the last confirmed probe, empty before the first confirmation.
        /// </summary>
        public string DeviceProbeSerialText =>
            _colorimeter != null ? string.Format(ProbeSerialNumberLabel, _colorimeter.ProbeSN) : string.Empty;

        /// <summary>
        /// Gets or sets whether the inspector has confirmed the active channel after calibration.
        /// Model and channel labels stay hidden until then.
        /// </summary>
        public bool IsChannelConfirmed
            {
            get => _isChannelConfirmed;
            set
                {
                if (SetProperty(ref _isChannelConfirmed, value))
                    {
                    OnPropertyChanged(nameof(DeviceModelName));
                    OnPropertyChanged(nameof(DeviceActiveChannelStatusText));
                    OnPropertyChanged(nameof(DeviceProbeSerialText));
                    }
                }
            }
        public ObservableCollection<DeviceConfigTreeNode> DeviceConfigurationTree { get; } = [];
        public IMeasurementStatusService MeasurementStatusService => _measurementStatusService;

        #endregion

        #region Commands

        public ICommand ZeroCalibrationCommand { get; }
        public ICommand SaveResultsCommand { get; }
        public ICommand ClearFieldsCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand SwitchLanguageCommand { get; }
        public ICommand MeasureCommand { get; }
        public ICommand ApplySerialNumberCommand { get; }
        public ICommand NewDeviceUnderTestCommand { get; }
        public ICommand ReportGenerateCommand { get; }
        public ICommand UploadReportsCommand { get; }
        public ICommand SelectDeviceConfigurationCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasurementViewModel"/> class.
        /// Configures services, commands, and event subscriptions.
        /// </summary>
        /// <param name="colorMeasurementService">Service for colorimeter interaction.</param>
        /// <param name="fileService">Service for file operations.</param>
        /// <param name="dialogService">Service for displaying dialogs.</param>
        /// <param name="localizationService">Service for localization management.</param>
        /// <param name="dispatcher">Dispatcher for UI thread marshaling.</param>
        /// <param name="uploadService">Service for uploading reports.</param>
        /// <param name="settingsService">Service for application settings.</param>
        /// <param name="measurementStatusService">Service for managing measurement statuses.</param>
        /// <param name="analyzerRegistryService">Registry of the last confirmed channel per probe.</param>
        /// <param name="specCheck">Coordinator checking the measurements against the device specification.</param>
        /// <param name="acceptance">Rules deciding whether a measurement is plausible enough to be kept.</param>
        /// <param name="deviceConfigurationService">Service discovering the device configuration files.</param>
        /// <param name="reportGenerationService">Service running the report generator.</param>
        /// <param name="logService">Application log.</param>
        public MeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService,
            IDispatcher dispatcher,
            IUploadService uploadService,
            ISettingsService settingsService,
            IPathProvider pathProvider,
            IMeasurementStatusService measurementStatusService,
            IAnalyzerRegistryService analyzerRegistryService,
            ISpecCheckCoordinator specCheck,
            IMeasurementAcceptance acceptance,
            IDeviceConfigurationService deviceConfigurationService,
            IReportGenerationService reportGenerationService,
            ILogService logService)
            {
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
            _pathProvider = pathProvider ?? throw new ArgumentNullException(nameof(pathProvider));
            _measurementStatusService = measurementStatusService ?? throw new ArgumentNullException(nameof(measurementStatusService));
            _analyzerRegistryService = analyzerRegistryService ?? throw new ArgumentNullException(nameof(analyzerRegistryService));
            _specCheck = specCheck ?? throw new ArgumentNullException(nameof(specCheck));
            _acceptance = acceptance ?? throw new ArgumentNullException(nameof(acceptance));
            _deviceConfigurationService = deviceConfigurationService ?? throw new ArgumentNullException(nameof(deviceConfigurationService));
            _reportGenerationService = reportGenerationService ?? throw new ArgumentNullException(nameof(reportGenerationService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            Spec = new SpecSummaryViewModel(_measurementStatusService, _logService.Append);
            Results = new(( _measurementStatusService.AllMeasurementButtonStatuses ?? [] ).Select(status => new MeasurementResultRow(status)));

            ZeroCalibrationCommand = new RelayCommand(ExecuteZeroCalibrationAsync, CanExecuteZeroCalibration);
            SaveResultsCommand = new RelayCommand(ExecuteSaveResultsAsync, CanExecuteSaveResults);
            ClearFieldsCommand = new RelayCommand(ExecuteClearFields, CanExecuteClearFields);
            ClearLogCommand = new RelayCommand(_logService.Clear);
            SwitchLanguageCommand = new RelayCommand(ExecuteSwitchLanguage, CanExecuteSwitchLanguage);
            NewDeviceUnderTestCommand = new RelayCommand(ExecuteNewDeviceUnderTest, CanExecuteNewDeviceUnderTest, allowConcurrentExecution: true);
            ReportGenerateCommand = new RelayCommand(ExecuteReportGenerateCommandAsync, CanExecuteReportGenerateCommand, allowConcurrentExecution: true);
            MeasureCommand = new RelayCommand(ExecuteMeasureAsync, CanExecuteMeasure);
            ApplySerialNumberCommand = new RelayCommand(ExecuteApplySerialNumber, CanExecuteApplySerialNumber);
            UploadReportsCommand = new RelayCommand(ExecuteUploadReportsAsync, CanExecuteUploadReportsAsync, allowConcurrentExecution: true);
            RefreshPendingReports();
            SelectDeviceConfigurationCommand = new RelayCommand(ExecuteSelectDeviceConfiguration, CanExecuteSelectDeviceConfiguration);

            _colorMeasurementService.StatusMessage += ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage += FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged += ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged += ColorMeasurementService_CalibrationStatusChanged;
            _colorMeasurementService.CurrentChannelChanged += ColorMeasurementService_CurrentChannelChanged;
            _uploadService.StatusMessage += UploadService_StatusMessage;
            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;

            InitializeDeviceConfigurations();
            RefreshResults();
            RequestSpecCheck();
            UpdateUIState();
            }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the device configuration files, including any nested subfolders,
        /// and populates the hierarchical selection tree for cascading menu display.
        /// </summary>
        private void InitializeDeviceConfigurations()
            {
            try
                {
                var nodes = _deviceConfigurationService.LoadTree(SelectDeviceConfigurationCommand);

                DeviceConfigurationTree.Clear();
                _configNodesByPath.Clear();
                _selectedConfigNode = null;
                foreach (var node in nodes)
                    DeviceConfigurationTree.Add(node);
                IndexConfigurationNodes(nodes);
                OnPropertyChanged(nameof(DeviceConfigurationCaptions));

                if (_configNodesByPath.Count == 0)
                    Log(ConfigDirNotFound, [_pathProvider.ConfigDirectory], LogSeverity.Error);
                }
            catch (Exception ex)
                {
                Log(ErrUnexpected, [ex.Message], LogSeverity.Error);
                }
            }

        /// <summary>
        /// Indexes the configuration file nodes of the tree by their relative path.
        /// </summary>
        /// <param name="nodes">Nodes to index, folders are searched recursively.</param>
        private void IndexConfigurationNodes(IEnumerable<DeviceConfigTreeNode> nodes)
            {
            foreach (var node in nodes)
                {
                if (node.IsFolder) IndexConfigurationNodes(node.Children);
                else _configNodesByPath[node.RelativePath!] = node;
                }
            }

        /// <summary>
        /// Moves the checkmark shown in the cascading configuration menu to the node matching
        /// the currently selected device configuration.
        /// </summary>
        private void UpdateSelectedConfigNodeHighlight()
            {
            if (_selectedConfigNode != null)
                _selectedConfigNode.IsSelected = false;

            _selectedConfigNode = !string.IsNullOrEmpty(_selectedDeviceConfiguration) &&
                _configNodesByPath.TryGetValue(_selectedDeviceConfiguration, out var node)
                    ? node
                    : null;

            if (_selectedConfigNode != null)
                _selectedConfigNode.IsSelected = true;
            }

        /// <summary>
        /// Applies the device configuration selected from the cascading configuration menu.
        /// </summary>
        /// <param name="parameter">Relative path identifier of the selected configuration file.</param>
        private void ExecuteSelectDeviceConfiguration(object parameter)
            {
            SelectedDeviceConfiguration = (string)parameter;
            Flash(ConfigFeedback, true);
            }

        /// <summary>
        /// Determines whether a configuration menu entry represents a selectable configuration file
        /// rather than a folder node.
        /// </summary>
        /// <param name="parameter">Relative path identifier, or null for folder nodes.</param>
        /// <returns>True if the entry is a configuration file; otherwise, false.</returns>
        private bool CanExecuteSelectDeviceConfiguration(object parameter)
            {
            return parameter is string relativePath && !string.IsNullOrEmpty(relativePath);
            }

        /// <summary>
        /// Verifies if all required measurement statuses are present in the current device data.
        /// </summary>
        /// <returns>True if all statuses are represented; otherwise, false.</returns>
        public bool AreAllStatusesRepresentedInMeasurements()
            {
            if (_currentDevice?.Measurements == null || _currentDevice.Measurements.Count == 0)
                return false;

            var allStatusPoints = _measurementStatusService.AllMeasurementButtonStatuses;
            if (allStatusPoints == null || !allStatusPoints.Any())
                return false;

            return allStatusPoints.All(status =>
                ( status.Location == MeasurementLocation.WhiteColor.ToString() && !_currentDevice.IsTV ) ||
                _currentDevice.Measurements.Any(measurement => measurement.Location == status.Location));
            }

        /// <summary>
        /// Attempts to establish a connection with the measurement device.
        /// </summary>
        /// <returns>True if connection was successful, false otherwise.</returns>
        private async Task<bool> ExecuteConnectAsync()
            {
            if (!CanExecuteConnect()) return false;

            try
                {
                _isDeviceConnecting = true;
                bool connected = await _colorMeasurementService.ConnectAsync();
                if (!connected)
                    ExecuteDisconnect(force: true);
                return connected;
                }
            catch
                {
                ExecuteDisconnect(force: true);
                return false;
                }
            finally
                {
                _isDeviceConnecting = false;
                }
            }

        /// <summary>
        /// Disconnects the measurement device and updates the UI state.
        /// </summary>
        /// <param name="force">If true, bypasses state checks for error recovery scenarios.</param>
        private void ExecuteDisconnect(bool force = false)
            {
            if (!force && !CanExecuteDisconnect()) return;

            try
                {
                _colorMeasurementService.Disconnect();
                }
            catch (Exception ex)
                {
                _dialogService.ShowMessage($"{ErrUnexpected}: {ex.Message}", $"{Err}");
                }
            UpdateUIState();
            }

        /// <summary>
        /// Performs zero calibration on the connected device.
        /// </summary>
        /// <param name="parameter">Command parameter (unused).</param>
        private async Task ExecuteZeroCalibrationAsync(object parameter)
            {
            if (!CanExecuteZeroCalibration(parameter)) return;

            try
                {
                // Use return value directly to avoid race condition with event-based property updates
                bool isConnected = _colorMeasurementService.IsDeviceConnected;
                if (!isConnected)
                    isConnected = await ExecuteConnectAsync();

                if (isConnected)
                    {
                    _isDeviceCalibrating = true;
                    if (!await _colorMeasurementService.CalibrateZeroAsync())
                        {
                        ExecuteDisconnect(force: true);
                        Flash(ZeroCalibrationFeedback, false);
                        _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
                        return;
                        }
                    if (!ConfirmActiveChannel())
                        {
                        ExecuteDisconnect(force: true);
                        FlashCanceled(ZeroCalibrationFeedback);
                        return;
                        }
                    Flash(ZeroCalibrationFeedback, true);
                    }
                else
                    {
                    Flash(ZeroCalibrationFeedback, false);
                    }
                }
            catch
                {
                // Force disconnect on error to cleanup COM objects
                if (_colorMeasurementService.IsDeviceConnected)
                    ExecuteDisconnect(force: true);
                Flash(ZeroCalibrationFeedback, false);
                _dialogService.ShowMessage($"{ErrAtCalibration}", $"{Err}");
                }
            finally
                {
                _isDeviceCalibrating = false;
                UpdateUIState();
                }
            }

        /// <summary>
        /// Reconciles the channel currently set on the probe with the last channel registered for it.
        /// On mismatch the inspector picks one of the two; the confirmed channel is stored in the registry and in the settings.
        /// </summary>
        /// <returns>True when a channel was confirmed; false when the inspector dismissed the dialog.</returns>
        private bool ConfirmActiveChannel()
            {
            string probeSN = _colorMeasurementService.ProbeSN;
            int channel = _colorMeasurementService.CurrentChannel;

            if (_analyzerRegistryService.GetLastChannel(probeSN) is int lastChannel && lastChannel != channel)
                {
                int? choice = _dialogService.ShowChoice(
                    string.Format(ChannelMismatchMessage, lastChannel, channel),
                    ChannelMismatchTitle,
                    string.Format(UseProbeChannel, channel),
                    string.Format(UseLastChannel, lastChannel));

                if (choice is null) return false;

                if (choice == 1)
                    {
                    _colorMeasurementService.CurrentChannel = lastChannel;
                    channel = lastChannel;
                    }
                }

            _analyzerRegistryService.Update(probeSN, _colorMeasurementService.Model, channel);
            _settingsService.UpdateChannel(channel);
            string channelName = _colorMeasurementService.ChannelName;
            _colorimeter = new ColorimeterInfo(
                _colorMeasurementService.Model.ToDisplayName(),
                probeSN,
                channel,
                string.IsNullOrWhiteSpace(channelName) ? null : channelName);
            if (_currentDevice != null) _currentDevice.Colorimeter = _colorimeter;
            IsChannelConfirmed = true;
            return true;
            }

        /// <summary>
        /// Saves the measurement results of the current device to a JSON file.
        /// </summary>
        /// <param name="parameter">Command parameter (unused).</param>
        /// <returns>True if save was successful; otherwise, false.</returns>
        private async Task<bool> ExecuteSaveResultsAsync(object parameter)
            {
            if (!CanExecuteSaveResults(parameter)) return false;

            Log(Saving);
            try
                {
                if (_currentDevice == null || string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) || _currentDevice.Measurements.Count == 0)
                    {
                    _dialogService.ShowMessage($"{SaveJSONErrDeviceIsEmpty}", $"{Err}");
                    Flash(SaveFeedback, false);
                    return false;
                    }

                _currentDevice.DeviceConfiguration = SelectedDeviceConfiguration;
                _currentDevice.IsTV = IsTvCheckboxChecked;

                await _specCheck.Pending;
                string? warning = Spec.BuildSaveWarning(!AreAllStatusesRepresentedInMeasurements());
                if (warning != null && !_dialogService.ShowQuestion(warning, $"{Warning}"))
                    {
                    Log(SaveCanceled);
                    return false;
                    }

                bool saved = await _fileService.SaveDeviceDataToJsonAsync(_currentDevice);
                if (saved) _hasUnsavedMeasurements = false;
                Flash(SaveFeedback, saved);
                return saved;
                }
            catch (Exception ex)
                {
                Log(ErrUnexpected, [ex.Message], LogSeverity.Error);
                _dialogService.ShowMessage($"{SaveJSONErrForSN}: {ex.Message}", $"{Err}");
                Flash(SaveFeedback, false);
                return false;
                }
            }

        /// <summary>
        /// Clears the input fields and resets the current device state.
        /// </summary>
        /// <param name="parameter">Optional context string triggering the clear action.</param>
        private void ExecuteClearFields(object parameter)
            {
            string approveQuestion = parameter?.Equals("CalledFromNewDeviceMethod") == true
                ? CleanFieldWarningAfterSave
                : CleanFieldWarning;

            if (_dialogService.ShowQuestion(approveQuestion, Warning))
                {
                SerialNumber = "";
                MeasurementTime = 2;
                _currentDevice = null;
                IsSerialNumberConfirmed = false;
                ResetMeasurementStatuses();
                Log(ClearFieldsDone);
                _hasUnsavedMeasurements = false;
                if (parameter?.Equals("CalledFromNewDeviceMethod") != true) Flash(ClearFeedback, true);
                Spec.Reset();
                RequestSpecCheck();
                UpdateUIState();
                }
            }

        /// <summary>
        /// Switches the application language.
        /// </summary>
        /// <param name="parameter">The language code (e.g., "en-US").</param>
        private void ExecuteSwitchLanguage(object parameter)
            {
            if (parameter is string languageCode && !string.IsNullOrWhiteSpace(languageCode))
                {
                try
                    {
                    _localizationService.SetLanguage(languageCode);
                    }
                catch (Exception ex)
                    {
                    _dialogService.ShowMessage($"{ErrMsgLangSwitchFailed}: {ex.Message}", $"{Err}");
                    }
                }
            }

        /// <summary>
        /// Orchestrates the measurement process, including validation, retries, and UI updates.
        /// </summary>
        /// <param name="parameter">The measurement location (e.g., "RedColor").</param>
        private async Task ExecuteMeasureAsync(object parameter)
            {
            if (parameter?.ToString() is not string measurementLocation || !CanExecuteMeasure(parameter)) return;

            bool isMeasurementSuccess = false;
            string messageWithMeasuredValues = $"{NoData}";
            UpdateMeasurementStatus(measurementLocation, null, $"{Measuring}");

            try
                {
                if (!EnsureDeviceInitialized(measurementLocation, ref messageWithMeasuredValues))
                    return;

                var measurement = await _colorMeasurementService.MeasureAsync(_measurementTime);

                if (measurement != null && measurement.IsValid)
                    {
                    measurement.Location = measurementLocation;
                    ProcessValidMeasurement(measurement, ref isMeasurementSuccess, ref messageWithMeasuredValues);
                    }
                else
                    {
                    messageWithMeasuredValues = measurement != null ? $"{InvalidResultErr}" : $"{ColorAnalyzerErr}";
                    Log(ColorServiceErr, [measurementLocation], LogSeverity.Error);
                    }
                }
            catch (Exception ex)
                {
                _dialogService.ShowMessage($"{UnexpectedMeasurementErr} '{measurementLocation}': {ex.Message}", $"{Err}");
                messageWithMeasuredValues = $"{Err}: {ex.Message}";
                }
            finally
                {
                UpdateMeasurementStatus(measurementLocation, isMeasurementSuccess, messageWithMeasuredValues);
                UpdateCommandsCanExecute();
                RefreshResults();
                RequestSpecCheck();
                }
            }

        /// <summary>
        /// Applies the serial number entered by the user.
        /// </summary>
        /// <param name="parameter">The serial number string.</param>
        public void ExecuteApplySerialNumber(object parameter)
            {
            if (parameter is not string enteredSerialNumber || string.IsNullOrWhiteSpace(enteredSerialNumber)) return;

            if (SerialNumberRegex().IsMatch(enteredSerialNumber))
                {
                SerialNumber = enteredSerialNumber;
                IsSerialNumberConfirmed = true;
                Log(CurrentSN, [SerialNumber]);
                Flash(SerialFeedback, true);
                RequestClearInputFocus?.Invoke(this, EventArgs.Empty);
                }
            else
                {
                IsSerialNumberConfirmed = false;
                Flash(SerialFeedback, false);
                _dialogService.ShowMessage($"{IncorrectFormatForSNErr}", $"{Err}");
                return;
                }

            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
                {
                try
                    {
                    _currentDevice = new DeviceUnderTest(SerialNumber) { Colorimeter = _colorimeter };
                    ResetMeasurementStatuses();
                    RequestSpecCheck();
                    }
                catch (ArgumentException ex)
                    {
                    IsSerialNumberConfirmed = false;
                    Log(ErrWithArg, [ex.Message], LogSeverity.Error);
                    _dialogService.ShowMessage(ex.Message, $"{Err} SN");
                    SerialNumber = "";
                    _currentDevice = null;
                    }
                }
            else
                {
                Log(SerialNumber, [AlreadyActivated]);
                }
            UpdateUIState();
            }

        /// <summary>
        /// Saves current results and prepares for a new device under test.
        /// </summary>
        private async Task ExecuteNewDeviceUnderTest(object parameter)
            {
            if (_isSavingDevice) return;

            _isSavingDevice = true;
            try
                {
                if (await ExecuteSaveResultsAsync(parameter))
                    ExecuteClearFields("CalledFromNewDeviceMethod");
                }
            finally
                {
                _isSavingDevice = false;
                }
            UpdateUIState();
            }

        /// <summary>
        /// Runs the report generator; calling it again while the generator runs cancels the run.
        /// </summary>
        private async Task ExecuteReportGenerateCommandAsync(object parameter)
            {
            if (IsReportGenerating)
                {
                _reportCts?.Cancel();
                return;
                }

            if (!_reportGenerationService.IsAvailable)
                {
                Log(RunExternalAppNotFoundErr, [_reportGenerationService.ExecutablePath], LogSeverity.Error);
                Flash(ReportFeedback, false);
                return;
                }

            using var cts = new CancellationTokenSource();
            _reportCts = cts;
            Log(StartingReportGeneration);
            IsReportGenerating = true;

            try
                {
                var result = await _reportGenerationService.GenerateAsync(cts.Token);
                HandleReportGenerationResult(result);
                }
            catch (Exception ex)
                {
                Log(ErrorInBackgroundTask, [ex.Message], LogSeverity.Error);
                }
            finally
                {
                _reportCts = null;
                ExecuteThreadInUI(() => IsReportGenerating = false);
                }
            }

        /// <summary>
        /// Uploads the generated reports using the upload service; calling it again while the upload runs cancels it.
        /// </summary>
        private async Task ExecuteUploadReportsAsync(object parameter)
            {
            if (IsUploadingReports)
                {
                _uploadService.CancelUpload();
                return;
                }

            Log(StartingReportUploading);
            IsUploadingReports = true;

            try
                {
                bool uploaded = await _uploadService.UploadReportsAsync();
                RefreshPendingReports();
                if (!uploaded)
                    {
                    Log(UploadFailed, severity: LogSeverity.Error);
                    }
                Flash(UploadFeedback, uploaded);
                }
            catch (OperationCanceledException)
                {
                Log(UploadCanceled, severity: LogSeverity.Warning);
                RefreshPendingReports();
                FlashCanceled(UploadFeedback);
                }
            finally
                {
                IsUploadingReports = false;
                }
            }

        #endregion

        #region Helpers

        private bool EnsureDeviceInitialized(string location, ref string message)
            {
            if (_currentDevice == null || _currentDevice.SerialNumber != SerialNumber)
                {
                if (string.IsNullOrWhiteSpace(SerialNumber))
                    {
                    _dialogService.ShowMessage($"{FillSN}", $"{Err}");
                    message = $"{NoSNErr}";
                    UpdateMeasurementStatus(location, false, message);
                    return false;
                    }

                _currentDevice = new DeviceUnderTest(SerialNumber) { Colorimeter = _colorimeter };
                Log(TestStartInfo, [_currentDevice.SerialNumber]);
                ResetMeasurementStatuses();
                UpdateMeasurementStatus(location, null, $"{Measuring}");
                }
            return true;
            }

        private void ProcessValidMeasurement(Measurement measurement, ref bool isSuccess, ref string message)
            {
            switch (_acceptance.Assess(measurement))
                {
                case AcceptanceResult.Accepted:
                    ApplyMeasurementResult(measurement, ref message);
                    isSuccess = true;
                    break;

                case AcceptanceResult.Rejected rejected:
                    _dialogService.ShowMessage($"{rejected.Reason} \n{PleaseTryToMeasureAgain}", $"{Warning}");
                    message = $"{FailedMeasurementForLocation}: {measurement.Location}.\n ({Attempt} {rejected.Attempt}). {PleaseTryToMeasureAgain}";
                    string reason = System.Text.RegularExpressions.Regex.Replace(rejected.Reason, @"\s+", " ").Trim();
                    Log($"{FailedMeasurementForLocation}: {LocalizedLocation(measurement.Location)} ({Attempt} {rejected.Attempt}). {reason}", severity: LogSeverity.Warning);
                    break;

                case AcceptanceResult.ConfirmationNeeded confirmation:
                    if (_dialogService.ShowQuestion($"{SaveNotCorrectResultQuestion}\n{confirmation.Reason}.\n", $"{Warning}"))
                        {
                        ApplyMeasurementResult(measurement, ref message);
                        message = $"{ResultsSaved} '{measurement.Location}'. \n {ValidationMessage}: {confirmation.Reason}";
                        isSuccess = true;
                        }
                    else
                        {
                        message = $"{SaveCanceled}";
                        }
                    Log(message);
                    break;
                }
            }

        private void ApplyMeasurementResult(Measurement measurement, ref string message)
            {
            var (x, y, Lv, T) = MeasurementFormatter.Format(measurement);
            message = $"x={x}, y={y}, Lv={Lv}, T={T}";
            _currentDevice!.RecordAttempt(measurement);
            _hasUnsavedMeasurements = true;
            }

        private void HandleReportGenerationResult(ReportGenerationResult result)
            {
            if (result.Status == ReportGenerationStatus.Canceled)
                {
                Log(ReportGenerationCanceled, severity: LogSeverity.Warning);
                FlashCanceled(ReportFeedback);
                return;
                }

            Flash(ReportFeedback, result.Status == ReportGenerationStatus.Success);
            ExecuteThreadInUI(RefreshPendingReports);
            var (message, severity) = result.Status switch
                {
                    ReportGenerationStatus.Success => ($"{ReportGeneratedSuccessfully}", LogSeverity.Info),
                    ReportGenerationStatus.NoDataFound => ($"{NoDataFoundForReportGenerator}", LogSeverity.Warning),
                    ReportGenerationStatus.ConfigError => ($"{InvalidReportConfiguration}", LogSeverity.Error),
                    _ => ($"{ReportGenerationFailed} ({Code}: {result.ExitCode})", LogSeverity.Error)
                    };
            Log(message, severity: severity);
            }

        /// <summary>
        /// Returns the name of a measurement point in the language of the interface (the identifier when it has no name).
        /// </summary>
        private static string LocalizedLocation(string location) =>
            Resources.Resources.ResourceManager.GetString($"SpecLocation_{location}") is { Length: > 0 } name ? name : location;

        private void Log(string messageOrKey, object[]? args = null, LogSeverity severity = LogSeverity.Info, [CallerArgumentExpression(nameof(messageOrKey))] string? resourceName = null) =>
            _logService.Write(messageOrKey, args, severity, resourceName);

        private void UpdateUIState()
            {
            IsMeasurementButtonsEnabled = IsDeviceConnected && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber);
            UpdateCommandsCanExecute();
            }

        private void UpdateCommandsCanExecute()
            {
            ( ZeroCalibrationCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( MeasureCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( SaveResultsCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( ApplySerialNumberCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( NewDeviceUnderTestCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( ReportGenerateCommand as RelayCommand )?.RaiseCanExecuteChanged();
            ( UploadReportsCommand as RelayCommand )?.RaiseCanExecuteChanged();
            }

        private void UpdateMeasurementStatus(string location, bool? isPassed, string measuredValuesString)
            {
            var status = _measurementStatusService.AllMeasurementButtonStatuses.FirstOrDefault(s => s.Location == location);
            if (status != null)
                {
                status.IsPassed = isPassed;
                status.MeasuredValuesString = measuredValuesString;
                if (isPassed is bool passed) Flash(status.Feedback, passed);
                }
            else
                {
                Log(MeasButStatusErr, [location], LogSeverity.Error);
                }
            }

        /// <summary>
        /// Starts the specification check of the current measurements; the previous request is abandoned.
        /// </summary>
        private void RequestSpecCheck() =>
            _specCheck.Request(new SpecCheckRequest(SelectedDeviceConfiguration, _currentDevice, PreviewSerialNumber, IsTvCheckboxChecked), ShowSpecCheck);

        /// <summary>
        /// Shows the outcome of a specification check: the device info panel, the attempts of the points and the log.
        /// </summary>
        private void ShowSpecCheck(SpecCheckResult result)
            {
            switch (result)
                {
                case SpecCheckResult.ConfigurationNeeded:
                    Spec.ShowMessage(ResourceManager.GetString("SpecSelectConfig")!);
                    break;

                case SpecCheckResult.Evaluated evaluated:
                    evaluated.Device.ApplyAttemptChoices(evaluated.Evaluation);
                    ShowAttempts(evaluated.Evaluation);
                    Spec.Show(evaluated.Evaluation, evaluated.MeasuredPoints, evaluated.TotalPoints);
                    break;

                case SpecCheckResult.Unavailable unavailable:
                    string message = string.Format(ResourceManager.GetString("SpecUnavailable")!, unavailable.Error);
                    Spec.ShowMessage(message);
                    if (unavailable.IsNew) _logService.Append(message, LogSeverity.Warning);
                    break;
                }
            }

        /// <summary>
        /// Refreshes the results table and shows the attempts counters of the points.
        /// </summary>
        private void ShowAttempts(SpecEvaluation evaluation)
            {
            RefreshResults();
            foreach (var row in Results)
                {
                bool isClean = !evaluation.Checks.Any(check => check.Status is SpecStatus.Warning or SpecStatus.Fail && check.Locations.Contains(row.Location));
                row.ShowAttempts(evaluation.Attempts.GetValueOrDefault(row.Location), isClean);
                }
            }

        private void LocalizationService_LanguageChanged(object? sender, EventArgs e)
            {
            OnPropertyChanged(nameof(SelectedDeviceConfigurationDisplayName));
            OnPropertyChanged(nameof(DeviceConfigurationCaptions));
            Spec.Refresh();
            RefreshResults();
            }

        /// <summary>
        /// Updates the results table from the measurements of the current device.
        /// </summary>
        private void RefreshResults()
            {
            foreach (var row in Results)
                {
                row.IsAvailable = row.Location != nameof(MeasurementLocation.WhiteColor) || IsTvCheckboxChecked;
                row.Show(_currentDevice?.Measurements.FirstOrDefault(m => m.Location == row.Location));
                row.RefreshName();
                }
            }

        private void ClearAttempts()
            {
            foreach (var row in Results) row.ShowAttempts(null);
            }

        private void ResetMeasurementStatuses()
            {
            foreach (var status in _measurementStatusService.AllMeasurementButtonStatuses)
                {
                status.IsPassed = null;
                status.MeasuredValuesString = "";
                status.SpecRemark = SpecStatus.Pass;
                status.SpecTooltip = "";
                }
            RefreshResults();
            ClearAttempts();
            }

        /// <summary>
        /// Signals the outcome of a button action to the view (from any thread).
        /// </summary>
        private void Flash(ActionFeedbackSignal signal, bool success) =>
            ExecuteThreadInUI(() =>
            {
                if (success) signal.Succeed();
                else signal.Fail();
            });

        /// <summary>
        /// Recounts the reports waiting for upload for the label of the Upload reports button.
        /// </summary>
        private void RefreshPendingReports()
            {
            _pendingReportsCount = _uploadService.GetPendingReportCount();
            OnPropertyChanged(nameof(PendingReportsSuffix));
            }

        /// <summary>
        /// Asks the operator to confirm closing the application when the measurements of the current device are not saved.
        /// </summary>
        /// <returns>True if the application may close.</returns>
        public bool ConfirmClose() =>
            !_hasUnsavedMeasurements
            || _dialogService.ShowQuestion(ResourceManager.GetString("CloseUnsavedQuestion")!, $"{Warning}");

        /// <summary>
        /// Signals that the operator canceled a button action (from any thread).
        /// </summary>
        private void FlashCanceled(ActionFeedbackSignal signal) => ExecuteThreadInUI(signal.Cancel);

        private void ExecuteThreadInUI(Action action)
            {
            if (_dispatcher.CheckAccess()) action.Invoke();
            else _dispatcher.BeginInvoke(action);
            }

        // CanExecute predicates
        private bool CanExecuteConnect() => !IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteDisconnect() => !_isDeviceCalibrating && !_isDeviceConnecting;
        private bool CanExecuteZeroCalibration(object parameter) => !_isDeviceConnecting && !_isDeviceCalibrating;
        private bool CanExecuteSaveResults(object parameter) => HasSelectedDeviceConfiguration && _currentDevice != null && !string.IsNullOrWhiteSpace(_currentDevice.SerialNumber) && _currentDevice.Measurements.Count > 0;
        private bool CanExecuteNewDeviceUnderTest(object parameter) => CanExecuteSaveResults(parameter);
        private bool CanExecuteClearFields(object parameter) => true;
        private bool CanExecuteSwitchLanguage(object parameter) => parameter is string code && !string.IsNullOrWhiteSpace(code);
        private bool CanExecuteMeasure(object parameter) => IsDeviceConnected && !_isDeviceCalibrating && !_isDeviceConnecting && IsDeviceCalibrated && !string.IsNullOrWhiteSpace(SerialNumber) && MeasurementTime > 0;
        private bool CanExecuteApplySerialNumber(object parameter) => !string.IsNullOrWhiteSpace(parameter as string);
        private bool CanExecuteReportGenerateCommand(object parameter) => true;
        private bool CanExecuteUploadReportsAsync(object parameter) => true;

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
            {
            Log(ViewModelClearing);
            _colorMeasurementService.StatusMessage -= ColorMeasurementService_StatusMessage;
            _fileService.StatusMessage -= FileService_StatusMessage;
            _colorMeasurementService.ConnectionStatusChanged -= ColorMeasurementService_ConnectionStatusChanged;
            _colorMeasurementService.CalibrationStatusChanged -= ColorMeasurementService_CalibrationStatusChanged;
            _colorMeasurementService.CurrentChannelChanged -= ColorMeasurementService_CurrentChannelChanged;
            _uploadService.StatusMessage -= UploadService_StatusMessage;
            _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            _specCheck.Cancel();

            ( _colorMeasurementService as IDisposable )?.Dispose();
            ( _fileService as IDisposable )?.Dispose();
            ( _dialogService as IDisposable )?.Dispose();

            _logService.Clear();
            _currentDevice = null;
            _serialNumber = string.Empty;
            _measurementTime = 2;
            _isDeviceConnected = false;
            _isDeviceCalibrated = false;
            _isSerialNumberConfirmed = false;
            _isMeasurementButtonsEnabled = false;

            GC.SuppressFinalize(this);
            Log(ViewModelCleared);
            }

        #endregion

        #region Event Handlers

        private void ColorMeasurementService_StatusMessage(object? sender, string message)
            {
            if (message == Measuring) return;
            ExecuteThreadInUI(() => _logService.Append(message, ServiceMessageSeverity(message)));
            }

        private void FileService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => _logService.Append(message, ServiceMessageSeverity(message)));

        private void UploadService_StatusMessage(object? sender, string message) =>
            ExecuteThreadInUI(() => _logService.Append(message, ServiceMessageSeverity(message)));

        /// <summary>
        /// Tells whether a message coming from a service reports a failure, by the localized prefix of its error texts.
        /// </summary>
        private static LogSeverity ServiceMessageSeverity(string message)
            {
            string[] errorPrefixes =
                [
                ConnectionError, CheckConnectionCA, ErrAtCalibration, ErrorAtMeasuringIteration, MeasureWihoutConnectionError,
                SaveJSONErrDeviceIsEmpty, SaveJSONErrForSN, WorkingFolderInitErr, CsvSnErr, CsvDataErr, CsvLocationErr, ErrCSV, UploadFailed
                ];
            return errorPrefixes.Any(prefix => !string.IsNullOrEmpty(prefix) && message.StartsWith(prefix, StringComparison.Ordinal))
                ? LogSeverity.Error
                : LogSeverity.Info;
            }

        private void ColorMeasurementService_CalibrationStatusChanged(object? sender, bool isCalibrated) =>
            ExecuteThreadInUI(() => { IsDeviceCalibrated = isCalibrated; UpdateUIState(); });

        private void ColorMeasurementService_ConnectionStatusChanged(object? sender, bool isConnected) =>
            ExecuteThreadInUI(() => { IsDeviceConnected = isConnected; UpdateUIState(); });

        private void ColorMeasurementService_CurrentChannelChanged(object? sender, int channel) =>
            ExecuteThreadInUI(() => OnPropertyChanged(nameof(DeviceActiveChannelStatusText)));

        #endregion

        [GeneratedRegex(SERIAL_NUMBER_PATTERN)]
        public static partial Regex SerialNumberRegex();
        }
    }
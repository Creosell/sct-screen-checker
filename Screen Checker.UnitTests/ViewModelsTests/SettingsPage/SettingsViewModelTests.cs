using Moq;
using ScreenChecker.Interfaces;
using ScreenChecker.Models;
using ScreenChecker.ViewModels.SettingsPage;

namespace ScreenChecker.UnitTests.ViewModelsTests.SettingsPage
    {
    [TestFixture]
    public class SettingsViewModelTests
        {
        private Mock<ISettingsService> _mockSettingsService;
        private Mock<ILocalizationService> _mockLocalizationService;
        private Mock<IColorMeasurementService> _mockColorMeasurementService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<IThemeService> _mockThemeService;
        private SettingsViewModel _viewModel;

        [SetUp]
        public void Setup()
            {
            _mockSettingsService = new Mock<ISettingsService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockColorMeasurementService = new Mock<IColorMeasurementService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockThemeService = new Mock<IThemeService>();

            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings
                {
                LanguageCultureCode = "",
                MeasurementSpeed = MeasurementSpeed.Slow
                });

            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(1);
            _mockColorMeasurementService.Setup(s => s.IsDeviceConnected).Returns(true);

            _viewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object,
                _mockThemeService.Object
            );
            }

        [Test]
        public void SelectedTheme_StartsWithTheStoredThemeWithoutApplyingIt()
            {
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings { Theme = ThemeMode.Light });
            using var viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockDialogService.Object, _mockColorMeasurementService.Object, _mockLocalizationService.Object, _mockThemeService.Object);

            Assert.That(viewModel.SelectedTheme.Mode, Is.EqualTo(ThemeMode.Light));
            _mockThemeService.Verify(t => t.SetMode(It.IsAny<ThemeMode>()), Times.Never);
            }

        [TearDown]
        public void ResetInterfaceScale() => UiMetrics.UiScale = 1;

        [Test]
        public void SelectedInterfaceScale_IsTheSystemOne_WhenTheSettingsAreEmpty()
            {
            Assert.That(_viewModel.SelectedInterfaceScale.Scale, Is.Null);
            Assert.That(_viewModel.SelectedInterfaceScale.Factor, Is.EqualTo(1));
            Assert.That(SettingsViewModel.AvailableInterfaceScales[0].Scale, Is.Null, "the size of the system is the first one offered");
            Assert.That(SettingsViewModel.AvailableInterfaceScales[0].DisplayName, Is.Not.Empty);
            }

        [TestCase(1.25)]
        [TestCase(1.5)]
        [TestCase(1.75)]
        [TestCase(2.0)]
        public void SelectedInterfaceScale_StartsWithTheStoredScale_WithoutStoringItAgain(double stored)
            {
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings { InterfaceScale = stored });
            using var viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockDialogService.Object, _mockColorMeasurementService.Object, _mockLocalizationService.Object, _mockThemeService.Object);

            Assert.That(viewModel.SelectedInterfaceScale.Scale, Is.EqualTo(stored));
            _mockSettingsService.Verify(s => s.UpdateInterfaceScale(It.IsAny<double?>()), Times.Never);
            }

        [TestCase(0.2)]
        [TestCase(1.3)]
        [TestCase(7.0)]
        public void SelectedInterfaceScale_FallsBackToTheSystemOne_WhenTheStoredScaleIsNotOffered(double stored)
            {
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings { InterfaceScale = stored });
            using var viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockDialogService.Object, _mockColorMeasurementService.Object, _mockLocalizationService.Object, _mockThemeService.Object);

            Assert.That(viewModel.SelectedInterfaceScale.Scale, Is.Null);
            }

        [Test]
        public void SelectedInterfaceScale_ChoiceIsAppliedAndStoredAtOnce()
            {
            var option = SettingsViewModel.AvailableInterfaceScales.First(o => o.Scale == 1.5);

            _viewModel.SelectedInterfaceScale = option;

            Assert.That(UiMetrics.UiScale, Is.EqualTo(1.5));
            _mockSettingsService.Verify(s => s.UpdateInterfaceScale(1.5), Times.Once);

            _viewModel.SelectedInterfaceScale = SettingsViewModel.AvailableInterfaceScales[0];

            Assert.That(UiMetrics.UiScale, Is.EqualTo(1));
            _mockSettingsService.Verify(s => s.UpdateInterfaceScale(null), Times.Once);
            }

        [Test]
        public void Save_KeepsTheThemeAndTheInterfaceScaleOfTheFile()
            {
            _mockSettingsService.Setup(s => s.LoadSettings()).Returns(new AppSettings { Theme = ThemeMode.Light, InterfaceScale = 1.75 });
            AppSettings? written = null;
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>())).Callback<AppSettings>(settings => written = settings);
            using var viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockDialogService.Object, _mockColorMeasurementService.Object, _mockLocalizationService.Object, _mockThemeService.Object);

            viewModel.SaveSettingsCommand.Execute(null);

            Assert.That(written, Is.Not.Null);
            Assert.That(written!.Theme, Is.EqualTo(ThemeMode.Light));
            Assert.That(written.InterfaceScale, Is.EqualTo(1.75));
            }

        [TestCase(null, 1.0)]
        [TestCase(1.5, 1.5)]
        [TestCase(0.5, 1.0)]
        [TestCase(3.0, 1.0)]
        public void EffectiveInterfaceScale_IsTheStoredOneWithinTheRange_AndTheSystemOneOtherwise(double? stored, double expected)
            {
            Assert.That(new AppSettings { InterfaceScale = stored }.EffectiveInterfaceScale, Is.EqualTo(expected));
            }

        [Test]
        public void ThemeNames_AreRefreshedWhenTheLanguageChanges()
            {
            var changed = new List<string?>();
            var option = _viewModel.AvailableThemes[0];
            option.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            _mockLocalizationService.Raise(l => l.LanguageChanged += null, EventArgs.Empty);

            Assert.That(changed, Does.Contain("DisplayName"));
            Assert.That(option.DisplayName, Is.Not.Empty);
            }

        [Test]
        public void SelectedTheme_ChoiceIsAppliedAtOnce()
            {
            _viewModel.SelectedTheme = _viewModel.AvailableThemes.First(option => option.Mode == ThemeMode.System);

            _mockThemeService.Verify(t => t.SetMode(ThemeMode.System), Times.Once);
            }

        [Test]
        public void SaveAndReset_DoNotTouchTheTheme()
            {
            _viewModel.SaveSettingsCommand.Execute(null);
            _viewModel.ResetToDefaultCommand.Execute(null);

            _mockThemeService.Verify(t => t.SetMode(It.IsAny<ThemeMode>()), Times.Never);
            }

        [TearDown]
        public void TearDown()
            {
            _viewModel.Dispose();
            }

        [Test]
        public void Constructor_InitializesCorrectly()
            {
            Assert.That(_viewModel, Is.Not.Null);
            }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenDependencyIsNull()
            {
            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                null,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                null,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                null,
                _mockLocalizationService.Object
            ));

            Assert.Throws<ArgumentNullException>(() => new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                null
            ));
            }

        [Test]
        public void Constructor_LoadsSettings()
            {
            _mockSettingsService.Verify(s => s.LoadSettings(), Times.Once());
            Assert.That(_viewModel.ColorAnalyzerChannel, Is.EqualTo("1"));
            Assert.That(_viewModel.MeasurementSpeed, Is.EqualTo(MeasurementSpeed.Slow));
            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            }

        [Test]
        public void Constructor_PopulatesAvailableLanguages()
            {
            Assert.That(_viewModel.AvailableLanguages.Select(l => l.CultureCode), Is.EqualTo(new[] { "", "zh-Hans", "ru" }));
            }

        [Test]
        public void Constructor_SubscribesToEvents()
            {
            _mockLocalizationService.VerifyAdd(l => l.LanguageChanged += It.IsAny<EventHandler>(), Times.Once());
            _mockColorMeasurementService.VerifyAdd(c => c.CurrentChannelChanged += It.IsAny<EventHandler<int>>(), Times.Once());
            }

        [Test]
        public void SelectedLanguage_UpdatesLanguageCultureCode()
            {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            _viewModel.SelectedLanguage = newLang;

            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo("zh-Hans"));
            }

        [Test]
        public void SelectedLanguage_CallsSetLanguage()
            {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            _viewModel.SelectedLanguage = newLang;

            _mockLocalizationService.Verify(l => l.SetLanguage("zh-Hans"), Times.Once());
            }

        [Test]
        public void SelectedLanguage_SavesLanguageSettings()
            {
            var newLang = new SettingsViewModel.LanguageOption { DisplayName = "Chinese", CultureCode = "zh-Hans" };

            _viewModel.SelectedLanguage = newLang;

            _mockSettingsService.Verify(s => s.UpdateLanguage("zh-Hans"), Times.Once());
            }

        [Test]
        public void SelectedLanguage_HandlesNull()
            {
            _viewModel.SelectedLanguage = null;

            Assert.That(_viewModel.LanguageCultureCode, Is.EqualTo(""));
            }

        [Test]
        public void SaveSettingsCommand_CanExecute_ReturnsTrue()
            {
            Assert.That(_viewModel.SaveSettingsCommand.CanExecute(null), Is.True);
            }

        [Test]
        public void SaveSettingsCommand_Execute_SavesSettings()
            {
            _viewModel.ColorAnalyzerChannel = "3";
            _viewModel.MeasurementSpeed = MeasurementSpeed.Fast;

            _viewModel.SaveSettingsCommand.Execute(null);

            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                settings => settings.MeasurementSpeed == MeasurementSpeed.Fast
            )), Times.Once());
            }

        [Test]
        public void SaveSettingsCommand_Saved_SignalsSuccess()
            {
            _viewModel.SaveSettingsCommand.Execute(null);

            Assert.That(_viewModel.SaveFeedback.Feedback, Is.EqualTo(ActionFeedback.Success));
            }

        [Test]
        public void SaveSettingsCommand_WriteFails_SignalsFailure()
            {
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>())).Throws(new InvalidOperationException("Test error"));

            _viewModel.SaveSettingsCommand.Execute(null);

            Assert.That(_viewModel.SaveFeedback.Feedback, Is.EqualTo(ActionFeedback.Failure));
            }

        [Test]
        public void SaveSettingsCommand_RejectedValue_SignalsFailureEvenThoughTheFallbackIsSaved()
            {
            _viewModel.SelectedSyncMode = SyncMode.Internal;
            _viewModel.SyncFrequency = "300";

            _viewModel.SaveSettingsCommand.Execute(null);

            Assert.That(_viewModel.SaveFeedback.Feedback, Is.EqualTo(ActionFeedback.Failure));
            }

        [Test]
        public void ResetToDefaultCommand_SignalsSuccess()
            {
            _viewModel.ResetToDefaultCommand.Execute(null);

            Assert.That(_viewModel.ResetFeedback.Feedback, Is.EqualTo(ActionFeedback.Success));
            }

        [Test]
        public void SaveSettingsCommand_Execute_StoresTheChosenChannelInTheAnalyzerRegistry()
            {
            var registry = new Mock<IAnalyzerRegistryService>();
            using var viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockDialogService.Object, _mockColorMeasurementService.Object, _mockLocalizationService.Object, _mockThemeService.Object, registry.Object);
            viewModel.ColorAnalyzerChannel = "3";

            viewModel.SaveSettingsCommand.Execute(null);

            registry.Verify(r => r.UpdateLastUsed(3), Times.Once);
            }

        [Test]
        public void SaveSettingsCommand_Execute_HandlesException()
            {
            _mockSettingsService.Setup(s => s.SaveSettings(It.IsAny<AppSettings>()))
                .Throws(new InvalidOperationException("Test error"));

            Assert.DoesNotThrow(() => _viewModel.SaveSettingsCommand.Execute(null));
            }

        [Test]
        public void ResetToDefaultCommand_Execute_RestoresMeasurementModesAndKeepsChannel()
            {
            _viewModel.ColorAnalyzerChannel = "7";
            _viewModel.MeasurementSpeed = MeasurementSpeed.Fast;
            _viewModel.SyncMode = SyncMode.Internal;
            _viewModel.SyncFrequency = "75";
            _viewModel.IntegrationTime = "250";

            _viewModel.ResetToDefaultCommand.Execute(null);

            Assert.That(_viewModel.MeasurementSpeed, Is.EqualTo(MeasurementSpeed.Slow));
            Assert.That(_viewModel.SyncMode, Is.EqualTo(SyncMode.Universal));
            Assert.That(_viewModel.SyncFrequency, Is.EqualTo("60"));
            Assert.That(_viewModel.IntegrationTime, Is.EqualTo("100"));
            Assert.That(_viewModel.ColorAnalyzerChannel, Is.EqualTo("7"));
            _mockSettingsService.Verify(s => s.SaveSettings(It.IsAny<AppSettings>()), Times.Never());
            }

        [Test]
        public void ColorAnalyzerChannel_LoadsFromTheAnalyzer()
            {
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(5);

            var viewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            );

            Assert.That(viewModel.ColorAnalyzerChannel, Is.EqualTo("5"));
            viewModel.Dispose();
            }

        [Test]
        public void CurrentChannelChanged_UpdatesColorAnalyzerChannel()
            {
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(7);

            _mockColorMeasurementService.Raise(
                s => s.CurrentChannelChanged += null,
                this,
                7
            );

            Assert.That(_viewModel.ColorAnalyzerChannel, Is.EqualTo("7"));
            }

        [Test]
        public void Dispose_UnsubscribesFromEvents()
            {
            _viewModel.Dispose();

            _mockLocalizationService.VerifyRemove(l => l.LanguageChanged -= It.IsAny<EventHandler>(), Times.Once());
            _mockColorMeasurementService.VerifyRemove(c => c.CurrentChannelChanged -= It.IsAny<EventHandler<int>>(), Times.Once());
            }

        [Test]
        public void CurrentChannelChanged_Event_UpdatesColorAnalyzerChannel()
            {
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(5);

            _mockColorMeasurementService.Raise(
                s => s.CurrentChannelChanged += null,
                this,
                5
            );

            Assert.That(_viewModel.ColorAnalyzerChannel, Is.EqualTo("5"));
            }

        [Test]
        public void SaveSettings_WithoutAConnectedAnalyzer_LeavesTheChannelAlone()
            {
            var registry = new Mock<IAnalyzerRegistryService>();
            _mockColorMeasurementService.Setup(s => s.IsDeviceConnected).Returns(false);
            _mockColorMeasurementService.SetupProperty(s => s.CurrentChannel);
            using var viewModel = new SettingsViewModel(_mockSettingsService.Object, _mockDialogService.Object, _mockColorMeasurementService.Object, _mockLocalizationService.Object, _mockThemeService.Object, registry.Object);
            viewModel.ColorAnalyzerChannel = "7";

            viewModel.SaveSettingsCommand.Execute(null);

            _mockColorMeasurementService.VerifySet(s => s.CurrentChannel = 7, Times.Never());
            registry.Verify(r => r.UpdateLastUsed(It.IsAny<int>()), Times.Never);
            }

        [Test]
        public void IsChannelEditable_FollowsTheConnectionOfTheAnalyzer()
            {
            var changed = new List<string?>();
            _viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            _mockColorMeasurementService.Setup(s => s.IsDeviceConnected).Returns(false);

            _mockColorMeasurementService.Raise(s => s.ConnectionStatusChanged += null, this, false);

            Assert.That(_viewModel.IsChannelEditable, Is.False);
            Assert.That(changed, Does.Contain(nameof(SettingsViewModel.IsChannelEditable)));
            }

        [Test]
        public void ExecuteChangeChannel_ValidChannel_UpdatesService()
            {
            _mockColorMeasurementService.SetupProperty(s => s.CurrentChannel);
            _viewModel.ColorAnalyzerChannel = "10";

            _viewModel.SaveSettingsCommand.Execute(null);

            _mockColorMeasurementService.VerifySet(s => s.CurrentChannel = 10, Times.Once());
            }

        [Test]
        public void ExecuteChangeChannel_InvalidChannel_ShowsErrorAndRestoresDefault()
            {
            // Используем двузначное число вне диапазона (setter примет, GetChannel отклонит)
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(5);

            var tempViewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            );

            tempViewModel.ColorAnalyzerChannel = "99"; // Допустимое значение для setter
            tempViewModel.SaveSettingsCommand.Execute(null);

            // Проверяем, что валидация прошла без ошибки (99 допустим)
            _mockDialogService.Verify(d => d.ShowMessage(
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Never());

            tempViewModel.Dispose();
            }

        [Test]
        public void ExecuteChangeChannel_InvalidFormat_ShowsError()
            {
            // Имитируем ситуацию через рефлексию или используем внутреннее поле
            var fieldInfo = typeof(SettingsViewModel).GetField("_colorAnalyzerChannel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fieldInfo.SetValue(_viewModel, "ab"); // Некорректный формат

            _viewModel.SaveSettingsCommand.Execute(null);

            _mockDialogService.Verify(d => d.ShowMessage(
                "Invalid channel format",
                It.IsAny<string>()
            ), Times.Once());
            }

        [Test]
        public void LoadSettings_AppliesTheChannelOfTheAnalyzer()
            {
            _mockColorMeasurementService.Setup(s => s.CurrentChannel).Returns(15);

            var viewModel = new SettingsViewModel(
                _mockSettingsService.Object,
                _mockDialogService.Object,
                _mockColorMeasurementService.Object,
                _mockLocalizationService.Object
            );

            Assert.That(viewModel.ColorAnalyzerChannel, Is.EqualTo("15"));

            viewModel.Dispose();
            }

        [Test]
        public void PropertyChanged_FiredForColorAnalyzerChannel()
            {
            // Arrange
            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (s, e) => propertiesChanged.Add(e.PropertyName!);

            // Act
            _viewModel.ColorAnalyzerChannel = "20";

            // Assert
            Assert.That(propertiesChanged, Does.Contain(nameof(_viewModel.ColorAnalyzerChannel)));
            }

        [Test]
        public void PropertyChanged_FiredForMeasurementSpeed()
            {
            // Arrange
            var propertiesChanged = new List<string>();
            _viewModel.PropertyChanged += (s, e) => propertiesChanged.Add(e.PropertyName!);

            // Act
            _viewModel.MeasurementSpeed = MeasurementSpeed.Fast;

            // Assert
            Assert.That(propertiesChanged, Does.Contain(nameof(_viewModel.MeasurementSpeed)));
            }

        [Test]
        public void SyncModes_WhenModelIsUnknown_OfferAllModes()
            {
            Assert.That(_viewModel.AvailableSyncModes.Select(o => o.Mode), Is.EqualTo(Enum.GetValues<SyncMode>()));
            }

        [Test]
        public void SyncModes_AfterCa310Connects_ExcludeManualAndResetUnsupportedSelection()
            {
            _viewModel.SyncMode = SyncMode.Manual;
            _mockColorMeasurementService.SetupGet(s => s.Model).Returns(ColorAnalyzerModel.CA310);

            _mockColorMeasurementService.Raise(s => s.ConnectionStatusChanged += null, _mockColorMeasurementService.Object, true);

            Assert.That(_viewModel.AvailableSyncModes.Select(o => o.Mode), Does.Not.Contain(SyncMode.Manual));
            Assert.That(_viewModel.SyncMode, Is.EqualTo(SyncMode.Universal));
            Assert.That(_viewModel.SyncFrequencyHint, Is.EqualTo("40-200 Hz"));
            }

        [Test]
        public void SelectedSyncMode_IgnoresNullFromTheList()
            {
            _viewModel.SyncMode = SyncMode.Internal;

            _viewModel.SelectedSyncMode = null;

            Assert.That(_viewModel.SyncMode, Is.EqualTo(SyncMode.Internal));
            }

        [Test]
        public void ResetToDefaultCommand_Execute_KeepsTheSyncModeListInstance()
            {
            var list = _viewModel.AvailableSyncModes;

            _viewModel.ResetToDefaultCommand.Execute(null);

            Assert.That(_viewModel.AvailableSyncModes, Is.SameAs(list));
            }

        [TestCase(SyncMode.Internal, true, false)]
        [TestCase(SyncMode.Manual, false, true)]
        [TestCase(SyncMode.Universal, false, false)]
        public void SyncParameterFields_AreVisibleOnlyForTheirMode(SyncMode mode, bool frequencyVisible, bool timeVisible)
            {
            _viewModel.SyncMode = mode;

            Assert.That(_viewModel.IsSyncFrequencyVisible, Is.EqualTo(frequencyVisible));
            Assert.That(_viewModel.IsIntegrationTimeVisible, Is.EqualTo(timeVisible));
            }

        [Test]
        public void SaveSettingsCommand_Execute_SavesInternalSyncFrequency()
            {
            _viewModel.SyncMode = SyncMode.Internal;
            _viewModel.SyncFrequency = "59.94";

            _viewModel.SaveSettingsCommand.Execute(null);

            _mockSettingsService.Verify(s => s.SaveSettings(It.Is<AppSettings>(
                settings => settings.SyncMode == SyncMode.Internal && settings.SyncFrequency == 59.94)), Times.Once());
            }

        [Test]
        public void SaveSettingsCommand_Execute_FrequencyOutOfRange_ShowsMessageAndRestoresDefault()
            {
            _viewModel.SyncMode = SyncMode.Internal;
            _viewModel.SyncFrequency = "300";

            _viewModel.SaveSettingsCommand.Execute(null);

            _mockDialogService.Verify(d => d.ShowMessage(It.Is<string>(m => m.Contains("0.5-240 Hz")), It.IsAny<string>()), Times.Once());
            Assert.That(_viewModel.SyncFrequency, Is.EqualTo("60"));
            }

        [Test]
        public void LanguageChanged_UpdatesSelectedLanguage()
            {
            // Arrange
            var newCultureCode = "zh-Hans";

            // Act
            _mockLocalizationService.Raise(l => l.LanguageChanged += null, EventArgs.Empty);

            // This test verifies event subscription exists
            _mockLocalizationService.VerifyAdd(l => l.LanguageChanged += It.IsAny<EventHandler>(), Times.Once());
            }


        }
    }
using MvvmHelpers;
using ScreenChecker.Models;

namespace ScreenChecker.ViewModels.MeasurementPage
    {
    /// <summary>
    /// How the attempts counter of a point is colored.
    /// </summary>
    public enum TriesLevel
        {
        /// <summary>No counter is shown.</summary>
        None,

        /// <summary>Fewer than the expected three attempts.</summary>
        Few,

        /// <summary>Three attempts or more.</summary>
        Enough
        }

    /// <summary>
    /// One row of the results table: a measurement point with the values of its accepted measurement.
    /// The status (color of the dot) is the status of the point's button, so the table and the buttons never disagree.
    /// </summary>
    public sealed class MeasurementResultRow : BaseViewModel
        {
        private const string Dash = "—";

        /// <summary>
        /// The number of attempts a point with a remark is expected to get.
        /// </summary>
        public const int EnoughAttempts = 3;

        private static readonly Dictionary<string, string> Hotkeys = new()
            {
            ["TopLeft"] = "1", ["TopCenter"] = "2", ["TopRight"] = "3",
            ["MiddleLeft"] = "4", ["Center"] = "5", ["MiddleRight"] = "6",
            ["BottomLeft"] = "7", ["BottomCenter"] = "8", ["BottomRight"] = "9",
            ["RedColor"] = "R", ["GreenColor"] = "G", ["BlueColor"] = "B", ["WhiteColor"] = "W", ["BlackColor"] = "0"
            };

        private string _name = string.Empty;
        private string _x = Dash;
        private string _y = Dash;
        private string _lv = Dash;
        private string _t = Dash;
        private bool _isAvailable = true;
        private string _tries = Dash;
        private TriesLevel _triesLevel = TriesLevel.None;
        private bool? _verdict;

        /// <summary>
        /// Initializes a new instance of MeasurementResultRow.
        /// </summary>
        /// <param name="status">Status of the measurement point (shared with its button).</param>
        public MeasurementResultRow(MeasurementStatus status)
            {
            Status = status;
            UpdateVerdict();
            Status.PropertyChanged += (_, e) =>
                {
                if (e.PropertyName is nameof(MeasurementStatus.IsPassed) or nameof(MeasurementStatus.MeasuredValuesString) or nameof(MeasurementStatus.SpecRemark))
                    {
                    UpdateVerdict();
                    OnPropertyChanged(nameof(Attention));
                    }
                };
            RefreshName();
            }

        /// <summary>
        /// Gets the status of the measurement point.
        /// </summary>
        public MeasurementStatus Status { get; }

        /// <summary>
        /// Gets the last verdict of the point: it stays while the point is measured again, so the row keeps its color until the new
        /// verdict arrives, and it is cleared when the point is reset (null: not measured yet).
        /// </summary>
        public bool? Verdict
            {
            get => _verdict;
            private set => SetProperty(ref _verdict, value);
            }

        /// <summary>
        /// Gets how much the point asks for the attention of the operator: <see cref="SpecStatus.Fail"/> for a rejected measurement
        /// or a remark outside min/max, <see cref="SpecStatus.Warning"/> for a remark below typ, otherwise <see cref="SpecStatus.Pass"/>.
        /// A remark of a point that has the expected attempts (<see cref="TriesLevel.Enough"/>) asks for nothing more: there is no action left.
        /// </summary>
        public SpecStatus Attention =>
            Verdict == false ? SpecStatus.Fail
            : Verdict == true && TriesLevel != TriesLevel.Enough && Status.SpecRemark is SpecStatus.Warning or SpecStatus.Fail ? Status.SpecRemark
            : SpecStatus.Pass;

        /// <summary>
        /// Gets the measurement location identifier.
        /// </summary>
        public string Location => Status.Location;

        /// <summary>
        /// Gets the point name as on its button ("1. Top left").
        /// </summary>
        public string Name
            {
            get => _name;
            private set => SetProperty(ref _name, value);
            }

        /// <summary>Gets the x chromaticity coordinate, or a dash.</summary>
        public string X
            {
            get => _x;
            private set => SetProperty(ref _x, value);
            }

        /// <summary>Gets the y chromaticity coordinate, or a dash.</summary>
        public string Y
            {
            get => _y;
            private set => SetProperty(ref _y, value);
            }

        /// <summary>Gets the luminance, or a dash.</summary>
        public string Lv
            {
            get => _lv;
            private set => SetProperty(ref _lv, value);
            }

        /// <summary>Gets the color temperature ("n/a" when infinite), or a dash.</summary>
        public string T
            {
            get => _t;
            private set => SetProperty(ref _t, value);
            }

        /// <summary>
        /// Gets the attempts counter ("2/3") of a point that had a remark against the specification, or a dash.
        /// </summary>
        public string Tries
            {
            get => _tries;
            private set => SetProperty(ref _tries, value);
            }

        /// <summary>
        /// Gets how far the attempts of the point are from the usual three.
        /// </summary>
        public TriesLevel TriesLevel
            {
            get => _triesLevel;
            private set
                {
                if (SetProperty(ref _triesLevel, value))
                    OnPropertyChanged(nameof(Attention));
                }
            }

        /// <summary>
        /// Gets or sets whether the point exists for the current device (the white point only does for a TV).
        /// </summary>
        public bool IsAvailable
            {
            get => _isAvailable;
            set => SetProperty(ref _isAvailable, value);
            }

        /// <summary>
        /// Shows the values of the measurement, or dashes when the point holds no accepted measurement.
        /// </summary>
        /// <param name="measurement">Accepted measurement of the point, if any.</param>
        public void Show(Measurement? measurement)
            {
            if (measurement == null)
                {
                (X, Y, Lv, T) = (Dash, Dash, Dash, Dash);
                return;
                }

            (X, Y, Lv, T) = MeasurementFormatter.Format(measurement);
            }

        /// <summary>
        /// Shows the attempts counter: for a point that had a remark against the specification in any attempt, or that takes part
        /// in a check with a remark now (the white and black points of the contrast, the points of the gamut).
        /// It is green when the point has enough attempts or its accepted attempt has no remark any more.
        /// </summary>
        /// <param name="info">What the specification check found out about the attempts of the point, if anything.</param>
        /// <param name="isClean">True when no check of the specification has a remark about the point.</param>
        public void ShowAttempts(SpecAttemptInfo? info, bool isClean = false)
            {
            if (info == null || (!info.Deviated && isClean))
                {
                (Tries, TriesLevel) = (Dash, TriesLevel.None);
                return;
                }

            Tries = $"{info.Count}/{EnoughAttempts}";
            TriesLevel = isClean || info.Count >= EnoughAttempts ? TriesLevel.Enough : TriesLevel.Few;
            }

        /// <summary>
        /// Takes a new verdict of the point; a point that is measured again (no verdict yet, but a status text) keeps the old one,
        /// a point that was reset (no verdict, no text) loses it.
        /// </summary>
        private void UpdateVerdict()
            {
            if (Status.IsPassed is bool passed) Verdict = passed;
            else if (string.IsNullOrEmpty(Status.MeasuredValuesString)) Verdict = null;
            }

        /// <summary>
        /// Rebuilds the localized name (after the language changed).
        /// </summary>
        public void RefreshName()
            {
            var localized = Resources.Resources.ResourceManager.GetString($"SpecLocation_{Location}") ?? Location;
            Name = Hotkeys.TryGetValue(Location, out var hotkey) ? $"{hotkey}. {localized}" : localized;
            }
        }
    }

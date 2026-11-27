using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenChecker.Views
    {
    /// <summary>
    /// Lays its content out at least at a design size and, when the room is smaller than that, shrinks the whole content in proportion
    /// to fit it, centered. The content is never enlarged: with room enough it fills the room as it does without this container.
    /// A display scale of 200% or 300% leaves a full HD screen with a window area of 960 x 520 pixels of the layout or less, which the
    /// pages are not made for: the layout stays as designed and only its size on the screen changes, so nothing is cut, overlaps or leaves the screen.
    /// </summary>
    public sealed class MinimumSizeViewbox : Decorator
        {
        /// <summary>Identifies <see cref="MinContentWidth"/>.</summary>
        public static readonly DependencyProperty MinContentWidthProperty = DependencyProperty.Register(
            nameof(MinContentWidth), typeof(double), typeof(MinimumSizeViewbox), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>Identifies <see cref="MinContentHeight"/>.</summary>
        public static readonly DependencyProperty MinContentHeightProperty = DependencyProperty.Register(
            nameof(MinContentHeight), typeof(double), typeof(MinimumSizeViewbox), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

        private readonly MatrixTransform _transform = new();

        /// <summary>
        /// Initializes the container: the content that is shrunk must not be drawn beyond it.
        /// </summary>
        public MinimumSizeViewbox() => ClipToBounds = true;

        /// <summary>Gets or sets the width the content is laid out at least at.</summary>
        public double MinContentWidth
            {
            get => (double)GetValue(MinContentWidthProperty);
            set => SetValue(MinContentWidthProperty, value);
            }

        /// <summary>Gets or sets the height the content is laid out at least at.</summary>
        public double MinContentHeight
            {
            get => (double)GetValue(MinContentHeightProperty);
            set => SetValue(MinContentHeightProperty, value);
            }

        /// <summary>
        /// Gets the factor the content is shrunk by now (1: not shrunk).
        /// </summary>
        public double Scale { get; private set; } = 1;

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
            {
            if (Child == null) return default;

            Child.Measure(ContentSize(constraint));
            return new Size(
                double.IsInfinity(constraint.Width) ? Child.DesiredSize.Width : constraint.Width,
                double.IsInfinity(constraint.Height) ? Child.DesiredSize.Height : constraint.Height);
            }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size arrangeSize)
            {
            if (Child == null) return arrangeSize;

            var content = ContentSize(arrangeSize);
            Scale = Math.Min(1, Math.Min(arrangeSize.Width / content.Width, arrangeSize.Height / content.Height));
            _transform.Matrix = new Matrix(
                Scale, 0, 0, Scale,
                (arrangeSize.Width - (content.Width * Scale)) / 2,
                (arrangeSize.Height - (content.Height * Scale)) / 2);
            Child.RenderTransform = _transform;
            Child.Arrange(new Rect(content));
            return arrangeSize;
            }

        private Size ContentSize(Size room) => new(Math.Max(room.Width, MinContentWidth), Math.Max(room.Height, MinContentHeight));
        }
    }

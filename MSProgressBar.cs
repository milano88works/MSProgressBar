using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace milano88.UI.Controls
{
    [DefaultEvent("ProgressChanged")]

    public class MSProgressBar : Control
    {
        private BufferedGraphics _bufGraphics;

        public MSProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(300, 30);
            ForeColor = Color.Black;
            BackColor = Color.Transparent;
            Font = new Font("Segoe UI", 9f);
            UpdateGraphicsBuffer();
        }

        #region Virtual and Overridden
        [Description("Occurs when the progress property has changed and the control has invalidated")]
        public event EventHandler ProgressChanged;

        [Description("Occurs when progress reaches 100%")]
        public event EventHandler ProgressCompleted;

        private void UpdateGraphicsBuffer()
        {
            if (Width > 0 && Height > 0)
            {
                BufferedGraphicsContext context = BufferedGraphicsManager.Current;
                context.MaximumBuffer = new Size(Width + 1, Height + 1);
                _bufGraphics = context.Allocate(CreateGraphics(), ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            DrawProgress(_bufGraphics.Graphics);
            DrawCenterElement(_bufGraphics.Graphics);
            _bufGraphics.Render(e.Graphics);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (Parent != null && BackColor == Color.Transparent)
            {
                Rectangle rect = new Rectangle(Left, Top, Width, Height);
                _bufGraphics.Graphics.TranslateTransform(-rect.X, -rect.Y);
                try
                {
                    using (PaintEventArgs pea = new PaintEventArgs(_bufGraphics.Graphics, rect))
                    {
                        pea.Graphics.SetClip(rect);
                        InvokePaintBackground(Parent, pea);
                        InvokePaint(Parent, pea);
                    }
                }
                finally
                {
                    _bufGraphics.Graphics.TranslateTransform(rect.X, rect.Y);
                }
            }
            else
            {
                using (SolidBrush backColor = new SolidBrush(this.BackColor))
                    _bufGraphics.Graphics.FillRectangle(backColor, ClientRectangle);
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
			base.OnSizeChanged(e);

			if (_borderRadius > 0)
            {
                if (Width < Height || _vertical)
                {
                    if (_roundedCorners)
                        _borderRadius = Width / 2;

                    if (_borderRadius > Width / 2)
                        _borderRadius = Width / 2;
                }
                else
                {
                    if (_roundedCorners)
                        _borderRadius = Height / 2;

                    if (_borderRadius > Height / 2)
                        _borderRadius = Height / 2;
                }
            }

            UpdateGraphicsBuffer();
        }

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                this.Invalidate();
            }
        }

        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "White")]
        public override Color BackColor { get => base.BackColor; set => base.BackColor = value; }
        #endregion

        #region Drawing
        private GraphicsPath GetFigurePath(RectangleF rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float curveSize = radius * 2F;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, curveSize, curveSize, 180, 90);
            path.AddArc(rect.Right - curveSize, rect.Y, curveSize, curveSize, 270, 90);
            path.AddArc(rect.Right - curveSize, rect.Bottom - curveSize, curveSize, curveSize, 0, 90);
            path.AddArc(rect.X, rect.Bottom - curveSize, curveSize, curveSize, 90, 90);
            path.CloseFigure();
            return path;
        }

        void DrawProgress(Graphics graphics)
        {
            if (!_vertical)
            {
                int pixelPercent = Convert.ToInt32(Width * (_value / _maximum));

                if (_borderRadius == 0)
                {
                    Region = new Region(ClientRectangle);
                    graphics.SmoothingMode = SmoothingMode.None;
                    using (SolidBrush brushProgressBack = new SolidBrush(_progressBackColor))
                        graphics.FillRectangle(brushProgressBack, 0, 0, Width, Height);

                    if (pixelPercent > 0)
                        using (LinearGradientBrush _progressFillBrush = new LinearGradientBrush(ClientRectangle, _progressColor1, _progressColor2, LinearGradientMode.Horizontal))
                            graphics.FillRectangle(_progressFillBrush, 0, 0, pixelPercent, Height);

                    if (_showBorder)
                        using (Pen penBorder = new Pen(_borderColor, 1F) { Alignment = PenAlignment.Inset })
                            graphics.DrawRectangle(penBorder, 0F, 0F, Width - 0.5F, Height - 0.5F);
                }
                else
                {
                    Rectangle rectSurface = ClientRectangle;
                    Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);

                    using (SolidBrush brushProgressBack = new SolidBrush(_progressBackColor))
                    using (GraphicsPath pathSurface = GetFigurePath(rectSurface, _borderRadius))
                    using (GraphicsPath pathBorder = GetFigurePath(rectBorder, _borderRadius - 1))
                    using (LinearGradientBrush brushPercent = new LinearGradientBrush(ClientRectangle, _progressColor1, _progressColor2, LinearGradientMode.Horizontal))
                    using (Pen penSurface = new Pen(Parent.BackColor, 1F))
                    using (Pen penBorder = new Pen(_borderColor, 1F))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        Region = new Region(pathSurface);
                        graphics.FillPath(brushProgressBack, pathSurface);
                        graphics.DrawPath(penSurface, pathSurface);

                        if (pixelPercent >= 1)
                        {
                            RectangleF rectPercent = new RectangleF(0.5F, 0.5F, pixelPercent - 1, Height - 1);
                            using (GraphicsPath pathPercent = GetFigurePath(rectPercent, _borderRadius))
                                graphics.FillPath(brushPercent, pathPercent);
                        }

                        if (_showBorder)
                            graphics.DrawPath(penBorder, pathBorder);
                    }
                }
            }
            else
            {
                int pixelPercent = Convert.ToInt32(Height * (_value / _maximum));

                if (_borderRadius == 0)
                {
                    Region = new Region(this.ClientRectangle);
                    graphics.SmoothingMode = SmoothingMode.None;
                    using (SolidBrush brushProgressBack = new SolidBrush(_progressBackColor))
                        graphics.FillRectangle(brushProgressBack, 0, 0, Width, Height);

                    if (pixelPercent > 0)
                        using (LinearGradientBrush brushPercent = new LinearGradientBrush(ClientRectangle, _progressColor2, _progressColor1, LinearGradientMode.Vertical))
                            graphics.FillRectangle(brushPercent, 0, Height - pixelPercent, Width, pixelPercent);

                    if (_showBorder)
                        using (Pen penBorder = new Pen(_borderColor, 1F) { Alignment = PenAlignment.Inset })
                            graphics.DrawRectangle(penBorder, 0F, 0F, Width - 0.5F, Height - 0.5F);
                }
                else
                {
                    Rectangle rectSurface = ClientRectangle;
                    Rectangle rectBorder = Rectangle.Inflate(rectSurface, -1, -1);

                    using (SolidBrush brushProgressBack = new SolidBrush(_progressBackColor))
                    using (GraphicsPath pathSurface = GetFigurePath(rectSurface, _borderRadius))
                    using (GraphicsPath pathBorder = GetFigurePath(rectBorder, _borderRadius - 1))
                    using (LinearGradientBrush brushPercent = new LinearGradientBrush(ClientRectangle, _progressColor2, _progressColor1, LinearGradientMode.Vertical))
                    using (Pen penSurface = new Pen(Parent.BackColor, 1F))
                    using (Pen penBorder = new Pen(_borderColor, 1F))
                    {
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        Region = new Region(pathSurface);
                        graphics.FillPath(brushProgressBack, pathSurface);
                        graphics.DrawPath(penSurface, pathSurface);

                        if (pixelPercent >= 1)
                        {
                            RectangleF rectPercent = new RectangleF(0.5F, Height - pixelPercent, Width - 1, pixelPercent - 0.5F);
                            using (GraphicsPath pathPercent = GetFigurePath(rectPercent, _borderRadius))
                                graphics.FillPath(brushPercent, pathPercent);
                        }

                        if (_showBorder)
                            graphics.DrawPath(penBorder, pathBorder);
                    }
                }
            }
        }

        public enum CenterElement { None, Text, Percent }
        void DrawCenterElement(Graphics graphics)
        {
            if (_centerElement == CenterElement.None) return;
            string text;
            if (_centerElement == CenterElement.Percent)
                text = ((int)(_value + 0.5f)).ToString(CultureInfo.InvariantCulture);
            else text = Text;
            TextFormatFlags format = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.GlyphOverhangPadding;
            TextRenderer.DrawText(graphics, text, Font, ClientRectangle, ForeColor, format);
        }
        #endregion

        #region Properties
        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "Black")]
        public override Color ForeColor
        {
            get { return base.ForeColor; }
            set
            {
                base.ForeColor = value;
                this.Invalidate();
            }
        }

        [Category("Custom Properties")]
        [DefaultValue(typeof(Font), "Segoe UI, 9pt")]
        public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                this.Invalidate();
            }
        }

        private Color _borderColor = Color.Black;
        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "Black")]
        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;
                this.Invalidate();
            }
        }

        private bool _showBorder = false;
        [Category("Custom Properties")]
        [DefaultValue(typeof(bool), "False")]
        public bool ShowBorder
        {
            get { return _showBorder; }
            set
            {
                _showBorder = value;
                this.Invalidate();
            }
        }

        private int _borderRadius = 0;
        private int _currentRadius = 0;
        [Category("Custom Properties")]
        [DefaultValue(0)]
        public int BorderRadius
        {
            get { return _borderRadius; }
            set
            {
                _roundedCorners = false;
                _borderRadius = value < 0 ? 0 : value > Height / 2 ? Height / 2 : value;
                _currentRadius = _borderRadius;
                Invalidate();
            }
        }

        private bool _roundedCorners;
        [Category("Custom Properties")]
        [DefaultValue(typeof(bool), "False")]
        public bool RoundedCorners
        {
            get { return _roundedCorners; }
            set
            {
                if (value)
                {
                    _roundedCorners = value;
                    _borderRadius = Height / 2;
                }
                else
                {
                    _roundedCorners = value;
                    _borderRadius = _currentRadius;
                }
                Invalidate();
            }
        }

        private Color _progressBackColor = Color.White;
        private Color _progressColor1 = Color.LightSkyBlue;
        private Color _progressColor2 = Color.SteelBlue;

        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "White")]
        public Color ProgressBackColor
        {
            get { return _progressBackColor; }
            set
            {
                _progressBackColor = value;
                this.Invalidate();
            }
        }

        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "LightSkyBlue")]
        public Color ProgressColor1
        {
            get { return _progressColor1; }
            set
            {
                _progressColor1 = value;
                this.Invalidate();
            }
        }

        [Category("Custom Properties")]
        [DefaultValue(typeof(Color), "SteelBlue")]
        public Color ProgressColor2
        {
            get { return _progressColor2; }
            set
            {
                _progressColor2 = value;
                this.Invalidate();
            }
        }

        CenterElement _centerElement;
        [Category("Custom Properties")]
        [DisplayName("Center Element")]
        [DefaultValue(CenterElement.None)]
        public CenterElement CenterText
        {
            get { return _centerElement; }
            set
            {
                _centerElement = value;
                this.Invalidate();
            }
        }

        private int _maximum = 100;
        [Category("Custom Properties")]
        [DefaultValue(100)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public int Maximum
        {
            get { return _maximum; }
            set
            {
                if (value <= _minimum)
                    throw new ArgumentOutOfRangeException("Value must be greater than Minimum");
                _maximum = value;
                if (_value > _maximum)
                    Value = _maximum;
                this.Invalidate();
            }
        }

        private int _minimum = 0;
        [Category("Custom Properties")]
        [DefaultValue(0)]
        [RefreshProperties(RefreshProperties.Repaint)]
        public int Minimum
        {
            get { return _minimum; }
            set
            {
                if (value >= _maximum)
                    throw new ArgumentOutOfRangeException("Value must be less than Maximum");
                _minimum = value;
                if (_value < _minimum)
                    Value = _minimum;
                this.Invalidate();
            }
        }

        private float _value = 0F;
        [Category("Custom Properties")]
        [DefaultValue(0)]
        public float Value
        {
            get { return _value; }
            set
            {
                if (value < _minimum || value > _maximum)
                    throw new ArgumentOutOfRangeException("value must be less than or equal to Maximum and greater than or equal to Minimum");
                if (value >= _maximum)
                {
                    _value = _maximum;
                    ProgressCompleted?.Invoke(this, EventArgs.Empty);
                }
                else if (value < 0) _value = 0;

                bool changed = value != _value;
                if (changed)
                {
                    _value = value;
                    ProgressChanged?.Invoke(this, EventArgs.Empty);
                }
                this.Invalidate();
            }
        }

        private bool _vertical = false;
        [Category("Custom Properties")]
        [DefaultValue(typeof(bool), "False")]
        public bool Vertical
        {
            get => _vertical;
            set
            {
                bool changed = _vertical != value;
                if (changed)
                {
                    _vertical = value;
                    this.Invalidate();
                }
            }
        }

        #endregion
    }
}


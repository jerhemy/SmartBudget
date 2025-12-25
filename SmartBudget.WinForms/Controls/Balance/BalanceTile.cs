using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Controls.Balance
{
    [DefaultEvent(nameof(Clicked))]
    public sealed partial class BalanceTile : UserControl
    {
        private decimal _balance;
        private string _title = "Total Balance";
        private string _subtitle = "Across all accounts";
        private DateTime? _asOf;
        private bool _hover;
        private bool _showSparkline = true;
        private decimal[] _sparkline = Array.Empty<decimal>();

        // Theme knobs
        private Color _cardBack1 = Color.FromArgb(24, 26, 32);
        private Color _cardBack2 = Color.FromArgb(18, 20, 25);
        private Color _borderColor = Color.FromArgb(50, 55, 70);
        private Color _textMuted = Color.FromArgb(170, 175, 190);

        private Color _positive = Color.FromArgb(50, 205, 120);
        private Color _negative = Color.FromArgb(235, 80, 80);
        private Color _neutral = Color.FromArgb(120, 170, 255);

        private int _cornerRadius = 18;

        public event EventHandler? Clicked;

        public BalanceTile()
        {
            InitializeComponent();

            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            Cursor = Cursors.Hand;
            Padding = new Padding(16);

            MinimumSize = new Size(240, 120);
            Size = new Size(340, 140);

            MouseEnter += (_, __) => { _hover = true; Invalidate(); };
            MouseLeave += (_, __) => { _hover = false; Invalidate(); };
            MouseUp += (_, __) => Clicked?.Invoke(this, EventArgs.Empty);
        }

        // --------------------------
        // Public API
        // --------------------------

        [Category("Balance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal Total
        {
            get => _balance;
            set { _balance = value; Invalidate(); }
        }

        [Category("Balance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title
        {
            get => _title;
            set { _title = value ?? ""; Invalidate(); }
        }

        [Category("Balance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value ?? ""; Invalidate(); }
        }

        [Category("Balance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime? AsOf
        {
            get => _asOf;
            set { _asOf = value; Invalidate(); }
        }

        [Category("Balance")]
        [Description("Optional values used to draw a sparkline in the background. Provide 12–24 points for best look.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal[] Sparkline
        {
            get => _sparkline;
            set { _sparkline = value ?? Array.Empty<decimal>(); Invalidate(); }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Category("Balance")]
        public bool ShowSparkline
        {
            get => _showSparkline;
            set { _showSparkline = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CardBack1
        {
            get => _cardBack1;
            set { _cardBack1 = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color CardBack2
        {
            get => _cardBack2;
            set { _cardBack2 = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TextMuted
        {
            get => _textMuted;
            set { _textMuted = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PositiveColor
        {
            get => _positive;
            set { _positive = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NegativeColor
        {
            get => _negative;
            set { _negative = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color NeutralColor
        {
            get => _neutral;
            set { _neutral = value; Invalidate(); }
        }

        // --------------------------
        // Paint
        // --------------------------

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = ClientRectangle;
            rect.Inflate(-1, -1);

            // Hover "lift"
            var lift = 0; // _hover ? -2 : 0;
            rect.Offset(0, lift);

            // Shadow
            DrawShadow(g, rect, _cornerRadius, _hover ? 22 : 18);

            // Card shape
            using var path = RoundedRect(rect, _cornerRadius);

            // Background gradient
            using (var brush = new LinearGradientBrush(rect, _cardBack1, _cardBack2, 45f))
                g.FillPath(brush, path);

            // Border
            using (var pen = new Pen(_borderColor, 1f))
                g.DrawPath(pen, path);

            // Accent color based on sign
            var accent = _balance > 0 ? _positive : _balance < 0 ? _negative : _neutral;

            // Left accent bar
            var accentRect = new Rectangle(rect.X + 10, rect.Y + 14, 4, rect.Height - 28);
            using (var accentPath = RoundedRect(accentRect, 6))
            using (var accentBrush = new SolidBrush(Color.FromArgb(220, accent)))
                g.FillPath(accentBrush, accentPath);

            // Sparkline (subtle, behind the value)
            if (_showSparkline && _sparkline.Length >= 4)
                DrawSparkline(g, rect, accent, _sparkline);

            // Layout text
            var inner = rect;
            inner.Inflate(-Padding.Left, -Padding.Top);

            // Title
            using (var titleFont = new Font(Font.FontFamily, 10f, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(235, ForeColor)))
            {
                g.DrawString(_title, titleFont, titleBrush, new PointF(inner.X + 10, inner.Y + 2));
            }

            // Subtitle / AsOf
            var subText = _subtitle;
            if (_asOf.HasValue)
                subText = $"{subText}  •  As of {_asOf.Value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)}";

            using (var subFont = new Font(Font.FontFamily, 8.5f, FontStyle.Regular))
            using (var subBrush = new SolidBrush(_textMuted))
            {
                g.DrawString(subText, subFont, subBrush, new PointF(inner.X + 10, inner.Y + 24));
            }

            // Balance big
            var balanceStr = _balance.ToString("C", CultureInfo.CurrentCulture);

            using (var balFont = new Font(Font.FontFamily, 22f, FontStyle.Bold))
            using (var balBrush = new SolidBrush(accent))
            {
                var y = inner.Y + 50;
                g.DrawString(balanceStr, balFont, balBrush, new PointF(inner.X + 10, y));
            }

            // Tiny delta label (optional, derived from last two sparkline points)
            if (_sparkline.Length >= 2)
            {
                var last = _sparkline[^1];
                var prev = _sparkline[^2];
                var delta = last - prev;

                var deltaColor = delta > 0 ? _positive : delta < 0 ? _negative : _textMuted;
                var deltaStr = delta == 0 ? "No change" : $"{(delta > 0 ? "▲" : "▼")} {Math.Abs(delta).ToString("C", CultureInfo.CurrentCulture)}";

                using var dFont = new Font(Font.FontFamily, 9f, FontStyle.Bold);
                using var dBrush = new SolidBrush(deltaColor);

                var size = g.MeasureString(deltaStr, dFont);
                g.DrawString(deltaStr, dFont, dBrush,
                    new PointF(rect.Right - size.Width - 18, rect.Bottom - size.Height - 14));
            }
        }

        // --------------------------
        // Helpers
        // --------------------------

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(r);
                path.CloseFigure();
                return path;
            }

            int d = radius * 2;
            var arc = new Rectangle(r.X, r.Y, d, d);

            path.AddArc(arc, 180, 90);                 // TL
            arc.X = r.Right - d;
            path.AddArc(arc, 270, 90);                 // TR
            arc.Y = r.Bottom - d;
            path.AddArc(arc, 0, 90);                   // BR
            arc.X = r.X;
            path.AddArc(arc, 90, 90);                  // BL

            path.CloseFigure();
            return path;
        }

        private static void DrawShadow(Graphics g, Rectangle rect, int radius, int blur)
        {
            // Cheap soft shadow: draw multiple expanded translucent paths
            for (int i = 1; i <= blur; i += 3)
            {
                var alpha = Math.Max(0, 60 - i * 2);
                var shadowRect = rect;
                shadowRect.Offset(0, 2);
                shadowRect.Inflate(i, i);

                using var path = RoundedRect(shadowRect, radius + i);
                using var b = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
                g.FillPath(b, path);
            }
        }

        private static void DrawSparkline(Graphics g, Rectangle rect, Color accent, decimal[] values)
        {
            // Draw in bottom half, subtle.
            var area = new Rectangle(
                rect.Right - 150,
                rect.Top + 28,
                130,
                rect.Height - 56
            );

            var min = values.Min();
            var max = values.Max();
            if (min == max) max = min + 1; // prevent divide by zero

            PointF Map(int i)
            {
                float x = area.X + (area.Width * (i / (float)(values.Length - 1)));
                var v = values[i];
                float t = (float)((v - min) / (max - min));
                float y = area.Bottom - (t * area.Height);
                return new PointF(x, y);
            }

            var pts = new PointF[values.Length];
            for (int i = 0; i < values.Length; i++)
                pts[i] = Map(i);

            using var path = new GraphicsPath();
            path.AddLines(pts);

            // Very subtle fill under the line
            using (var fill = new GraphicsPath())
            {
                fill.AddLines(pts);
                fill.AddLine(pts[^1].X, pts[^1].Y, pts[^1].X, area.Bottom);
                fill.AddLine(pts[^1].X, area.Bottom, pts[0].X, area.Bottom);
                fill.CloseFigure();

                using var fillBrush = new SolidBrush(Color.FromArgb(28, accent));
                g.FillPath(fillBrush, fill);
            }

            using var pen = new Pen(Color.FromArgb(180, accent), 3f);
            pen.LineJoin = LineJoin.Round;
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            g.DrawPath(pen, path);
        }
    }
}

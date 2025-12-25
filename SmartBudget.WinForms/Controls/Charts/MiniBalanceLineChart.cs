// Project: SmartBudget.WinForms
// File: Controls/Charts/MiniBalanceLineChart.cs

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Controls.Charts;

public sealed partial class MiniBalanceLineChart : Control
{
    private IReadOnlyList<(DateOnly MonthStart, long ValueCents)> _points
        = Array.Empty<(DateOnly, long)>();

    public MiniBalanceLineChart()
    {
        SetStyle(ControlStyles.UserPaint |
                 ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Height = 72; // short by default
    }

    public void SetData(IReadOnlyList<(DateOnly MonthStart, long ValueCents)> points)
    {
        _points = points ?? Array.Empty<(DateOnly, long)>();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_points.Count < 2)
            return;

        // Layout
        const int pad = 6;
        const int labelHeight = 18;

        var plot = new Rectangle(
            pad,
            pad,
            Width - pad * 2,
            Height - pad * 2 - labelHeight);

        if (plot.Width <= 10 || plot.Height <= 10)
            return;

        // Keep lines off the border by 1px
        plot.Inflate(-1, -1);

        // --- Zero-centered scaling ---
        var minValue = _points.Min(p => p.ValueCents);
        var maxValue = _points.Max(p => p.ValueCents);

        var maxAbs = Math.Max(Math.Abs(minValue), Math.Abs(maxValue));
        if (maxAbs == 0) maxAbs = 1;

        float centerY = plot.Top + plot.Height / 2f;
        float halfH = plot.Height / 2f;

        float ValueToY(long value)
        {
            float t = value / (float)maxAbs;  // -1..+1
            return centerY - (t * halfH);
        }

        float xStep = plot.Width / (float)(_points.Count - 1);

        // --- Plot border (subtle) ---
        using (var borderPen = new Pen(Color.FromArgb(80, ForeColor), 1f))
        {
            e.Graphics.DrawRectangle(borderPen, plot.Left, plot.Top, plot.Width, plot.Height);
        }

        // --- Dotted 0 line (dim) ---
        using (var zeroPen = new Pen(Color.FromArgb(100, ForeColor), 1f))
        {
            zeroPen.DashStyle = DashStyle.Dot;
            zeroPen.DashCap = DashCap.Round;

            e.Graphics.DrawLine(zeroPen, plot.Left, centerY, plot.Right, centerY);
        }

        // --- Line pens (green above, red below) ---
        using var posPen = new Pen(Color.FromArgb(0, 140, 0), 2f);
        using var negPen = new Pen(Color.FromArgb(200, 0, 0), 2f);

        // Draw segment-by-segment so we can color + split at zero crossing
        for (int i = 0; i < _points.Count - 1; i++)
        {
            long v1 = _points[i].ValueCents;
            long v2 = _points[i + 1].ValueCents;

            float x1 = plot.Left + i * xStep;
            float x2 = plot.Left + (i + 1) * xStep;

            float y1 = ValueToY(v1);
            float y2 = ValueToY(v2);

            // Both on same side (or touching zero)
            if ((v1 >= 0 && v2 >= 0) || (v1 <= 0 && v2 <= 0))
            {
                var pen = (v1 >= 0 && v2 >= 0) ? posPen : negPen;
                e.Graphics.DrawLine(pen, x1, y1, x2, y2);
                continue;
            }

            // Crosses zero: split at intersection
            float f = (0f - v1) / (float)(v2 - v1); // 0..1
            float xm = x1 + (x2 - x1) * f;
            float ym = centerY; // 0 maps to centerY

            e.Graphics.DrawLine(v1 >= 0 ? posPen : negPen, x1, y1, xm, ym);
            e.Graphics.DrawLine(v2 >= 0 ? posPen : negPen, xm, ym, x2, y2);
        }

        // Last-point dot (match sign color)
        var lastValue = _points[^1].ValueCents;
        using (var dotBrush = new SolidBrush(lastValue >= 0
            ? Color.FromArgb(0, 140, 0)
            : Color.FromArgb(200, 0, 0)))
        {
            float lx = plot.Left + (_points.Count - 1) * xStep;
            float ly = ValueToY(lastValue);
            e.Graphics.FillEllipse(dotBrush, lx - 2.5f, ly - 2.5f, 5f, 5f);
        }

        // --- Month labels (horizontal only) ---
        using var textBrush = new SolidBrush(ForeColor);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };

        var labelY = plot.Bottom + 2;

        for (int i = 0; i < _points.Count; i++)
        {
            // Every other month to reduce clutter (change/remove if you want all labels)
            if (i % 2 == 1) continue;

            var label = _points[i].MonthStart.ToString("MMM", CultureInfo.InvariantCulture);
            float x = plot.Left + i * xStep;

            var r = new RectangleF(x - 20, labelY, 40, labelHeight);
            e.Graphics.DrawString(label, Font, textBrush, r, sf);
        }
    }
}

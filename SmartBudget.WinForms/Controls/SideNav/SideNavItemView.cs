using SmartBudget.WinForms.Controls.SideNav;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Controls.SideNav;

public sealed partial class SideNavItemView : UserControl
{
    private readonly Panel _accent;
    private readonly PictureBox _icon;
    private readonly Label _text;
    private readonly Label _badge;

    private bool _hover;
    private bool _selected;
    private UiTheme _theme = new();

    public SideNavItem Item { get; private set; } = new SideNavItem("?", "?");

    public event EventHandler? ItemClicked;

    public SideNavItemView()
    {
        DoubleBuffered = true;
        TabStop = false;
        Height = 40;
        Cursor = Cursors.Hand;

        _accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = Color.Transparent
        };

        _icon = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.CenterImage,
            Width = 36,
            Dock = DockStyle.Left
        };

        _badge = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Right,
            Width = 34,
            Height = 22,
            Margin = new Padding(0),
            Visible = false
        };

        _text = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Padding = new Padding(2, 0, 0, 0),
            ForeColor = _theme.Primary
        };

        Controls.Add(_text);
        Controls.Add(_badge);
        Controls.Add(_icon);
        Controls.Add(_accent);

        // Make entire row clickable/hoverable
        WireMouse(this);
        WireMouse(_text);
        WireMouse(_icon);
        WireMouse(_badge);
        WireMouse(_accent);
    }

    public void Bind(SideNavItem item, UiTheme theme)
    {
        Item = item;
        _theme = theme;

        _text.Text = item.Text;
        _text.Font = theme.ItemFont;

        _icon.Image = item.Icon;

        SetBadge(item.Badge);

        Enabled = item.Enabled;
        _text.ForeColor = item.Enabled ? theme.Primary : theme.TextMuted;

        ApplyVisual();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyVisual();
    }

    public void SetBadge(int? badge)
    {
        if (badge is null || badge <= 0)
        {
            _badge.Visible = false;
            _badge.Text = "";
            return;
        }

        _badge.Visible = true;
        _badge.Text = badge.Value > 99 ? "99+" : badge.Value.ToString();
        _badge.BackColor = _theme.BadgeBack;
        _badge.ForeColor = _theme.BadgeText;
        _badge.Font = _theme.ItemFont;

        // “pill” look
        _badge.Padding = new Padding(0);
        _badge.Paint -= Badge_Paint;
        _badge.Paint += Badge_Paint;
        _badge.Invalidate();
    }

    private void Badge_Paint(object? sender, PaintEventArgs e)
    {
        // Rounded badge background
        var r = _badge.ClientRectangle;
        r.Inflate(-1, -1);

        using var path = RoundedRect(r, 10);
        using var brush = new SolidBrush(_theme.BadgeBack);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            e.Graphics,
            _badge.Text,
            _badge.Font,
            r,
            _theme.BadgeText,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void WireMouse(Control c)
    {
        c.MouseEnter += (_, __) => { _hover = true; ApplyVisual(); };
        c.MouseLeave += (_, __) => { _hover = false; ApplyVisual(); };
        c.Click += (_, __) =>
        {
            if (!Item.Enabled) return;
            ItemClicked?.Invoke(this, EventArgs.Empty);
        };
    }

    private void ApplyVisual()
    {
        // Accent stripe
        _accent.BackColor = _selected ? _theme.Primary : Color.Transparent;

        // Background states
        if (_selected)
            BackColor = _theme.SelectedBack;
        else if (_hover && Item.Enabled)
            BackColor = _theme.HoverBack;
        else
            BackColor = Color.Transparent; // matches your old ItemNormalBack = Transparent

        // Text states
        _text.ForeColor = Item.Enabled ? _theme.Primary : _theme.TextMuted;

        Invalidate();
    }
}
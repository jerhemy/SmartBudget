using System.Drawing;

namespace SmartBudget.WinForms.Controls.SideNav;

public sealed class SideNavTheme
{
    // Container
    public Color Background { get; set; } = Color.FromArgb(22, 37, 53);
    public Color BorderRight { get; set; } = Color.FromArgb(35, 55, 75);

    // Items
    public Color ItemNormalBack { get; set; } = Color.Transparent;
    public Color ItemHoverBack { get; set; } = Color.FromArgb(28, 49, 70);
    public Color ItemSelectedBack { get; set; } = Color.FromArgb(28, 49, 70);

    public Color TextNormal { get; set; } = Color.FromArgb(220, 230, 240);
    public Color TextDisabled { get; set; } = Color.FromArgb(130, 150, 170);

    public Color Accent { get; set; } = Color.FromArgb(0, 150, 255); // selection stripe

    // Header
    public Color HeaderText { get; set; } = Color.FromArgb(160, 180, 200);

    // Badge
    public Color BadgeBack { get; set; } = Color.FromArgb(0, 150, 255);
    public Color BadgeText { get; set; } = Color.White;

    public Font ItemFont { get; set; } = SystemFonts.MessageBoxFont;
    public Font HeaderFont { get; set; } = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);
}
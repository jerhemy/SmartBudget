using System.Drawing;

namespace SmartBudget.WinForms.Controls.SideNav;

public enum SideNavActionKind
{
    Dashboard,        
    Account,
}


public abstract record SideNavEntry;

public sealed record SideNavHeader(string Text) : SideNavEntry;

public sealed record SideNavItem(
    string Id,
    string Text,
    Image? Icon = null,
    int? Badge = null,
    bool Enabled = true,
    SideNavActionKind ActionKind = SideNavActionKind.Account,
    object? Payload = null
) : SideNavEntry;
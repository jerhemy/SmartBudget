using System;

namespace SmartBudget.WinForms.Controls.SideNav;

public sealed class SideNavSelectionChangedEventArgs : EventArgs
{
    public SideNavSelectionChangedEventArgs(string selectedId) => SelectedId = selectedId;
    public string SelectedId { get; }
    public SideNavItem Item { get; }

}

public sealed class SideNavItemInvokedEventArgs : EventArgs
{
    public SideNavItemInvokedEventArgs(SideNavItem item) => Item = item;
    public SideNavItem Item { get; }
}
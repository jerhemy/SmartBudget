using SmartBudget.WinForms.Pages;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Abstractions;

public interface INavigationService
{
    PageKey Current { get; }
    void Navigate(PageKey key, NavigationContext? ctx = null);
}

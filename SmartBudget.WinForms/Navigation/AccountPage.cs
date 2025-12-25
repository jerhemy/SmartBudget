using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Controls.Calendar;
using SmartBudget.WinForms.Pages;
using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using SmartBudget.WinForms.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Navigation;

public sealed record AccountNavArgs(AccountInfo Account);

public sealed partial class AccountPage : UserControl, IPage
{
    public PageKey Key => PageKey.Accounts;
    public string Title => "Account";
    public Control View => this;

    private long? _accountId;

    private readonly CalendarDataService _calendarDataService;
    public AccountPage(CalendarDataService calendarDataService, UiTheme theme)
    {
        InitializeComponent();
        _calendarDataService = calendarDataService;
        calendarControl.ApplyTheme(theme);
    }

    public async Task OnNavigatedTo(NavigationContext context)
    {
        
        _accountId = context.AccountId;
        //// If you also pass a full AccountInfo:
        //if (context.Payload is AccountNavArgs a)
        //{
        //    // show header immediately
        //    // lblAccountName.Text = a.Account.Name;
        //    _accountId = a.Account.Id;
        //}
        _accountId = context.AccountId;

        if (_accountId is null)
        {
            ShowNoAccountSelected();
            return;
        }

        Invalidate();
        //LoadAccount(_accountId.Value);
        await ReloadCalendarAsync();
    }

    private void LoadAccount(long accountId)
    {
        // calendar.Load(accountId);
        // recurringGrid.Load(accountId);
        // etc...
    }

    private void ShowNoAccountSelected()
    {
        // placeholder UI
    }

    private async Task ReloadCalendarAsync(DateOnly? currentDate = null)
    {
        if (!_accountId.HasValue)
            return;

        var today = DateOnly.FromDateTime(DateTime.Now);
        var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

        if (currentDate.HasValue)
        {
            firstDayOfMonth = currentDate.Value;
        }

        var month = firstDayOfMonth;

        var days = await _calendarDataService.GetMonthAsync(_accountId.Value, month.Year, month.Month, CancellationToken.None);

        var balance = await _calendarDataService.GetPreviousBalance(_accountId.Value, month.Year, month.Month, CancellationToken.None);

        calendarControl.StartingBalanceCents = 0; // TODO: compute from account + prior txns
        calendarControl.DisplayedMonth = month;
        calendarControl.SetMonth(month);
        calendarControl.SetData(days, balance / 100);
    }
}

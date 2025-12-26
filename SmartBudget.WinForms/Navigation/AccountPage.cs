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
    private long? _selectedTransaction;

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
        calendarControl.DayClicked += CalendarControl_DayClicked;
        calendarControl.TransactionClicked += CalendarControl_TransactionClicked;
        calendarControl.TransactionMoved += CalendarControl_TransactionMoved;
        calendarControl.DateChanged += CalendarControl_DateChanged;

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

    private void panel1_Paint(object sender, PaintEventArgs e)
    {

    }

    private async void CalendarControl_DayClicked(object? sender, CalendarDayClickedEventArgs e)
    {
        btnAddTransaction.Show();
        btnDeleteTransaction.Hide();

        _selectedTransaction = null;
        txtAmount.Text = string.Empty;
        txtTitle.Text = string.Empty;
        dtTransactionDate.Value = e.Date.ToDateTime(TimeOnly.MinValue);
    }


    private async void CalendarControl_TransactionClicked(object? sender, CalendarTransactionClickedEventArgs e)
    {
        _selectedTransaction = e.TransactionId;
        btnAddTransaction.Hide();
        btnDeleteTransaction.Show();
        txtAmount.Text = (e.Amount / 100).ToString();
        txtTitle.Text = e.Title;
        dtTransactionDate.Value = e.Date.ToDateTime(TimeOnly.MinValue);
    }

    private async void CalendarControl_TransactionMoved(object? sender, CalendarTransactionMovedEventArgs e)
    {
        //try
        //{
        //    await _transactions.UpdateDateAsync(e.TransactionId, e.To, CancellationToken.None);
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(this, ex.ToString(), "Error saving move", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //}

        //await ReloadCalendarAsync();
    }

    private async void CalendarControl_DateChanged(object? sender, DateOnly e)
    {
        await ReloadCalendarAsync(e);
    }

    //private async Task ReloadCalendarAsync(DateOnly? currentDate = null)
    //{
    //    if (_accountId == 0)
    //        return;

    //    var today = DateOnly.FromDateTime(DateTime.Now);
    //    var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);

    //    if (currentDate.HasValue)
    //    {
    //        firstDayOfMonth = currentDate.Value;
    //    }

    //    var month = firstDayOfMonth;

    //    var days = await _calendarDataService.GetMonthAsync(_accountId, month.Year, month.Month, CancellationToken.None);

    //    var balance = await _calendarDataService.GetPreviousBalance(_accountId, month.Year, month.Month, CancellationToken.None);

    //    calendarControl.StartingBalanceCents = 0; // TODO: compute from account + prior txns
    //    calendarControl.DisplayedMonth = month;
    //    calendarControl.SetMonth(month);
    //    calendarControl.SetData(days, balance / 100);


    //}
}

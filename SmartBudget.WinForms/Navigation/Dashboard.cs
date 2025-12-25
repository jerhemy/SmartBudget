using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Controls.SideNav;
using SmartBudget.WinForms.Pages;
using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SmartBudget.WinForms.Navigation;

public sealed partial class Dashboard : UserControl, IPage
{
    public PageKey Key => PageKey.Dashboard;
    public string Title => "Dashboard";
    public Control View => this;

    //private readonly Label _lbl;
    private bool _shown;

    public event EventHandler? DashboardShown;

    private readonly ITransactionRepository _txnRepository;
    public Dashboard(ITransactionRepository txn)
    {
        InitializeComponent();

        _txnRepository = txn;

        Dock = DockStyle.Fill;
        BackColor = SystemColors.Control;

        //_lbl = new Label
        //{
        //    Text = "Dashboard",
        //    AutoSize = true,
        //    Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
        //    Location = new Point(16, 16)
        //};

        //Controls.Add(_lbl);

        //Shown += Dashboard_Shown;
    }

    public async Task OnNavigatedTo()
    {
        var total = await _txnRepository.GetTotal(DateOnly.FromDateTime(DateTime.Now), CancellationToken.None);
        balanceTile.Total = (decimal)(total / 100.0);
    }

    public async Task OnNavigatedTo(NavigationContext context)
    {
        var total = await _txnRepository.GetTotal(DateOnly.FromDateTime(DateTime.Now), CancellationToken.None);
        balanceTile.Total = (decimal)(total / 100.0);
    }
}

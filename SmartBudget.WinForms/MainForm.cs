using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SmartBudget.Recurring;
using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Controls.SideNav;
using SmartBudget.WinForms.Navigation;
using SmartBudget.WinForms.Pages;
using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using SmartBudget.WinForms.Quicken;
using System.Xml.Linq;

namespace SmartBudget.WinForms;

public partial class MainForm : Form
{
    private INavigationService _nav = null!;
    private readonly IServiceProvider _sp;

    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    private readonly UiTheme _theme;

    private long _accountId = 0;

    public MainForm(IServiceProvider sp, IAccountRepository accounts, ITransactionRepository transactions, UiTheme theme)
    {
        InitializeComponent();

        _sp = sp;
        _accounts = accounts;
        _transactions = transactions;
        _theme = theme;

        // Map PageKey -> factory that resolves pages from DI (so pages get constructor injection)
        var factories = new Dictionary<PageKey, Func<IPage>>
        {
            [PageKey.Dashboard] = () => _sp.GetRequiredService<Dashboard>(),
            [PageKey.Accounts] = () => _sp.GetRequiredService<AccountPage>(),
            [PageKey.Import] = () => _sp.GetRequiredService<ImportPage>()
        };

        _nav = new NavigationService(pnlContentHost, factories);

        Shown += MainForm_Shown;
    }



    private async void MainForm_Shown(object? sender, EventArgs e)
    {

        await PopulateSideNavAccounts();
        sideNav.ApplyTheme(_theme);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        base.OnFormClosed(e);
    }

    private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        // Re-apply system colors when user changes theme/colors
        ApplySystemTheme();
    }

    private void ApplySystemTheme()
    {
        // Use system colors at the top and push down to child controls
        this.BackColor = SystemColors.Control;
        this.ForeColor = SystemColors.ControlText;

        ApplyToChildren(this);
        Invalidate(true);
    }

    private static void ApplyToChildren(Control root)
    {
        foreach (Control c in root.Controls)
        {
            // Use heuristics: containers vs input-like controls
            if (c is TextBoxBase or ListBox or ListView)
            {
                c.BackColor = SystemColors.Window;
                c.ForeColor = SystemColors.WindowText;
            }
            else
            {
                c.BackColor = SystemColors.Control;
                c.ForeColor = SystemColors.ControlText;
            }

            if (c.HasChildren)
                ApplyToChildren(c);
        }
    }

    private async Task LoadMiniChartAsync(long accountId, CancellationToken ct)
    {
        //var (start, endExclusive) = MonthRange.SixBackSixForward();

        //var rows = await _summaryRepository.GetMonthlyEndBalancesAsync(accountId, start, endExclusive, ct);

        //// chart wants MonthStart + value
        //var points = rows
        //    .Select(r => (r.MonthStart, r.EndBalanceCents))
        //    .ToArray();

        //miniBalanceLineChart1.SetData(points);
    }

    public static class MonthRange
    {
        public static (DateOnly startInclusive, DateOnly endExclusive) SixBackSixForward()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var firstThisMonth = new DateOnly(today.Year, today.Month, 1);

            var start = firstThisMonth.AddMonths(-6);
            var endExclusive = firstThisMonth.AddMonths(7); // +6 months forward inclusive => exclusive is +7

            return (start, endExclusive);
        }
    }


    private async void btnImportCsv_Click(object sender, EventArgs e)
    {
        //if (cboAccounts.SelectedValue is not long accountId)
        //{
        //    MessageBox.Show("Please select an account first.");
        //    return;
        //}

        //using var dlg = new OpenFileDialog
        //{
        //    Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
        //    Title = "Import bank transactions"
        //};

        //if (dlg.ShowDialog(this) != DialogResult.OK)
        //    return;

        //try
        //{
        //    var csvText = await File.ReadAllTextAsync(dlg.FileName);
        //    var result = await _importService.ImportAsync(
        //        accountId,
        //        csvText,
        //        sourceName: Path.GetFileName(dlg.FileName),
        //        ct: CancellationToken.None);

        //    MessageBox.Show(
        //        $"Parsed: {result.Parsed}\nInserted: {result.Inserted}\nDuplicates skipped: {result.SkippedAsDuplicate}",
        //        "Import complete");

        //    await ReloadCalendarAsync();
        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(ex.Message, "Import failed");
        //}
    }

    private async void btnAddTransaction_Click(object sender, EventArgs e)
    {
        //if (cboAccounts.SelectedValue is not long accountId)
        //    return;

        //double amount = 0;
        //if (!string.IsNullOrWhiteSpace(txtAmount.Text))
        //{
        //    double.TryParse(txtAmount.Text, out amount);
        //}

        //try
        //{
        //    var result = await _transactions.AddTransaction(
        //        accountId: accountId,
        //        date: DateOnly.FromDateTime(dtTransactionDate.Value),
        //        title: txtTitle.Text,
        //        amount: amount,
        //        ct: CancellationToken.None);

        //    if (result)
        //        await ReloadCalendarAsync();

        //}
        //catch (Exception ex)
        //{
        //    MessageBox.Show(this, ex.ToString(), "Error adding transaction", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //}
    }

    private async void btnImportQuicken_Click(object sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Quicken Files (*.qfx)|*.qfx",
            Title = "Import Quicken File"
        };

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var qfxFile = await File.ReadAllTextAsync(dlg.FileName);

            var xml = OfxSgmlToXml.ConvertToXml(qfxFile);
            var doc = XDocument.Parse(xml);
            var quickenParser = new OfxParser();
            var txn = quickenParser.Parse(qfxFile);

            MessageBox.Show(
                $"Parsed {txn.Count} transactions from Quicken file.",
                "Import complete");

            var transactionAccountId = long.Parse(txn[0].AccountId);

            ImportedStatement account = txn[0];

            var foundAccount = await _accounts.GetByIdAsync(transactionAccountId, CancellationToken.None);

            if (foundAccount is null)
            {
                await _accounts.AddAccount(transactionAccountId, $"{account.AccountType} - {Mask(transactionAccountId.ToString())}", CancellationToken.None);
            }

            var transactions = account.Transactions.Select(x => new InsertTransactionRequest(
                AccounId: transactionAccountId,
                Amount: (double)x.Amount,
                CheckNumber: x.CheckNumber,
                Description: x.Name ?? string.Empty,
                Date: x.PostedDate,
                Memo: x.Memo,
                Source: "QFX",
                ExternalId: x.FitId
             )).ToList();

            await _transactions.BulkInsertAsync(transactions, CancellationToken.None);

            //MessageBox.Show(
            //    $"Parsed: {result.Parsed}\nInserted: {result.Inserted}\nDuplicates skipped: {result.SkippedAsDuplicate}",
            //    "Import complete");

            //await PopulateSideNavAccounts();
            //await ReloadCalendarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import failed");
        }
    }

    public static string Mask(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        const int keep = 4;
        if (input.Length <= keep)
            return input;

        return new string('*', input.Length - keep) + input[^keep..];
    }

    public async Task PopulateSideNavAccounts()
    {

        try
        {
            var accounts = await _accounts.GetAllAsync(CancellationToken.None);

            var entries = new List<SideNavEntry>
            {
                new SideNavHeader("MAIN"),
                //new SideNavItem("home", "Home" /*, homeIcon*/),
                new SideNavItem("dashboard", "Dashboard", null, null, true, SideNavActionKind.Dashboard, null),
                new SideNavItem("reports", "Reports"),

                new SideNavHeader("ACCOUNTS")
            };

            foreach (var account in accounts)
            {
                entries.Add(new SideNavItem($"{account.Id}", account.Name, null, null, true, SideNavActionKind.Account, account.Id));
            }

            entries.AddRange(new List<SideNavEntry> {
                new SideNavHeader("TOOLS"),
                new SideNavItem("import", "Import Statements", null, null, true, SideNavActionKind.Import, null),
                new SideNavItem("recurring", "Recurring Transactions", null, null, true, SideNavActionKind.Import, null),
            });

            sideNav.SetEntries(entries, selectId: "dashboard");
            _nav.Navigate(PageKey.Dashboard);

            sideNav.ItemInvoked += async (_, e) =>
            {
                switch (e.Item.ActionKind)
                {
                    case SideNavActionKind.Dashboard:
                        {
                            _nav.Navigate(PageKey.Dashboard);
                            break;
                        }
                    case SideNavActionKind.Account:
                        {
                            _accountId = e.Item.Payload is long id ? id : long.Parse(e.Item.Payload!.ToString()!);
                            _nav.Navigate(PageKey.Accounts, new NavigationContext(AccountId: _accountId));
                            break;
                        }
                    case SideNavActionKind.Import:
                        {
                            _nav.Navigate(PageKey.Import);
                            break;
                        }
                }
            };

            sideNav.SelectedChanged += (_, e) =>
            {
                //switch (e.Item.ActionKind)
                //{
                //    case SideNavActionKind.Navigate:
                //        {
                //            //var route = (string?)e.Item.Payload ?? e.Item.Id;
                //            //NavigateTo(route);
                //            break;
                //        }
                //    case SideNavActionKind.SelectAccount:
                //        {
                //            var accountId = e.Item.Payload is long id ? id : long.Parse(e.Item.Payload!.ToString()!);
                //            //SetSelectedAccount(accountId);
                //            break;
                //        }
                //    case SideNavActionKind.Command:
                //        {
                //            var cmd = (string?)e.Item.Payload ?? e.Item.Id;
                //            //RunCommand(cmd);
                //            break;
                //        }
                //}
            };

            //await ReloadCalendarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }

    //private async void btnDeleteTransaction_Click(object sender, EventArgs e)
    //{
    //    if (_selectedTranscation.HasValue)
    //    {
    //        await _transactions.DeleteTransactionById(_selectedTranscation.Value, CancellationToken.None);
    //        await ReloadCalendarAsync();
    //    }
    //}

    private static DetectedAutoPayRow ToRow(DetectedAutoPay d) => new()
    {
        Name = d.DisplayName,
        Cadence = d.Cadence,
        Count = d.Count,
        AvgAmount = d.AvgAmountCents / 100m,
        Confidence = d.Confidence,
        SeriesKey = d.SeriesKey,
        FirstSeen = d.FirstSeen,
        LastSeen = d.LastSeen
    };

    private readonly BindingSource _autoPaySource = new();

    //private void SetupAutoPayGrid()
    //{
    //    gridAutoPays.AutoGenerateColumns = false;
    //    gridAutoPays.AllowUserToAddRows = false;
    //    gridAutoPays.AllowUserToDeleteRows = false;
    //    gridAutoPays.ReadOnly = true;
    //    gridAutoPays.MultiSelect = false;
    //    gridAutoPays.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    //    gridAutoPays.RowHeadersVisible = false;
    //    gridAutoPays.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

    //    gridAutoPays.Columns.Clear();

    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.Name),
    //        HeaderText = "Auto Pay",
    //        FillWeight = 40
    //    });

    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.Cadence),
    //        HeaderText = "Cadence",
    //        FillWeight = 12
    //    });

    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.Count),
    //        HeaderText = "Count",
    //        FillWeight = 10,
    //        DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
    //    });

    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.AvgAmount),
    //        HeaderText = "Avg",
    //        FillWeight = 12,
    //        DefaultCellStyle =
    //    {
    //        Format = "C2",
    //        Alignment = DataGridViewContentAlignment.MiddleRight
    //    }
    //    });

    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.Confidence),
    //        HeaderText = "Confidence",
    //        FillWeight = 12,
    //        DefaultCellStyle =
    //    {
    //        Format = "P0", // 0.92 -> 92%
    //        Alignment = DataGridViewContentAlignment.MiddleRight
    //    }
    //    });

    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.LastSeen),
    //        HeaderText = "Last",
    //        FillWeight = 14,
    //        DefaultCellStyle = { Format = "yyyy-MM-dd" }
    //    });

    //    // Optional: hide SeriesKey but keep it for selection/drill-in
    //    gridAutoPays.Columns.Add(new DataGridViewTextBoxColumn
    //    {
    //        DataPropertyName = nameof(DetectedAutoPayRow.SeriesKey),
    //        HeaderText = "SeriesKey",
    //        Visible = false
    //    });

    //    gridAutoPays.DataSource = _autoPaySource;
    //}

    //private async Task LoadDetectedAutoPaysAsync(long accountId)
    //{
    //    var list = await _summaryRepository.GetForDetectionAsync(_accountId, 12, CancellationToken.None);
    //    var detected = AutoPayDetector.DetectMonthlyAutoPays(list, minOccurrences: 2, minConfidence: 0.75);

    //    //var detected = await _autoPayDetectionService.DetectAsync(accountId, CancellationToken.None);

    //    var rows = detected.Select(ToRow).ToList();

    //    _autoPaySource.DataSource = rows;
    //}

    //private DetectedAutoPayRow? GetSelectedAutoPay()
    //{
    //    if (_autoPaySource.Current is DetectedAutoPayRow row)
    //        return row;

    //    return null;
    //}


    //private void gridAutoPays_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    //{
    //    if (gridAutoPays.Columns[e.ColumnIndex].DataPropertyName == nameof(DetectedAutoPayRow.AvgAmount)
    //        && e.Value is decimal d)
    //    {
    //        e.CellStyle.ForeColor = d < 0 ? Color.FromArgb(200, 0, 0) : Color.FromArgb(0, 140, 0);
    //    }
    //}

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }
}

public sealed record InsertTransactionRequest(
    long AccounId,
    double Amount,
    DateOnly Date,
    string? CheckNumber,
    string Description,
    string? Memo,
    string Source,
    string? ExternalId);

public sealed class DetectedAutoPayRow
{
    public string Name { get; init; } = "";
    public string Cadence { get; init; } = "";
    public int Count { get; init; }
    public decimal AvgAmount { get; init; }        // dollars
    public double Confidence { get; init; }        // 0..1
    public string SeriesKey { get; init; } = "";
    public DateOnly FirstSeen { get; init; }
    public DateOnly LastSeen { get; init; }
}

public enum RecurringKind
{
    AutoPay,
    Deposit
}

public sealed class RecurringDetectedRow
{
    public RecurringKind Kind { get; init; }
    public string Name { get; init; } = "";
    public string Cadence { get; init; } = "";
    public int Count { get; init; }
    public decimal AvgAmount { get; init; }         // dollars
    public double Confidence { get; init; }         // 0..1
    public DateOnly FirstSeen { get; init; }
    public DateOnly LastSeen { get; init; }

    // Hidden key used for drill-in
    public string SeriesKey { get; init; } = "";
}
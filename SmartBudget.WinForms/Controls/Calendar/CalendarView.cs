// Project: SmartBudget.WinForms
// File: Controls/Calendar/CalendarView.cs

using System.Globalization;
using System.ComponentModel;
using SmartBudget.Domain;

namespace SmartBudget.WinForms.Controls.Calendar;

public sealed partial class CalendarView : UserControl
{
    private UiTheme? _theme;


    private const int HeaderHeight = 44;
    private const int Columns = 7;
    private const int MaxRows = 6;

    private readonly Panel _header;
    private readonly Button _btnPrev;
    private readonly Button _btnNext;
    private readonly Label _lblMonthYear;

    private readonly CalendarDayCell[] _cells = new CalendarDayCell[Columns * MaxRows];

    private DateOnly _displayedMonth;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DateOnly DisplayedMonth { get; set; }

    /// <summary>
    /// Balance (in cents) that should be considered the "starting point" for the first visible cell.
    /// Typically: account balance as-of the day BEFORE the first visible date in the grid.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public long StartingBalanceCents { get; set; }

    public CalendarView()
    {
        SuspendLayout();

        // Header
        _header = new Panel
        {
            Dock = DockStyle.Top,
            Height = HeaderHeight
        };

        _btnPrev = new Button
        {
            Text = "<",
            Width = 32,
            Dock = DockStyle.Left
        };
        _btnPrev.Click += (_, _) => ChangeMonth(-1);

        _btnNext = new Button
        {
            Text = ">",
            Width = 32,
            Dock = DockStyle.Right
        };
        _btnNext.Click += (_, _) => ChangeMonth(1);

        _lblMonthYear = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font, FontStyle.Bold)
        };

        _header.Controls.Add(_lblMonthYear);
        _header.Controls.Add(_btnPrev);
        _header.Controls.Add(_btnNext);

        Controls.Add(_header);

        // Create day cells
        for (var i = 0; i < _cells.Length; i++)
        {
            var cell = new CalendarDayCell
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White
            };

            cell.LineDropped += OnCellLineDropped;

            _cells[i] = cell;
            Controls.Add(cell);
            WireCell(_cells[i]);
        }

        ResumeLayout();

        _displayedMonth = DateOnly.FromDateTime(DateTime.Today)
            .AddDays(-DateTime.Today.Day + 1);

        LayoutCells();
        PopulateMonth();
    }


    public void ApplyTheme(UiTheme theme)
    {
        _theme = theme;

        BackColor = theme.PanelBack; // match SideNav canvas/panel feel
        ForeColor = theme.TextPrimary;

        _header.BackColor = theme.PanelBack;
        _lblMonthYear.ForeColor = theme.TextPrimary;

        StyleNavButton(_btnPrev, theme);
        StyleNavButton(_btnNext, theme);

        for (var i = 0; i < _cells.Length; i++)
            _cells[i].ApplyTheme(theme);

        Invalidate(true);
    }

    private static void StyleNavButton(Button b, UiTheme theme)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = theme.Border;
        b.BackColor = theme.CardBack;            // same “pill/button” feel as SideNav items
        b.ForeColor = theme.TextPrimary;
        b.Cursor = Cursors.Hand;
    }

    // PUBLIC API --------------------------------------------------------------

    public void SetMonth(DateOnly month)
    {
        _displayedMonth = new DateOnly(month.Year, month.Month, 1);
        PopulateMonth();
    }

    public void SetData(IReadOnlyList<CalendarDayData> days, double startingAmount = 0)
    {
        var _startingAmount = startingAmount;

        foreach (var cell in _cells)
        {
            var dayData = days.FirstOrDefault(d => d.Date == cell.Date);
            if (dayData is null)
            {
                cell.SetLines(Array.Empty<CalendarTransactionLine>());
                continue;
            }

            string description = string.Empty;

            var lines = dayData.Transactions
                .Select(t => new CalendarTransactionLine
                {
                    TransactionId = t.Id,
                    Title = t.Description,
                    Amount = t.AmountCents
                })
                .OrderByDescending(d => d.Amount)
                .ToArray();

            _startingAmount = (double)(_startingAmount + (lines.Sum(x => x.Amount) / 100));

            cell.SetLines(lines, _startingAmount);
        }
    }

    public event EventHandler<CalendarTransactionMovedEventArgs>? TransactionMoved;
    public event EventHandler<DateOnly>? DateChanged;

    // LAYOUT -----------------------------------------------------------------

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutCells();
    }

    private void LayoutCells()
    {
        var cellWidth = Width / Columns;
        var cellHeight = (Height - HeaderHeight) / MaxRows;

        for (var i = 0; i < _cells.Length; i++)
        {
            var row = i / Columns;
            var col = i % Columns;

            _cells[i].Bounds = new Rectangle(
                col * cellWidth,
                HeaderHeight + row * cellHeight,
                cellWidth,
                cellHeight);
        }
    }

    // MONTH POPULATION --------------------------------------------------------

    private void PopulateMonth()
    {
        _lblMonthYear.Text = _displayedMonth
            .ToDateTime(TimeOnly.MinValue)
            .ToString("MMMM yyyy", CultureInfo.CurrentCulture);

        var firstOfMonth = _displayedMonth;
        var firstDayOfWeek = (int)firstOfMonth.DayOfWeek;
        var startDate = firstOfMonth.AddDays(-firstDayOfWeek);

        var today = DateOnly.FromDateTime(DateTime.Today);

        for (var i = 0; i < _cells.Length; i++)
        {
            var cellDate = startDate.AddDays(i);
            var inMonth = cellDate.Month == _displayedMonth.Month;
            var isToday = cellDate == today;

            _cells[i].SetDate(cellDate, inMonth, isToday);
        }
    }

    private void ChangeMonth(int delta)
    {
        _displayedMonth = _displayedMonth.AddMonths(delta);
        DateChanged?.Invoke(this, _displayedMonth);
        PopulateMonth();
    }

    // DRAG & DROP COORDINATION -----------------------------------------------

    private void OnCellLineDropped(object? sender, CalendarLineDroppedEventArgs e)
    {
        if (e.DragData.SourceDate == e.TargetDate)
            return;

        var sourceCell = _cells.FirstOrDefault(c => c.Date == e.DragData.SourceDate);
        var targetCell = (CalendarDayCell)sender!;

        if (sourceCell is null)
            return;

        if (!sourceCell.TryRemoveLineAt(e.DragData.SourceIndex, out var removed))
            return;

        targetCell.AddLine(removed!);

        TransactionMoved?.Invoke(this,
            new CalendarTransactionMovedEventArgs(
                removed!.TransactionId,
                e.DragData.SourceDate,
                e.TargetDate));
    }

    public event EventHandler<CalendarTransactionClickedEventArgs>? TransactionClicked;
    public event EventHandler<CalendarDayClickedEventArgs>? DayClicked;

    private CalendarDayCell? _selectedCell;

    private void WireCell(CalendarDayCell cell)
    {
        cell.TransactionClicked += Cell_TransactionClicked;
        cell.DayClicked += Cell_DayClicked;
    }

    private void Cell_TransactionClicked(object? sender, CalendarTransactionClickedEventArgs e)
    {
        if (sender is not CalendarDayCell cell)
            return;

        // ensure only one selected line across the whole calendar
        if (_selectedCell is not null && !ReferenceEquals(_selectedCell, cell))
            _selectedCell.ClearSelection();

        _selectedCell = cell;

        // bubble up
        TransactionClicked?.Invoke(this, e);
    }

    private void Cell_DayClicked(object? sender, CalendarDayClickedEventArgs e)
    {
        if (sender is CalendarDayCell cell)
        {
            if (_selectedCell is not null && !ReferenceEquals(_selectedCell, cell))
                _selectedCell.ClearSelection();

            _selectedCell = null; // day click clears transaction selection
        }

        DayClicked?.Invoke(this, e);
    }
}

// EVENTS ---------------------------------------------------------------------

public sealed class CalendarTransactionMovedEventArgs : EventArgs
{
    public CalendarTransactionMovedEventArgs(long transactionId, DateOnly from, DateOnly to)
    {
        TransactionId = transactionId;
        From = from;
        To = to;
    }

    public long TransactionId { get; }
    public DateOnly From { get; }
    public DateOnly To { get; }
}

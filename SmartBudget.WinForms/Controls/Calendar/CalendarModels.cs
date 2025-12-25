namespace SmartBudget.WinForms.Controls.Calendar;

public sealed record CalendarTransaction(DateOnly Date,string Title,decimal Amount);

public sealed record DayCellData(DateOnly Date,bool IsInDisplayedMonth,CalendarTransaction[] Transactions,decimal RunningTotalEndOfDay);

[Serializable]
public sealed record CalendarDragData(long TransactionId, DateOnly SourceDate, int SourceIndex);

public sealed record CalendarTransactionLine
{
    public long TransactionId { get; init; }
    public string Title { get; init; } = "";

    public string Memo { get; init; } = "";

    public double Amount { get; init; }
}
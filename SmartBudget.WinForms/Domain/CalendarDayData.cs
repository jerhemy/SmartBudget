using SmartBudget.WinForms.Persistence.Sqlite.Repositories;

namespace SmartBudget;

public sealed record CalendarDayData(
    DateOnly Date,
    long DepositsCents,
    long ChargesCents,
    long TotalCents,
    long EndingBalanceCents,
    IReadOnlyList<TransactionRow> Transactions
);
using SmartBudget.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Abstractions
{
    public interface ICalendarDataService
    {
        Task<IReadOnlyList<CalendarDayData>> GetMonthAsync(
            long accountId,
            int year,
            int month,
            CancellationToken ct);
    }
}

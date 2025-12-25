using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Services
{
    public class SummaryService(SummaryRepository summaryRepository)
    {
        //public Task<decimal> GetTotalSummaryAsync(long accountId, DateTime from, DateTime to, CancellationToken ct)
        //{
        //    return summaryRepository.GetTotalSummaryAsync(accountId, from, to, ct);
        //}
    }
}

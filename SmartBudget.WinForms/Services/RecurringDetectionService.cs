using SmartBudget.Recurring;
using SmartBudget.WinForms.Persistence.Sqlite.Repositories;

namespace SmartBudget.WinForms.Services
{
    public sealed class RecurringDetectionService
    {
        private readonly SummaryRepository _repo;

        public RecurringDetectionService(SummaryRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<DetectedAutoPay>> DetectAutoPaysAsync(long accountId, CancellationToken ct)
        {
            var txns = await _repo.GetForDetectionAsync(accountId, lookbackMonths: 18, ct);
            return AutoPayDetector.DetectMonthlyAutoPays(txns, minOccurrences: 4, minConfidence: 0.75);
        }

        public async Task<IReadOnlyList<DetectedRecurringDeposit>> DetectRecurringDepositsAsync(long accountId, CancellationToken ct)
        {
            var txns = await _repo.GetForDetectionAsync(accountId, lookbackMonths: 18, ct);
            return RecurringDepositDetector.Detect(txns, minOccurrences: 4, minConfidence: 0.75);
        }
    }
}

using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartBudget.WinForms.Services
{
    public sealed class TransactionImportService : ITransactionImportService
    {
        private readonly IBankCsvTransactionParser _parser;
        private readonly ITransactionRepository _writer;

        public TransactionImportService(
            IBankCsvTransactionParser parser,
            ITransactionRepository writer)
        {
            _parser = parser;
            _writer = writer;
        }

        public async Task<TransactionImportResult> ImportAsync(
            long accountId,
            string csvText,
            string sourceName,
            CancellationToken ct)
        {
            var parsed = _parser.Parse(csvText);

            var insertResult = await _writer.InsertImportedAsync(
                accountId: accountId,
                source: sourceName,
                parsed: parsed,
                ct: ct);

            return new TransactionImportResult(
                Parsed: parsed.Count,
                Inserted: insertResult.Inserted,
                SkippedAsDuplicate: insertResult.SkippedAsDuplicate);
        }
    }
}

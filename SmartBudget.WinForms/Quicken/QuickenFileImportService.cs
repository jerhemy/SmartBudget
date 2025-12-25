using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SmartBudget.WinForms.Quicken;
public sealed class QuickenFileImportService
{
    private readonly IAccountRepository _accounts;
    private readonly ITransactionRepository _transactions;

    public QuickenFileImportService(IAccountRepository accounts, ITransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public async Task ImportAsync(IReadOnlyList<ImportedStatement> statements, CancellationToken ct)
    {
        //foreach (var st in statements)
        //{
        //    var account = await _accounts.GetByExternalKeyAsync(st.ExternalAccountKey, ct);

        //    if (account is null)
        //    {
        //        // Pick a deterministic friendly name if you want
        //        var name = string.IsNullOrWhiteSpace(st.BankId)
        //            ? $"{st.AccountType} {Mask(st.AccountId)}"
        //            : $"{st.BankId} {st.AccountType} {Mask(st.AccountId)}";

        //        account = await _accounts.CreateAsync(new CreateAccount(
        //            Name: name,
        //            ExternalKey: st.ExternalAccountKey), ct);
        //    }

        //    foreach (var t in st.Transactions)
        //    {
        //        var externalId = t.FitId; // best option :contentReference[oaicite:4]{index=4}
        //        var externalHash = externalId is null ? ComputeFallbackHash(t) : null;

        //        // repository should do INSERT OR IGNORE (or handle unique constraint)
        //        await _transactions.InsertImportedAsync(new InsertImportedTransaction(
        //            AccountId: account.Id,
        //            PostedDate: t.PostedDate,
        //            Amount: t.Amount,
        //            Title: t.Name ?? t.Memo ?? "(no description)",
        //            Memo: t.Memo,
        //            Source: "OFX",
        //            ExternalId: externalId,
        //            ExternalHash: externalHash), ct);
        //    }
        //}
    }

    private static string ComputeFallbackHash(QuickenImportedTransaction t)
    {
        var input = $"{t.PostedDate:yyyy-MM-dd}|{t.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}|{t.Name}|{t.Memo}|{t.CheckNumber}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static string Mask(string acctId)
        => acctId.Length <= 4 ? acctId : acctId[^4..].PadLeft(acctId.Length, '•');
}
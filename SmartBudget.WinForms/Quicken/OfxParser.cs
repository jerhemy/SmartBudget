using System.Globalization;
using System.Xml.Linq;

namespace SmartBudget.WinForms.Quicken;

public sealed class OfxParser
{
    public IReadOnlyList<ImportedStatement> Parse(string fileText)
    {
        var xml = OfxSgmlToXml.ConvertToXml(fileText);
        var doc = XDocument.Parse(xml);

        // BANKMSGSRSV1 / STMTTRNRS / STMTRS
        var stmtrs = doc.Descendants("STMTRS").ToList();
        var results = new List<ImportedStatement>(stmtrs.Count);

        foreach (var s in stmtrs)
        {
            var bankAcct = s.Element("BANKACCTFROM");
            var ccAcct = s.Element("CCACCTFROM");

            var (kind, bankId, acctId, acctType) =
                bankAcct is not null
                    ? ("BANK",
                        bankAcct.Element("BANKID")?.Value ?? "",
                        bankAcct.Element("ACCTID")?.Value ?? "",
                        bankAcct.Element("ACCTTYPE")?.Value ?? "")
                    : ("CC",
                        "", // credit cards typically won’t have BANKID
                        ccAcct?.Element("ACCTID")?.Value ?? "",
                        "CREDITCARD");

            if (string.IsNullOrWhiteSpace(acctId))
                throw new FormatException("Missing ACCTID in statement.");

            var externalKey = $"{kind}|{bankId}|{acctId}|{acctType}";

            var txns = s.Descendants("STMTTRN")
                .Select(MapTxn)
                .ToList();

            results.Add(new ImportedStatement(
                ExternalAccountKey: externalKey,
                BankId: bankId,
                AccountId: acctId,
                AccountType: acctType,
                Transactions: txns));
        }

        return results;
    }

    private static QuickenImportedTransaction MapTxn(XElement e)
    {
        var fitid = e.Element("FITID")?.Value?.Trim();

        var posted = ParseOfxDate(e.Element("DTPOSTED")?.Value);
        var amount = decimal.Parse(e.Element("TRNAMT")?.Value ?? "0", CultureInfo.InvariantCulture);

        var name = e.Element("NAME")?.Value?.Trim();
        var memo = e.Element("MEMO")?.Value?.Trim();
        var checkNum = e.Element("CHECKNUM")?.Value?.Trim();

        return new QuickenImportedTransaction(
            PostedDate: posted,
            Amount: amount,
            FitId: string.IsNullOrWhiteSpace(fitid) ? null : fitid,
            Name: name,
            Memo: memo,
            CheckNumber: checkNum);
    }

    // OFX dates often look like YYYYMMDD or YYYYMMDDHHMMSS[0:GMT]
    private static DateOnly ParseOfxDate(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return DateOnly.MinValue;

        var digits = new string(v.TakeWhile(char.IsDigit).ToArray());
        if (digits.Length < 8) return DateOnly.MinValue;

        var y = int.Parse(digits.Substring(0, 4), CultureInfo.InvariantCulture);
        var m = int.Parse(digits.Substring(4, 2), CultureInfo.InvariantCulture);
        var d = int.Parse(digits.Substring(6, 2), CultureInfo.InvariantCulture);
        return new DateOnly(y, m, d);
    }
}
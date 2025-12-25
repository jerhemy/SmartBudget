using SmartBudget.WinForms.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SmartBudget.Infrastructure
{
    /// <summary>
    /// Parser for 5-column bank exports like Checking2.csv:
    /// Date, Amount, Status(*), CheckNumber(optional), Description
    /// No header row.
    /// </summary>
    public sealed class CheckingCsvParser : IBankCsvTransactionParser
    {
        private static readonly string[] DateFormats =
        [
            "M/d/yyyy", "MM/dd/yyyy",
        "M/d/yy", "MM/dd/yy"
        ];

        public IReadOnlyList<ImportedTransaction> Parse(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText))
                return Array.Empty<ImportedTransaction>();

            var result = new List<ImportedTransaction>();
            using var sr = new StringReader(csvText);

            string? line;
            var lineNo = 0;

            while ((line = sr.ReadLine()) is not null)
            {
                lineNo++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var fields = ParseCsvLine(line);

                // Expected 5 columns; if banks change format, fail loudly so you notice.
                if (fields.Count != 5)
                {
                    throw new FormatException(
                        $"CSV line {lineNo} expected 5 fields but found {fields.Count}. Line: {line}");
                }

                var dateText = fields[0].Trim();
                var amountText = fields[1].Trim();
                var statusText = fields[2].Trim();
                var checkText = string.IsNullOrWhiteSpace(fields[3]) ? null : fields[3].Trim();
                var description = fields[4].Trim();

                var postedDate = ParseDate(dateText, lineNo);
                var amount = ParseAmount(amountText, lineNo);
                var isCleared = statusText == "*";

                // Hash should be stable across imports and independent of whitespace differences.
                var importHash = ComputeImportHash(postedDate, amount, description);

                result.Add(new ImportedTransaction(
                    PostedDate: postedDate,
                    Amount: amount,
                    Description: description,
                    IsCleared: isCleared,
                    CheckNumber: checkText,
                    SourceLineNumber: lineNo,
                    ImportHash: importHash));
            }

            return result;
        }

        private static DateOnly ParseDate(string input, int lineNo)
        {
            if (DateOnly.TryParseExact(
                    input,
                    DateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                return date;
            }

            // Some exports use current culture, so try en-US as a fallback.
            if (DateOnly.TryParse(input, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.None, out date))
                return date;

            throw new FormatException($"CSV line {lineNo}: invalid date '{input}'.");
        }

        private static decimal ParseAmount(string input, int lineNo)
        {
            // Handle common bank formats: "-22.62", "22.62", "$22.62", "1,234.56", "(22.62)"
            var s = input.Trim();

            var negative = false;
            if (s.StartsWith("(", StringComparison.Ordinal) && s.EndsWith(")", StringComparison.Ordinal))
            {
                negative = true;
                s = s[1..^1];
            }

            s = s.Replace("$", "", StringComparison.Ordinal)
                 .Replace(",", "", StringComparison.Ordinal)
                 .Trim();

            if (!decimal.TryParse(
                    s,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new FormatException($"CSV line {lineNo}: invalid amount '{input}'.");
            }

            return negative ? -value : value;
        }

        private static string ComputeImportHash(DateOnly date, decimal amount, string description)
        {
            // Normalize description to reduce duplicate misses due to spacing.
            var descNorm = NormalizeSpaces(description).ToUpperInvariant();
            var payload = $"{date:yyyy-MM-dd}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}|{descNorm}";

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(bytes); // 64 hex chars
        }

        private static string NormalizeSpaces(string s)
        {
            var sb = new StringBuilder(s.Length);
            var prevWasSpace = false;

            foreach (var ch in s.Trim())
        {
                var isSpace = char.IsWhiteSpace(ch);
                if (isSpace)
                {
                    if (!prevWasSpace) sb.Append(' ');
                    prevWasSpace = true;
                }
                else
                {
                    sb.Append(ch);
                    prevWasSpace = false;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Minimal CSV parser that supports:
        /// - quoted fields
        /// - commas inside quotes
        /// - escaped quotes ("")
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>(8);
            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Escaped quote
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            fields.Add(sb.ToString());
            return fields;
        }
    }
}

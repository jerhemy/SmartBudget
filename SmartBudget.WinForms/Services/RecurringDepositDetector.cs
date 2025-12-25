using SmartBudget.Recurring;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SmartBudget.WinForms.Services;

public sealed record DetectedRecurringDeposit(
    string EmployerKey,
    string SeriesKey,
    string DisplayName,
    int Count,
    int AvgAmountCents,
    string Cadence,      // Weekly / Biweekly / Monthly
    double Confidence,   // 0..1
    DateOnly FirstSeen,
    DateOnly LastSeen
);

public static class RecurringDepositDetector
{
    private sealed record Item(AutoPayTxn Txn, string EmployerKey, string SeriesKey, HashSet<string> Tokens);

    private sealed record CadenceFit(string Name, int MinDays, int MaxDays);

    private static readonly CadenceFit[] Cadences =
    {
    new("Weekly",      6,  8),
    new("Biweekly",   12, 16),
    new("Every3Weeks", 20, 22),
    new("Every4Weeks", 27, 29),
    new("Monthly",    28, 35),
};

    public static IReadOnlyList<DetectedRecurringDeposit> Detect(
        IReadOnlyList<AutoPayTxn> txns,
        int minOccurrences = 4,
        double minConfidence = 0.75)
    {
        if (txns.Count == 0)
            return Array.Empty<DetectedRecurringDeposit>();

        // Only deposits
        var items = txns
            .Where(t => t.AmountCents > 0 && !string.IsNullOrWhiteSpace(t.Title))
            .Select(t =>
            {
                var tokens = TokenizeDeposit(t.Title);
                var employerKey = BuildEmployerKey(tokens);
                var seriesKey = employerKey;
                return new Item(t, employerKey, seriesKey, tokens);
            })
            .ToArray();

        var groups = items
            .GroupBy(i => i.SeriesKey, StringComparer.Ordinal)
            .Select(g => g.OrderBy(x => x.Txn.Date).ToArray())
            .ToArray();

        var results = new List<DetectedRecurringDeposit>();

        // ============================
        // MAIN LOOP (this one)
        // ============================
        foreach (var series in groups)
        {
            if (series.Length < minOccurrences)
                continue;

            // Safety: must be mostly deposits
            if (series.Count(x => x.Txn.AmountCents > 0) < series.Length * 0.9)
                continue;

            // -------- cadence detection --------
            var (cadenceName, cadenceScore) = BestCadenceScore(series);
            if (cadenceScore < 0.50)
                continue;

            // -------- amount consistency --------
            var amountScore = AmountConsistencyScore(series);

            // -------- frequency --------
            var freqScore = Math.Min(series.Length / 8.0, 1.0);

            // -------- text hints --------
            var hint = 0.0;
            if (series.Any(s => s.Tokens.Contains("deposit"))) hint += 0.06;
            if (series.Any(s => s.Tokens.Contains("payroll"))) hint += 0.06;
            if (series.Any(s => s.Tokens.Contains("direct"))) hint += 0.04;

            // -------- final confidence --------
            var confidence =
                0.50 * cadenceScore +
                0.25 * amountScore +
                0.25 * freqScore +
                hint;

            confidence = Math.Min(confidence, 1.0);

            if (confidence < minConfidence)
                continue;

            // -------- build result --------
            var avg = (int)Math.Round(
                series.Select(x => x.Txn.AmountCents).Average(),
                MidpointRounding.AwayFromZero);

            results.Add(new DetectedRecurringDeposit(
                EmployerKey: series[0].EmployerKey,
                SeriesKey: series[0].SeriesKey,
                DisplayName: MostCommonTitle(series),
                Count: series.Length,
                AvgAmountCents: avg,
                Cadence: cadenceName,
                Confidence: Math.Round(confidence, 3),
                FirstSeen: series.First().Txn.Date,
                LastSeen: series.Last().Txn.Date
            ));
        }

        return results
            .OrderByDescending(r => r.Confidence)
            .ThenByDescending(r => r.Count)
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }


    // ---------------- helpers ----------------

    private static int[] ComputeDayGaps(DateOnly[] datesAsc)
    {
        if (datesAsc.Length < 2) return Array.Empty<int>();
        var gaps = new int[datesAsc.Length - 1];
        for (int i = 1; i < datesAsc.Length; i++)
            gaps[i - 1] = datesAsc[i].DayNumber - datesAsc[i - 1].DayNumber;
        return gaps;
    }

    private static (string cadence, double score) BestCadenceScore(Item[] series)
    {
        var datesAsc = series.Select(s => s.Txn.Date).OrderBy(d => d).ToArray();
        var gaps = ComputeDayGaps(datesAsc);
        if (gaps.Length == 0) return ("Unknown", 0);

        // Start with semi-monthly (special)
        var bestName = "SemiMonthly";
        var bestScore = SemiMonthlyScore(series);

        // Compare against fixed-gap cadences
        foreach (var c in Cadences)
        {
            var hits = gaps.Count(d => d >= c.MinDays && d <= c.MaxDays);
            var score = hits / (double)gaps.Length;

            if (score > bestScore)
            {
                bestScore = score;
                bestName = c.Name;
            }
        }

        return (bestName, bestScore);
    }

    private static double AmountConsistencyScore(Item[] series)
    {
        var amounts = series.Select(s => s.Txn.AmountCents).OrderBy(x => x).ToArray();
        if (amounts.Length < 3) return 0;

        var median = amounts[amounts.Length / 2];
        if (median <= 0) return 0;

        // within $5 or 2%
        var within = amounts.Count(a =>
        {
            var diff = Math.Abs(a - median);
            return diff <= 500 || diff / (double)median <= 0.02;
        });

        return within / (double)amounts.Length;
    }

    private static string MostCommonTitle(Item[] series)
        => series.GroupBy(s => s.Txn.Title.Trim(), StringComparer.OrdinalIgnoreCase)
                 .OrderByDescending(g => g.Count())
                 .First().Key;

    private static HashSet<string> TokenizeDeposit(string title)
    {
        Span<char> buffer = stackalloc char[title.Length];
        int n = 0;
        foreach (var ch in title)
        {
            var c = char.ToLowerInvariant(ch);
            buffer[n++] = char.IsLetter(c) ? c : ' ';
        }

        var cleaned = new string(buffer[..n]);
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // deposit-focused stopwords
        var stop = new HashSet<string>(StringComparer.Ordinal)
        {
            "corp","co","inc","llc","ltd"
        };

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in parts)
        {
            if (p.Length <= 1) continue;
            if (stop.Contains(p)) continue;
            tokens.Add(p);
        }

        return tokens;
    }

    private static string BuildEmployerKey(HashSet<string> tokens)
    {
        // Prefer longest meaningful tokens to identify employer
        var core = tokens
            .Where(t => t is not ("deposit" or "direct" or "payroll"))
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.Ordinal)
            .Take(3)
            .ToArray();

        if (core.Length == 0)
            core = tokens.OrderBy(t => t, StringComparer.Ordinal).Take(2).ToArray();

        return string.Join(' ', core);
    }

    private static double SemiMonthlyScore(Item[] series)
    {
        // Two signals:
        // 1) Gap pattern: many gaps in 13–17 days
        // 2) DOM clusters: many payments fall into two “pay windows”
        var datesAsc = series.Select(s => s.Txn.Date).OrderBy(d => d).ToArray();
        var gaps = ComputeDayGaps(datesAsc);
        if (gaps.Length == 0) return 0;

        var gapHits = gaps.Count(d => d >= 13 && d <= 17);
        var gapScore = gapHits / (double)gaps.Length;

        // DOM window score: common semi-monthly “pay windows”
        // This is intentionally broad to handle 1st/15th and 15th/last-business-day.
        var doms = series.Select(s => s.Txn.Date.Day).ToArray();

        bool InWindowA(int d) => d >= 1 && d <= 6;     // early-month
        bool InWindowB(int d) => d >= 13 && d <= 18;   // mid-month
        bool InWindowC(int d) => d >= 24 && d <= 31;   // late-month

        // Accept (A+B) or (B+C) as typical two-pay windows
        int hitsAB = doms.Count(d => InWindowA(d) || InWindowB(d));
        int hitsBC = doms.Count(d => InWindowB(d) || InWindowC(d));

        var domScore = Math.Max(hitsAB, hitsBC) / (double)doms.Length;

        // Semi-monthly confidence is the better of the two signals (or combine)
        return Math.Max(gapScore, domScore);
    }
}

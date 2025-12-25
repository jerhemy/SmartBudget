// Project: SmartBudget.Application
// File: Recurring/AutoPayDetector.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SmartBudget.Recurring;

public sealed record AutoPayTxn(
    long Id,
    long AccountId,
    DateOnly Date,
    string Title,
    double AmountCents // signed
);

public sealed record DetectedAutoPay(
    string MerchantKey,
    string SeriesKey,
    string DisplayName,
    int Count,
    long AvgAmountCents,
    string Cadence,          // "Monthly"
    double Confidence,       // 0..1
    DateOnly FirstSeen,
    DateOnly LastSeen
);

public static class AutoPayDetector
{
    /// <summary>
    /// Detect monthly auto-pay series and return an average amount for each series.
    /// Designed to catch cases like:
    /// - "HOME DEPOT AUTO PYMT" vs "HOME DEPOT ONLINE PMT"
    /// - "CITY PHX WATER PAYMENT" that doesn't say "AUTO" but happens around 28th-1st
    /// </summary>
    public static IReadOnlyList<DetectedAutoPay> DetectMonthlyAutoPays(
        IReadOnlyList<AutoPayTxn> txns,
        int minOccurrences = 4,
        double minConfidence = 0.75)
    {
        if (txns.Count == 0) return Array.Empty<DetectedAutoPay>();

        // Build features for grouping
        var items = txns
            .Where(t => !string.IsNullOrWhiteSpace(t.Title))
            .Select(t =>
            {
                var tokens = Tokenize(t.Title);
                var merchantKey = BuildMerchantKey(tokens);
                var seriesKey = BuildSeriesKey(merchantKey, tokens);
                return new Item(t, merchantKey, seriesKey, tokens);
            })
            .ToArray();

        // Group by series (merchant + qualifier tokens)
        var bySeries = items
            .GroupBy(i => i.SeriesKey, StringComparer.Ordinal)
            .Select(g => g.OrderBy(x => x.Txn.Date).ToArray())
            .ToArray();

        // Score each series as "monthly auto pay"
        var scored = new List<(string MerchantKey, string SeriesKey, string DisplayName, Item[] Items, double Confidence)>();
        scored = MergeDriftedSeries(scored);

        foreach (var series in bySeries)
        {
            if (series.Length < minOccurrences)
                continue;

            // Monthly gap score: gaps between 28-35 days
            var gaps = ComputeDayGaps(series.Select(s => s.Txn.Date).ToArray());
            if (gaps.Length == 0)
                continue;

            var isPaymentLike = IsPaymentLike(series);

            var minGap = isPaymentLike ? 25 : 28;
            var maxGap = isPaymentLike ? 40 : 35;

            var monthlyGapScore = gaps.Count(d => d >= minGap && d <= maxGap) / (double)gaps.Length;

            // Day-of-month window score:
            //  - either clustered around a dominant DOM (±2)
            //  - or clustered in month-boundary window (26-31 OR 1-4)
            var doms = series.Select(s => s.Txn.Date.Day).ToArray();

            var domTol = isPaymentLike ? 5 : 2;
            var domScore = DominantDomScore(doms, tolerance: domTol);
            var monthBoundaryScore = MonthBoundaryWindowScore(doms);
            var effectiveDomScore = Math.Max(domScore, monthBoundaryScore);

            // Frequency score (cap at 6)
            var frequencyScore = Math.Min(series.Length / 6.0, 1.0);

            // Text hint (small bonus) - not required
            var textHint = series.Any(s => s.Tokens.Contains("auto")) ? 0.08 : 0.0;


            // Final confidence (tuned for monthly recurring)
            var confidence =
                0.50 * monthlyGapScore +
                0.30 * effectiveDomScore +
                0.20 * frequencyScore +
                textHint;

            confidence = Math.Min(confidence, 1.0);

            if (confidence < minConfidence)
                continue;

            var merchantKey = series[0].MerchantKey;
            var seriesKey = series[0].SeriesKey;
            var displayName = BuildDisplayName(series);

            scored.Add((merchantKey, seriesKey, displayName, series, confidence));
        }

        // If multiple series share the same merchant, keep the strongest as "autopay"
        // and allow others only if they ALSO pass threshold strongly.
        // This prevents "ONLINE PMT" extras from being labeled as autopay when AUTO series exists.
        var bestByMerchant = scored
            .GroupBy(s => s.MerchantKey, StringComparer.Ordinal)
            .SelectMany(g =>
            {
                var ordered = g.OrderByDescending(x => x.Confidence).ToArray();

                if (ordered.Length == 1)
                    return (IEnumerable<(string MerchantKey, string SeriesKey, string DisplayName, Item[] Items, double Confidence)>)ordered;

                var best = ordered[0];
                var keep = new List<(string MerchantKey, string SeriesKey, string DisplayName, Item[] Items, double Confidence)>
                {
                    best
                };

                foreach (var other in ordered.Skip(1))
                {
                    var countBest = best.Items.Length;
                    var countOther = other.Items.Length;

                    var dropAsExtra =
                        (best.Confidence - other.Confidence) >= 0.20 &&
                        countOther < countBest;

                    if (!dropAsExtra && other.Confidence >= (minConfidence + 0.10))
                        keep.Add(other);
                }

                return keep;
            })
            .ToArray();

        // Produce results with average
        var results = bestByMerchant
            .Select(s =>
            {
                var amounts = s.Items.Select(x => x.Txn.AmountCents).ToArray();
                var avg = (int)Math.Round(amounts.Average(), MidpointRounding.AwayFromZero);

                return new DetectedAutoPay(
                    MerchantKey: s.MerchantKey,
                    SeriesKey: s.SeriesKey,
                    DisplayName: s.DisplayName,
                    Count: s.Items.Length,
                    AvgAmountCents: avg,
                    Cadence: "Monthly",
                    Confidence: Math.Round(s.Confidence, 3),
                    FirstSeen: s.Items.First().Txn.Date,
                    LastSeen: s.Items.Last().Txn.Date
                );
            })
            .OrderByDescending(r => r.Confidence)
            .ThenByDescending(r => r.Count)
            .ThenBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return results;
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private sealed record Item(AutoPayTxn Txn, string MerchantKey, string SeriesKey, HashSet<string> Tokens);

    private static int[] ComputeDayGaps(DateOnly[] datesAsc)
    {
        if (datesAsc.Length < 2) return Array.Empty<int>();
        var gaps = new int[datesAsc.Length - 1];
        for (int i = 1; i < datesAsc.Length; i++)
        {
            gaps[i - 1] = datesAsc[i].DayNumber - datesAsc[i - 1].DayNumber;
        }
        return gaps;
    }

    // Score based on dominant day-of-month ± tolerance
    private static double DominantDomScore(int[] doms, int tolerance)
    {
        if (doms.Length == 0) return 0;

        // Find dominant DOM by frequency
        var counts = new Dictionary<int, int>();
        foreach (var d in doms)
            counts[d] = counts.TryGetValue(d, out var c) ? c + 1 : 1;

        var dominant = counts.OrderByDescending(kv => kv.Value).First().Key;

        int aligned = 0;
        foreach (var d in doms)
        {
            if (Math.Abs(dominant - d) <= tolerance)
                aligned++;
        }

        return aligned / (double)doms.Length;
    }

    // Month-boundary window (captures 28–31 + 1–4 patterns)
    private static double MonthBoundaryWindowScore(int[] doms)
    {
        if (doms.Length == 0) return 0;
        int hits = 0;
        foreach (var d in doms)
        {
            if (d >= 26 || d <= 4) hits++;
        }
        return hits / (double)doms.Length;
    }

    private static HashSet<string> Tokenize(string title)
    {
        // Lowercase, keep letters, convert other to spaces.
        // No regex to keep it deterministic and fast.
        Span<char> buffer = stackalloc char[title.Length];
        int n = 0;

        foreach (var ch in title)
        {
            var c = char.ToLowerInvariant(ch);
            buffer[n++] = char.IsLetter(c) ? c : ' ';
        }

        var cleaned = new string(buffer[..n]);
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Remove common noise tokens
        var stop = new HashSet<string>(StringComparer.Ordinal)
        {
            "pos","visa","debit","credit","ach","onlinebanking","purchase","payment","pmt","pymt","transaction", "ret"
        };

        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in parts)
        {
            if (p.Length <= 1) continue;
            if (stop.Contains(p)) continue;
            tokens.Add(p);
        }

        // NOTE: We intentionally do NOT remove "auto" or "online"
        // because they help split series.
        return tokens;
    }

    private static string BuildMerchantKey(HashSet<string> tokens)
    {
        // Use the first 2-3 “strongest” tokens as merchant.
        // Heuristic: pick the longest tokens (tends to favor meaningful words).
        var merchant = tokens
            .Where(t => t is not ("auto" or "online" or "recurring"))
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.Ordinal)
            .Take(3)
            .ToArray();

        // Fallback if title is tiny
        if (merchant.Length == 0)
            merchant = tokens.OrderBy(t => t, StringComparer.Ordinal).Take(2).ToArray();

        return string.Join(' ', merchant);
    }

    private static string BuildSeriesKey(string merchantKey, HashSet<string> tokens)
    {
        // Qualifier tokens that often separate “autopay” vs “manual/extra”
        // You can expand this list as you observe your data.
        var qualifiers = new[]
        {
            "auto", "online", "billpay", "web", "app", "card", "phone", "kiosk"
        };

        var picked = qualifiers.Where(tokens.Contains).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        return picked.Length == 0
            ? merchantKey
            : merchantKey + " | " + string.Join(" | ", picked);
    }

    private static string BuildDisplayName(Item[] series)
    {
        // Prefer a representative real title (most frequent title)
        var top = series
            .GroupBy(s => s.Txn.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .First().Key;

        return top;
    }

    private static List<(string MerchantKey, string SeriesKey, string DisplayName, Item[] Items, double Confidence)> MergeDriftedSeries(
    List<(string MerchantKey, string SeriesKey, string DisplayName, Item[] Items, double Confidence)> scored)
    {
        // Greedy merge: repeatedly merge the closest pairs until no changes.
        // This is deterministic if we sort by key.
        scored = scored
            .OrderBy(s => s.SeriesKey, StringComparer.Ordinal)
            .ThenBy(s => s.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        bool merged;
        do
        {
            merged = false;

            for (int i = 0; i < scored.Count && !merged; i++)
            {
                for (int j = i + 1; j < scored.Count && !merged; j++)
                {
                    var A = scored[i];
                    var B = scored[j];

                    // Require same direction (both mostly charges or deposits)
                    bool aNeg = A.Items.Count(x => x.Txn.AmountCents < 0) >= (A.Items.Length * 0.8);
                    bool bNeg = B.Items.Count(x => x.Txn.AmountCents < 0) >= (B.Items.Length * 0.8);
                    if (aNeg != bNeg) continue;

                    // Require both to look monthly-ish
                    if (MonthlyGapScore(A.Items) < 0.60) continue;
                    if (MonthlyGapScore(B.Items) < 0.60) continue;

                    // Amount similarity (loans should be tight)
                    var aMed = MedianAbsAmountCents(A.Items);
                    var bMed = MedianAbsAmountCents(B.Items);

                    var diff = Math.Abs(aMed - bMed);
                    var diffPct = aMed == 0 ? 1.0 : diff / (double)aMed;

                    // Accept either tight dollars OR tight percent
                    if (!(diff <= 500 || diffPct <= 0.02)) // $5 or 2%
                        continue;

                    // DOM window overlap
                    if (DomWindowOverlapScore(A.Items, B.Items) < 0.50)
                        continue;

                    // Text similarity (token overlap) - allow drift like "RET"
                    var tokensA = A.Items.SelectMany(x => x.Tokens).ToHashSet(StringComparer.Ordinal);
                    var tokensB = B.Items.SelectMany(x => x.Tokens).ToHashSet(StringComparer.Ordinal);

                    var textSim = Jaccard(tokensA, tokensB);
                    if (textSim < 0.35) // Nissan vs NISSAN RET should pass
                        continue;

                    // Merge!
                    var mergedItems = A.Items.Concat(B.Items)
                        .OrderBy(x => x.Txn.Date)
                        .ToArray();

                    var mergedDisplay = A.DisplayName.Length >= B.DisplayName.Length ? A.DisplayName : B.DisplayName;
                    var mergedKey = A.MerchantKey.Length >= B.MerchantKey.Length ? A.MerchantKey : B.MerchantKey;

                    var mergedSeriesKey = mergedKey; // collapse to one
                    var mergedConfidence = Math.Max(A.Confidence, B.Confidence);

                    scored[i] = (mergedKey, mergedSeriesKey, mergedDisplay, mergedItems, mergedConfidence);
                    scored.RemoveAt(j);

                    merged = true;
                }
            }

        } while (merged);

        return scored;
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count == 0 && b.Count == 0) return 1.0;
        if (a.Count == 0 || b.Count == 0) return 0.0;

        int intersect = 0;
        foreach (var x in a)
            if (b.Contains(x)) intersect++;

        int union = a.Count + b.Count - intersect;
        return union == 0 ? 0.0 : intersect / (double)union;
    }

    private static double Median(double[] values)
    {
        if (values.Length == 0) return 0;
        var tmp = values.ToArray();
        Array.Sort(tmp);
        var mid = tmp.Length / 2;
        return tmp.Length % 2 == 1 ? tmp[mid] : (tmp[mid - 1] + tmp[mid]) / 2;
    }

    private static double MedianAbsAmountCents(Item[] items)
    {
        var vals = items.Select(i => Math.Abs(i.Txn.AmountCents)).ToArray();
        return Median(vals);
    }

    private static double MonthlyGapScore(Item[] items)
    {
        var dates = items.Select(i => i.Txn.Date).OrderBy(d => d).ToArray();
        var gaps = ComputeDayGaps(dates);
        if (gaps.Length == 0) return 0;
        return gaps.Count(d => d >= 28 && d <= 35) / (double)gaps.Length;
    }

    private static double DomWindowOverlapScore(Item[] a, Item[] b)
    {
        // Use simple windows: month-boundary + dominant DOM ±2.
        // We compute a set of "allowed DOMs" for each series and see overlap.
        static HashSet<int> AllowedDoms(Item[] items)
        {
            var doms = items.Select(x => x.Txn.Date.Day).ToArray();
            var set = new HashSet<int>();

            // month boundary window
            foreach (var d in doms)
                if (d >= 26 || d <= 4) set.Add(d);

            // dominant dom ±2
            var counts = new Dictionary<int, int>();
            foreach (var d in doms)
                counts[d] = counts.TryGetValue(d, out var c) ? c + 1 : 1;

            var dom = counts.OrderByDescending(kv => kv.Value).First().Key;
            for (int k = dom - 2; k <= dom + 2; k++)
                if (k >= 1 && k <= 31) set.Add(k);

            return set;
        }

        var A = AllowedDoms(a);
        var B = AllowedDoms(b);

        if (A.Count == 0 || B.Count == 0) return 0;

        int inter = 0;
        foreach (var d in A)
            if (B.Contains(d)) inter++;

        return inter / (double)Math.Min(A.Count, B.Count);
    }

    private static bool IsPaymentLike(Item[] series)
    {
        // token presence (depends on your tokenizer)
        // Also fall back to title contains to be safe.
        return series.Any(s =>
            s.Tokens.Contains("amex") ||
            s.Tokens.Contains("epayment") ||
            s.Tokens.Contains("ach") ||
            s.Tokens.Contains("pmt") ||
            s.Tokens.Contains("pymt") ||
            s.Tokens.Contains("payment") ||
            s.Txn.Title.Contains("EPAYMENT", StringComparison.OrdinalIgnoreCase) ||
            s.Txn.Title.Contains("ACH", StringComparison.OrdinalIgnoreCase) ||
            s.Txn.Title.Contains("PMT", StringComparison.OrdinalIgnoreCase) ||
            s.Txn.Title.Contains("PAYMENT", StringComparison.OrdinalIgnoreCase));
    }
}
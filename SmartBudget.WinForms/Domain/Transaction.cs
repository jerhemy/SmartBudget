namespace SmartBudget.Domain;
public sealed class Transaction
{
    public long Id { get; }
    public long AccountId { get; }

    /// <summary>
    /// Local calendar date (stored as 'YYYY-MM-DD' in SQLite).
    /// </summary>
    public DateOnly TxnDate { get; }

    public string Title { get; }
    public string? Description { get; }
    public string? Category { get; }

    /// <summary>
    /// Signed amount in cents. Deposits are +, charges are -.
    /// </summary>
    public long AmountCents { get; }

    public decimal Amount => AmountCents / 100m;

    public bool IsCleared { get; }
    public DateTime CreatedUtc { get; }

    /// <summary>
    /// Origin of transaction, e.g. "manual", "OFX", "CSV".
    /// Always non-null; default is "manual".
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// External transaction id for imports, e.g. OFX/QFX FITID.
    /// Used as the primary de-dupe key when present.
    /// </summary>
    public string? ExternalId { get; }

    /// <summary>
    /// Stable fallback hash when ExternalId is missing.
    /// Unique per (AccountId, Source) in DB.
    /// </summary>
    public string? ImportHash { get; }

    /// <summary>
    /// Optional check number from OFX <CHECKNUM> or user entry.
    /// </summary>
    public string? CheckNumber { get; }

    public Transaction(
        long id,
        long accountId,
        DateOnly txnDate,
        string title,
        string? description,
        string? category,
        long amountCents,
        bool isCleared,
        DateTime createdUtc,
        string source,
        string? externalId,
        string? importHash,
        string? checkNumber)
    {
        Id = id;
        AccountId = accountId;

        TxnDate = txnDate;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description;
        Category = category;

        AmountCents = amountCents;
        IsCleared = isCleared;
        CreatedUtc = createdUtc;

        Source = string.IsNullOrWhiteSpace(source) ? "manual" : source;
        ExternalId = string.IsNullOrWhiteSpace(externalId) ? null : externalId;
        ImportHash = string.IsNullOrWhiteSpace(importHash) ? null : importHash;
        CheckNumber = string.IsNullOrWhiteSpace(checkNumber) ? null : checkNumber;
    }
}
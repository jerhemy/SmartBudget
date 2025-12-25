namespace SmartBudget.Domain;
public sealed class Account
{
    public long Id { get; }
    public string Name { get; }
    public long OpeningBalanceCents { get; }
    public string CurrencyCode { get; }
    public bool IsActive { get; }
    public DateTime CreatedUtc { get; }

    /// <summary>
    /// External identity for imported statements (OFX/QFX).
    /// Example: "BANK|{BANKID}|{ACCTID}|{ACCTTYPE}" or "CC||{ACCTID}|CREDITCARD"
    /// </summary>
    public string? ExternalKey { get; }

    public Account(
        long id,
        string name,
        long openingBalanceCents,
        string currencyCode,
        bool isActive,
        DateTime createdUtc,
        string? externalKey)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        OpeningBalanceCents = openingBalanceCents;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode;
        IsActive = isActive;
        CreatedUtc = createdUtc;
        ExternalKey = string.IsNullOrWhiteSpace(externalKey) ? null : externalKey;
    }
}
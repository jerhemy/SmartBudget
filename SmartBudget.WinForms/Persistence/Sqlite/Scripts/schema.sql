PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS [accounts] (
    id                      INTEGER PRIMARY KEY,
    name                    TEXT    NOT NULL,
    opening_balance_cents   INTEGER NOT NULL DEFAULT 0,
    currency_code           TEXT    NOT NULL DEFAULT 'USD',
    is_active               INTEGER NOT NULL DEFAULT 1,
    created_utc             TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),

    -- OFX/QFX identity for auto-create / upsert
    -- Example: "BANK|{BANKID}|{ACCTID}|{ACCTTYPE}" or "CC||{ACCTID}|CREDITCARD"
    external_key            TEXT    NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_accounts_external_key ON accounts(external_key)WHERE external_key IS NOT NULL;


-- Signed amount:
--   deposits are +, charges are -
CREATE TABLE IF NOT EXISTS [transactions] (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id         INTEGER NOT NULL,
    txn_date           TEXT    NOT NULL,  -- 'YYYY-MM-DD' (treat as local calendar date)
    description        TEXT    NOT NULL,
    memo               TEXT    NULL,
    category           TEXT    NULL,
    amount_cents       INTEGER NOT NULL,  -- signed
    is_cleared         INTEGER NOT NULL DEFAULT 0,
    created_utc        TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
    recurring_id       INTEGER NULL,  -- FK to recurring_transaction.id when applicable

    -- Import tracking / de-dupea
    source             TEXT    NOT NULL DEFAULT 'manual', -- e.g. 'OFX', 'CSV', 'manual'
    external_id        TEXT    NULL,  -- OFX <FITID> when present
    import_hash        TEXT    NULL,  -- fallback stable hash when external_id missing
    check_number       TEXT    NULL,  -- optional: OFX <CHECKNUM> if present

    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_transaction_account_date
ON transactions(account_id, txn_date);

CREATE INDEX IF NOT EXISTS ix_transaction_account_created
ON transactions(account_id, created_utc);

-- Best de-dupe: FITID per account + source
CREATE UNIQUE INDEX IF NOT EXISTS ux_transactions_accounts_source_external_id
ON transactions(account_id, source, external_id)
WHERE external_id IS NOT NULL;

-- Fallback de-dupe when FITID missing
CREATE UNIQUE INDEX IF NOT EXISTS ux_transactions_accounts_source_import_hash
ON transactions(account_id, source, import_hash)
WHERE import_hash IS NOT NULL;

CREATE TABLE IF NOT EXISTS [recurring] (
    id                 INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id         INTEGER NOT NULL,

    FOREIGN KEY (account_id) REFERENCES accounts(id) ON DELETE CASCADE
);
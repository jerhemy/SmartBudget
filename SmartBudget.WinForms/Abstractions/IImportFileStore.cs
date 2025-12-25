namespace SmartBudget.WinForms.Abstractions;

/// <summary>
/// Stores imported source documents (PDF/QFX/CSV) into the app data directory,
/// using content-addressed storage (hash-based) to enable dedupe and easy rebuild.
/// </summary>
public interface IImportFileStore
{
    /// <summary>
    /// Copies the file to the app's import storage and returns metadata.
    /// If the same file (same SHA-256) already exists, it is not copied again.
    /// </summary>
    Task<StoredImportFile> StoreAsync(string sourcePath, ImportSourceKind kind, CancellationToken ct);

    /// <summary>
    /// Converts a stored relative path (returned by StoreAsync) into an absolute path.
    /// </summary>
    string GetAbsolutePath(string storedRelativePath);
}

/// <summary>
/// Result metadata for a stored import file.
/// Persist Sha256Hex + StoredRelativePath in your imports table for replayability.
/// </summary>
public sealed record StoredImportFile(
    string Sha256Hex,
    string StoredRelativePath,
    long FileSizeBytes,
    string FileExtension,
    string OriginalFileName);

/// <summary>
/// What type of document is being stored. Used for folder structure and later replay logic.
/// </summary>
public enum ImportSourceKind
{
    Pdf = 0,
    Qfx = 1,
    Csv = 2,
    Other = 99
}

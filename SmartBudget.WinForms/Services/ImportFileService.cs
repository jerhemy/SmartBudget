// Project: SmartBudget.WinForms
// File: Services/ImportFileStore.cs

using SmartBudget.WinForms.Abstractions;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SmartBudget.WinForms.Services;

/// <summary>
/// Stores import source files in a content-addressed folder structure under the app data directory.
/// </summary>
public sealed class ImportFileStore : IImportFileStore
{
    private readonly string _baseDir;

    /// <param name="baseDir">
    /// The base data directory, e.g. %AppData%\SmartBudget\data
    /// </param>
    public ImportFileStore(string baseDir)
    {
        if (string.IsNullOrWhiteSpace(baseDir))
            throw new ArgumentException("Base directory is required.", nameof(baseDir));

        _baseDir = baseDir;
    }

    public string GetAbsolutePath(string storedRelativePath)
    {
        if (string.IsNullOrWhiteSpace(storedRelativePath))
            throw new ArgumentException("Stored relative path is required.", nameof(storedRelativePath));

        return Path.Combine(_baseDir, storedRelativePath);
    }

    public async Task<StoredImportFile> StoreAsync(string sourcePath, ImportSourceKind kind, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path is required.", nameof(sourcePath));

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file not found.", sourcePath);

        var fi = new FileInfo(sourcePath);
        var originalFileName = fi.Name;

        // Extension without dot (e.g. "pdf")
        var ext = fi.Extension.TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
            ext = "bin";

        // 1) Hash the file content
        var sha256Hex = await ComputeSha256HexAsync(sourcePath, ct).ConfigureAwait(false);

        // 2) Build content-addressed destination
        // imports/{kind}/{shard}/{sha}.{ext}
        var kindFolder = KindToFolder(kind);
        var shard = sha256Hex[..2];

        var relDir = Path.Combine("imports", kindFolder, shard);
        var relName = $"{sha256Hex}.{ext}";
        var storedRelativePath = Path.Combine(relDir, relName);

        var absDir = Path.Combine(_baseDir, relDir);
        var absPath = Path.Combine(_baseDir, storedRelativePath);

        Directory.CreateDirectory(absDir);

        // 3) Copy if not already present (dedupe)
        if (!File.Exists(absPath))
        {
            // Copy to temp file, then move into place (atomic if same volume)
            var tmpPath = absPath + ".tmp-" + Guid.NewGuid().ToString("N");

            try
            {
                await CopyFileAsync(sourcePath, tmpPath, ct).ConfigureAwait(false);

                try
                {
                    File.Move(tmpPath, absPath);
                }
                catch
                {
                    // Another process/thread may have created it first.
                    if (File.Exists(absPath))
                    {
                        TryDelete(tmpPath);
                    }
                    else
                    {
                        throw;
                    }
                }

                // Optional: mark read-only to discourage edits
                TrySetReadOnly(absPath);
            }
            catch
            {
                // Ensure temp is cleaned up on failure
                TryDelete(tmpPath);
                throw;
            }
        }

        return new StoredImportFile(
            Sha256Hex: sha256Hex,
            StoredRelativePath: storedRelativePath,
            FileSizeBytes: fi.Length,
            FileExtension: ext,
            OriginalFileName: originalFileName);
    }

    private static string KindToFolder(ImportSourceKind kind) => kind switch
    {
        ImportSourceKind.Pdf => "pdf",
        ImportSourceKind.Qfx => "qfx",
        ImportSourceKind.Csv => "csv",
        _ => "other"
    };

    private static async Task<string> ComputeSha256HexAsync(string path, CancellationToken ct)
    {
        // Big-ish buffer; fine for typical statements
        const int bufferSize = 128 * 1024;

        await using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs, ct).ConfigureAwait(false);

        // Uppercase hex string (stable)
        return Convert.ToHexString(hash);
    }

    private static async Task CopyFileAsync(string src, string dst, CancellationToken ct)
    {
        const int bufferSize = 128 * 1024;

        await using var input = new FileStream(
            src,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var output = new FileStream(
            dst,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        await input.CopyToAsync(output, bufferSize, ct).ConfigureAwait(false);
        await output.FlushAsync(ct).ConfigureAwait(false);
    }

    private static void TrySetReadOnly(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            File.SetAttributes(path, attrs | FileAttributes.ReadOnly);
        }
        catch
        {
            // ignore
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }
}
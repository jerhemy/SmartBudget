using System;
using System.Collections.Generic;
using System.Text;

public sealed class FileDroppedEventArgs : EventArgs
{
    public FileDroppedEventArgs(IReadOnlyList<string> filePaths)
    {
        FilePaths = filePaths ?? Array.Empty<string>();
        FilePath = FilePaths.Count > 0 ? FilePaths[0] : string.Empty;
        Extension = string.IsNullOrWhiteSpace(FilePath) ? string.Empty : Path.GetExtension(FilePath).ToLowerInvariant();
        FileName = string.IsNullOrWhiteSpace(FilePath) ? string.Empty : Path.GetFileName(FilePath);
    }

    /// <summary>All dropped files (if multi-drop enabled).</summary>
    public IReadOnlyList<string> FilePaths { get; }

    /// <summary>Convenience: the first file.</summary>
    public string FilePath { get; }

    public string FileName { get; }
    public string Extension { get; }
}
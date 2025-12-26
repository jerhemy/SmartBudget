using System;
using System.IO;

namespace SmartBudget.WinForms.Services;

/// <summary>
/// Centralized file/folder locations used by the application.
/// Keeps DB and imported-source storage under a single root for easy backup/restore.
/// </summary>
public sealed class AppPaths
{
    /// <summary>
    /// Root directory for the application under the user's profile, e.g.:
    /// %AppData%\SmartBudget
    /// </summary>
    public string AppRootDir { get; }

    /// <summary>
    /// Directory where non-database data lives, e.g.:
    /// %AppData%\SmartBudget\data
    /// </summary>
    public string DataDir { get; }

    /// <summary>
    /// Directory where import source files are stored, e.g.:
    /// %AppData%\SmartBudget\data\imports
    /// </summary>
    public string ImportsDir => Path.Combine(DataDir, "imports");

    /// <summary>
    /// Full path to the SQLite database file, e.g.:
    /// %AppData%\SmartBudget\smartbudget.db
    /// </summary>
    public string DbPath => Path.Combine(AppRootDir, "smartbudget.db");

    public AppPaths(string? appName = null)
    {
        appName ??= "SmartBudget";

        // Roaming AppData is typical for desktop apps; if you prefer LocalAppData, swap the folder.
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        AppRootDir = Path.Combine(baseDir, appName);
        DataDir = Path.Combine(AppRootDir, "data");
    }

    /// <summary>
    /// Ensures required directories exist (AppRootDir and DataDir).
    /// Does not create the DB file, only the folders.
    /// </summary>
    public void EnsureDirectories()
    {
        Directory.CreateDirectory(AppRootDir);
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(ImportsDir);
    }
}
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBudget.Infrastructure.Persistence.Sqlite;
using SmartBudget.WinForms.Abstractions;
using SmartBudget.WinForms.Navigation;
using SmartBudget.WinForms.Persistence.Sqlite.Repositories;
using SmartBudget.WinForms.Services;

namespace SmartBudget.WinForms
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            ApplicationConfiguration.Initialize();

            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {

                    services.AddSingleton(sp =>
                    {
                        var appData = @"D:\";
                        var dbPath = Path.Combine(appData, "SmartBudget", "smartbudget.db");

                        var cs = new SqliteConnectionStringBuilder
                        {
                            DataSource = dbPath,
                            Mode = SqliteOpenMode.ReadWriteCreate,
                            Cache = SqliteCacheMode.Shared
                        }.ToString();

                        return new DbOptions(dbPath, cs);
                    });

                    // Centralized paths (DB + imports under one root for easy backup/restore)
                    services.AddSingleton<AppPaths>(sp =>
                    {
                        var paths = new AppPaths("SmartBudget");
                        paths.EnsureDirectories();
                        return paths;
                    });

                    services.AddSingleton<IImportFileStore>(sp =>
                    {
                        var paths = sp.GetRequiredService<AppPaths>();
                        return new ImportFileStore(paths.DataDir);
                    });

                    // WinForms forms
                    services.AddTransient<MainForm>();

                    // App services (examples)
                    services.AddSingleton<SqliteConnectionFactory>();
                    services.AddSingleton<IDbInitializer, SqliteDbInitializer>();

                    // Repos/services
                    services.AddSingleton<IAccountRepository, SqliteAccountRepository>();
                    services.AddSingleton<ITransactionRepository, SqliteTransactionRepository>();
                    services.AddSingleton<SummaryRepository>();
                    services.AddSingleton<CalendarDataService>();




                    // Add Pages Here
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddTransient<Dashboard>();
                    services.AddTransient<AccountPage>();
                    services.AddTransient<ImportPage>();

                    services.AddSingleton<UiTheme>(theme => new UiTheme
                    {
                        AppBack = Color.FromArgb(18, 18, 18),
                        PanelBack = Color.FromArgb(22, 22, 22),
                        CardBack = Color.FromArgb(28, 28, 28),
                        CardBackDim = Color.FromArgb(24, 24, 24),
                        HoverBack = Color.FromArgb(36, 36, 36),
                        SelectedBack = Color.FromArgb(45, 45, 45),
                        TextPrimary = Color.Gainsboro,
                        TextMuted = Color.FromArgb(140, 140, 140),
                        //Border = Color.FromArgb(55, 55, 55),
                        //GridLine = Color.FromArgb(40, 40, 40),
                        //ZeroLine = Color.FromArgb(80, 80, 80),
                        Accent = Color.FromArgb(0, 120, 215),
                        Positive = Color.FromArgb(0, 140, 0),
                        Negative = Color.FromArgb(200, 0, 0),
                        Background = Color.FromArgb(22, 37, 53),
                        BorderRight = Color.FromArgb(35, 55, 75),
                        HeaderText = Color.White,
                        HeaderFont = new Font("Segoe UI", 9, FontStyle.Bold),
                        ItemFont = new Font("Segoe UI", 8, FontStyle.Bold),
                    });

            // Logging (optional)
            services.AddLogging(b => b.AddConsole());
                })
                .Build();

            host.Services.GetRequiredService<IDbInitializer>().Initialize();

            var mainForm = host.Services.GetRequiredService<MainForm>();

            Application.Run(mainForm);
        }

    }

    public sealed record DbOptions(string DbFilePath, string ConnectionString);
}
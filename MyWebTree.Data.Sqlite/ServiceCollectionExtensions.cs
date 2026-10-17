using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MyWebTree.Data.Sqlite;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Stores MyWebTree families in SQLite.
        /// </summary>
        /// <param name="connectionString">
        /// A SQLite connection string. When omitted, data goes to <c>MyWebTree.db</c> under the
        /// user's local application data folder, which survives redeployments of the app's own folder.
        /// </param>
        public IServiceCollection AddMyWebTreeSqlite(string? connectionString = null)
        {
            string resolved = string.IsNullOrWhiteSpace(connectionString)
                ? $"Data Source={DefaultDatabasePath()}"
                : connectionString;

            services.RemoveAll<IMyWebTreeDbContextFactory>();
            services.AddSingleton<IMyWebTreeDbContextFactory>(new MyWebTreeDbContextFactory(resolved));

            return services;
        }
    }

    private static string DefaultDatabasePath()
    {
        string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "MyWebTree";

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "MyWebTree.db");
    }
}

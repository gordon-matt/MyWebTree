using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MyWebTree.Data.Sqlite;

/// <summary>
/// Represents the my web tree database context factory.
/// </summary>
public class MyWebTreeDbContextFactory(string connectionString) : IMyWebTreeDbContextFactory
{
    private DbContextOptions<MyWebTreeDbContext> Options => field ??= BuildOptions(connectionString);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName => Constants.DatabaseProviders.Sqlite;

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <returns>The context.</returns>
    public DbContext GetContext() => new MyWebTreeDbContext(Options);

    /// <summary>
    /// Gets the context.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The context.</returns>
    public DbContext GetContext(string connectionString) => new MyWebTreeDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<MyWebTreeDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyWebTreeDbContext>();

        optionsBuilder.UseSqlite(Prepare(connectionString), sqlite => sqlite
            .MigrationsAssembly(typeof(MyWebTreeDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        // SQLite has no schemas. HasDefaultSchema("webtree") only names the tables; the warning is noise.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(SqliteEventId.SchemaConfiguredWarning));

        return optionsBuilder.Options;
    }

    /// <summary>
    /// Creates the database's folder, since SQLite will create a missing file but not a missing
    /// directory, and forces foreign keys on so deleting a tree can cascade to its people.
    /// </summary>
    private static string Prepare(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString) { ForeignKeys = true };

        if (builder.DataSource is { Length: > 0 } dataSource
            && !dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            && builder.Mode != SqliteOpenMode.Memory
            && Path.GetDirectoryName(Path.GetFullPath(dataSource)) is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}

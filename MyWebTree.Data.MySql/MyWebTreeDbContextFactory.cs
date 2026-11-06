namespace MyWebTree.Data.MySql;

/// <summary>
/// Represents the my web tree database context factory.
/// </summary>
public class MyWebTreeDbContextFactory(string connectionString) : IMyWebTreeDbContextFactory
{
    private DbContextOptions<MyWebTreeDbContext> Options => field ??= BuildOptions(connectionString);

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string ProviderName => Constants.DatabaseProviders.MySql;

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

        optionsBuilder.UseMySQL(connectionString, mySql => mySql
            .MigrationsAssembly(typeof(MyWebTreeDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable));

        return optionsBuilder.Options;
    }
}

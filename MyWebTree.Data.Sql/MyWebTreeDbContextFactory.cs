namespace MyWebTree.Data.Sql;

public class MyWebTreeDbContextFactory(string connectionString) : IMyWebTreeDbContextFactory
{
    private DbContextOptions<MyWebTreeDbContext> Options => field ??= BuildOptions(connectionString);

    public string ProviderName => Constants.DatabaseProviders.SqlServer;

    public DbContext GetContext() => new MyWebTreeDbContext(Options);

    public DbContext GetContext(string connectionString) => new MyWebTreeDbContext(BuildOptions(connectionString));

    internal static DbContextOptions<MyWebTreeDbContext> BuildOptions(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyWebTreeDbContext>();

        optionsBuilder.UseSqlServer(connectionString, sql => sql
            .MigrationsAssembly(typeof(MyWebTreeDbContext).Assembly.GetName().Name)
            .MigrationsHistoryTable(Constants.MigrationsHistoryTable, Constants.Schema));

        return optionsBuilder.Options;
    }
}

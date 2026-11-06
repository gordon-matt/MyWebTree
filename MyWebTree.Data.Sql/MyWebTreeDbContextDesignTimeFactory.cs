using Microsoft.EntityFrameworkCore.Design;

namespace MyWebTree.Data.Sql;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a running SQL Server.</summary>
public class MyWebTreeDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MyWebTreeDbContext>
{
    /// <summary>
    /// Creates the database context.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>The database context.</returns>
    public MyWebTreeDbContext CreateDbContext(string[] args) =>
        new(MyWebTreeDbContextFactory.BuildOptions(
            "Server=localhost;Database=mywebtree_design;Trusted_Connection=True;TrustServerCertificate=True"));
}

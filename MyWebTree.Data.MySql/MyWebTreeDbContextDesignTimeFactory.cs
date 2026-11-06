using Microsoft.EntityFrameworkCore.Design;

namespace MyWebTree.Data.MySql;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a running MySQL server.</summary>
public class MyWebTreeDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MyWebTreeDbContext>
{
    /// <summary>
    /// Creates the database context.
    /// </summary>
    /// <param name="args">The args.</param>
    /// <returns>The database context.</returns>
    public MyWebTreeDbContext CreateDbContext(string[] args) =>
        new(MyWebTreeDbContextFactory.BuildOptions(
            "Server=localhost;Database=mywebtree_design;User=root;Password=root"));
}

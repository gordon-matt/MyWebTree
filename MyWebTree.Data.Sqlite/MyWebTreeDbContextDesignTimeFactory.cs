using Microsoft.EntityFrameworkCore.Design;

namespace MyWebTree.Data.Sqlite;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a host application.</summary>
public class MyWebTreeDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MyWebTreeDbContext>
{
    public MyWebTreeDbContext CreateDbContext(string[] args) =>
        new(MyWebTreeDbContextFactory.BuildOptions("Data Source=mywebtree_design.db"));
}

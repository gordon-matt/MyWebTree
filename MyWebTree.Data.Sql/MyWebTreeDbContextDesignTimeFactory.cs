using Microsoft.EntityFrameworkCore.Design;

namespace MyWebTree.Data.Sql;

/// <summary>Used by <c>dotnet ef</c> so migrations can be generated without a running SQL Server.</summary>
public class MyWebTreeDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MyWebTreeDbContext>
{
    public MyWebTreeDbContext CreateDbContext(string[] args) =>
        new(MyWebTreeDbContextFactory.BuildOptions(
            "Server=localhost;Database=mywebtree_design;Trusted_Connection=True;TrustServerCertificate=True"));
}

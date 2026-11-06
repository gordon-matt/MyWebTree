namespace MyWebTree.Data.MySql;

/// <summary>
/// Represents the my web tree database context.
/// </summary>
public class MyWebTreeDbContext(DbContextOptions<MyWebTreeDbContext> options) : MyWebTreeDbContextBase(options)
{
    /// <inheritdoc/>
    protected override bool SupportsSchemas => false;
}

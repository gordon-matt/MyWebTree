namespace MyWebTree.Data.Npgsql;

/// <summary>
/// Represents the my web tree database context.
/// </summary>
public class MyWebTreeDbContext(DbContextOptions<MyWebTreeDbContext> options) : MyWebTreeDbContextBase(options);

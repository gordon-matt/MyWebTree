namespace MyWebTree.Data;

/// <summary>
/// Represents the my web tree database context base.
/// </summary>
public abstract class MyWebTreeDbContextBase(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    /// Gets the trees.
    /// </summary>
    public DbSet<FamilyTree> Trees => Set<FamilyTree>();

    /// <summary>
    /// Gets the people.
    /// </summary>
    public DbSet<Person> People => Set<Person>();

    /// <summary>
    /// Gets the relationships.
    /// </summary>
    public DbSet<PersonRelationship> Relationships => Set<PersonRelationship>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Constants.Schema);
        builder.ApplyConfigurationsFromAssembly(typeof(MyWebTreeDbContextBase).Assembly);
    }
}

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

    /// <summary>
    /// Gets the parent sets.
    /// </summary>
    public DbSet<ParentSet> ParentSets => Set<ParentSet>();

    /// <summary>
    /// Gets the parent set members.
    /// </summary>
    public DbSet<ParentSetMember> ParentSetMembers => Set<ParentSetMember>();

    /// <summary>
    /// Whether the provider can hold the tables in <see cref="Constants.Schema"/>. Providers where a
    /// schema is a database (MySQL) or does not exist (SQLite) return <see langword="false"/>, and
    /// get <see cref="Constants.TablePrefix"/> on every table name instead.
    /// </summary>
    protected virtual bool SupportsSchemas => true;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MyWebTreeDbContextBase).Assembly);

        if (SupportsSchemas)
        {
            builder.HasDefaultSchema(Constants.Schema);
            return;
        }

        builder.Model.SetDefaultSchema(null);

        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => e.BaseType is null))
        {
            if (entityType.GetTableName() is not { } tableName)
            {
                continue;
            }

            entityType.SetSchema(null);
            entityType.SetTableName($"{Constants.TablePrefix}{tableName}");
        }
    }
}

namespace MyWebTree.Data;

public abstract class MyWebTreeDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<FamilyTree> Trees => Set<FamilyTree>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<PersonRelationship> Relationships => Set<PersonRelationship>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Constants.Schema);
        builder.ApplyConfigurationsFromAssembly(typeof(MyWebTreeDbContextBase).Assembly);
    }
}

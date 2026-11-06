namespace MyWebTree.Data.Entities;

/// <summary>
/// Represents the person relationship.
/// </summary>
public class PersonRelationship : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the tree identifier.
    /// </summary>
    public int TreeId { get; set; }

    /// <summary>
    /// Gets or sets the from person identifier.
    /// </summary>
    public int FromPersonId { get; set; }

    /// <summary>
    /// Gets or sets the to person identifier.
    /// </summary>
    public int ToPersonId { get; set; }

    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
    public RelationshipKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the from person.
    /// </summary>
    public virtual Person? FromPerson { get; set; }

    /// <summary>
    /// Gets or sets the to person.
    /// </summary>
    public virtual Person? ToPerson { get; set; }
}

/// <summary>
/// Represents the person relationship map.
/// </summary>
public class PersonRelationshipMap : IEntityTypeConfiguration<PersonRelationship>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<PersonRelationship> builder)
    {
        builder.ToTable(Constants.Tables.Relationships, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Kind).HasConversion<int>();

        builder.HasOne(m => m.FromPerson)
            .WithMany()
            .HasForeignKey(m => m.FromPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ToPerson)
            .WithMany()
            .HasForeignKey(m => m.ToPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.TreeId, m.Kind });
        builder.HasIndex(m => new { m.FromPersonId, m.ToPersonId, m.Kind }).IsUnique();
    }
}

namespace MyWebTree.Data.Entities;

public class PersonRelationship : BaseEntity<int>
{
    public int TreeId { get; set; }

    public int FromPersonId { get; set; }

    public int ToPersonId { get; set; }

    public RelationshipKind Kind { get; set; }

    public virtual Person? FromPerson { get; set; }

    public virtual Person? ToPerson { get; set; }
}

public class PersonRelationshipMap : IEntityTypeConfiguration<PersonRelationship>
{
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

using System.Diagnostics;

namespace MyWebTree.Data.Entities;

[DebuggerDisplay("{FirstName} {LastName}")]
public class Person : BaseEntity<int>
{
    public int TreeId { get; set; }

    public Gender Gender { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public DateOnly? DeathDate { get; set; }

    public string? BirthPlace { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Notes { get; set; }

    public virtual FamilyTree? Tree { get; set; }
}

public class PersonMap : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable(Constants.Tables.People, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Gender).HasConversion<int>();
        builder.Property(m => m.FirstName).IsRequired().HasMaxLength(Constants.MaxLengths.PersonName).IsUnicode(true);
        builder.Property(m => m.LastName).IsRequired().HasMaxLength(Constants.MaxLengths.PersonName).IsUnicode(true);
        builder.Property(m => m.BirthPlace).HasMaxLength(Constants.MaxLengths.BirthPlace).IsUnicode(true);
        builder.Property(m => m.AvatarUrl).HasMaxLength(Constants.MaxLengths.AvatarUrl).IsUnicode(false);
        builder.Property(m => m.Notes).HasMaxLength(Constants.MaxLengths.Notes).IsUnicode(true);

        builder.HasOne(m => m.Tree)
            .WithMany(m => m.People)
            .HasForeignKey(m => m.TreeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.TreeId);
    }
}

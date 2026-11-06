using System.Diagnostics;
using MyWebTree.Models;

namespace MyWebTree.Data.Entities;

/// <summary>
/// Represents the person.
/// </summary>
[DebuggerDisplay("{FirstName} {LastName}")]
public class Person : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the tree identifier.
    /// </summary>
    public int TreeId { get; set; }

    /// <summary>
    /// Gets or sets the gender.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the birth date.
    /// </summary>
    public PartialDate? BirthDate { get; set; }

    /// <summary>
    /// Gets or sets the death date.
    /// </summary>
    public PartialDate? DeathDate { get; set; }

    /// <summary>
    /// Gets or sets the birth place.
    /// </summary>
    public string? BirthPlace { get; set; }

    /// <summary>
    /// Gets or sets the death place.
    /// </summary>
    public string? DeathPlace { get; set; }

    /// <summary>
    /// Gets or sets the avatar URL.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the tree.
    /// </summary>
    public virtual FamilyTree? Tree { get; set; }
}

/// <summary>
/// Represents the person map.
/// </summary>
public class PersonMap : IEntityTypeConfiguration<Person>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable(Constants.Tables.People, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Gender).HasConversion<int>();
        builder.Property(m => m.FirstName).IsRequired().HasMaxLength(Constants.MaxLengths.PersonName).IsUnicode(true);
        builder.Property(m => m.LastName).IsRequired().HasMaxLength(Constants.MaxLengths.PersonName).IsUnicode(true);
        builder.Property(m => m.BirthDate).HasConversion(PartialDateConverter.Instance).HasMaxLength(10).IsUnicode(false);
        builder.Property(m => m.DeathDate).HasConversion(PartialDateConverter.Instance).HasMaxLength(10).IsUnicode(false);
        builder.Property(m => m.BirthPlace).HasMaxLength(Constants.MaxLengths.Place).IsUnicode(true);
        builder.Property(m => m.DeathPlace).HasMaxLength(Constants.MaxLengths.Place).IsUnicode(true);
        builder.Property(m => m.AvatarUrl).HasMaxLength(Constants.MaxLengths.AvatarUrl).IsUnicode(false);
        builder.Property(m => m.Notes).HasMaxLength(Constants.MaxLengths.Notes).IsUnicode(true);

        builder.HasOne(m => m.Tree)
            .WithMany(m => m.People)
            .HasForeignKey(m => m.TreeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.TreeId);
    }
}

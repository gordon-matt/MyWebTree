using System.Diagnostics;

namespace MyWebTree.Data.Entities;

/// <summary>
/// Represents the family tree.
/// </summary>
[DebuggerDisplay("{Name}")]
public class FamilyTree : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the focus person identifier.
    /// </summary>
    public int? FocusPersonId { get; set; }

    /// <summary>
    /// Gets or sets the created at utc.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the updated at utc.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the focus person.
    /// </summary>
    public virtual Person? FocusPerson { get; set; }

    /// <summary>
    /// Gets or sets the people.
    /// </summary>
    public virtual ICollection<Person> People { get; set; } = [];
}

/// <summary>
/// Represents the family tree map.
/// </summary>
public class FamilyTreeMap : IEntityTypeConfiguration<FamilyTree>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<FamilyTree> builder)
    {
        builder.ToTable(Constants.Tables.Trees, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(Constants.MaxLengths.TreeName).IsUnicode(true);
        builder.Property(m => m.Description).HasMaxLength(Constants.MaxLengths.Description).IsUnicode(true);
        builder.Property(m => m.CreatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);
        builder.Property(m => m.UpdatedAtUtc).HasConversion(UtcDateTimeConverter.Instance);

        builder.HasOne(m => m.FocusPerson)
            .WithMany()
            .HasForeignKey(m => m.FocusPersonId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

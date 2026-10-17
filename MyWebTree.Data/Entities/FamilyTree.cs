using System.Diagnostics;

namespace MyWebTree.Data.Entities;

[DebuggerDisplay("{Name}")]
public class FamilyTree : BaseEntity<int>
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? FocusPersonId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Person? FocusPerson { get; set; }

    public virtual ICollection<Person> People { get; set; } = [];
}

public class FamilyTreeMap : IEntityTypeConfiguration<FamilyTree>
{
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

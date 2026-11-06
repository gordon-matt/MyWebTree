using System.Diagnostics;
using MyWebTree.Models;

namespace MyWebTree.Data.Entities;

/// <summary>
/// One set of parents of a child: the one or two people who parented them as a birth, adopted, or foster family.
/// This table, with <see cref="ParentSetMember"/>, is the only record of who a person's parents are.
/// </summary>
[DebuggerDisplay("{ChildPersonId} {Type}")]
public class ParentSet : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the tree identifier.
    /// </summary>
    public int TreeId { get; set; }

    /// <summary>
    /// Gets or sets the child these parents belong to.
    /// </summary>
    public int ChildPersonId { get; set; }

    /// <summary>
    /// Gets or sets how the child came to have these parents.
    /// </summary>
    public ParentageType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the child's primary set.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the date of the adoption or placement.
    /// </summary>
    public PartialDate? Date { get; set; }

    /// <summary>
    /// Gets or sets the child.
    /// </summary>
    public virtual Person? Child { get; set; }

    /// <summary>
    /// Gets or sets the parents in this set.
    /// </summary>
    public virtual ICollection<ParentSetMember> Members { get; set; } = [];
}

/// <summary>
/// Represents the parent set map.
/// </summary>
public class ParentSetMap : IEntityTypeConfiguration<ParentSet>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<ParentSet> builder)
    {
        builder.ToTable(Constants.Tables.ParentSets, Constants.Schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasConversion<int>();
        builder.Property(m => m.Date).HasConversion(PartialDateConverter.Instance).HasMaxLength(10).IsUnicode(false);

        // Restrict, like the other person links: the service removes a child's sets before it removes the child.
        builder.HasOne(m => m.Child)
            .WithMany()
            .HasForeignKey(m => m.ChildPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.TreeId);
        builder.HasIndex(m => m.ChildPersonId);
    }
}

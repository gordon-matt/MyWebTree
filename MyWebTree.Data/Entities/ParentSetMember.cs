using System.Diagnostics;

namespace MyWebTree.Data.Entities;

/// <summary>
/// One parent in a <see cref="ParentSet"/>.
/// </summary>
[DebuggerDisplay("{ParentSetId} {ParentPersonId}")]
public class ParentSetMember : BaseEntity<int>
{
    /// <summary>
    /// Gets or sets the tree identifier. It repeats the set's tree so a whole tree can be cleared without a join.
    /// </summary>
    public int TreeId { get; set; }

    /// <summary>
    /// Gets or sets the set this parent belongs to.
    /// </summary>
    public int ParentSetId { get; set; }

    /// <summary>
    /// Gets or sets the parent.
    /// </summary>
    public int ParentPersonId { get; set; }

    /// <summary>
    /// Gets or sets the set.
    /// </summary>
    public virtual ParentSet? ParentSet { get; set; }

    /// <summary>
    /// Gets or sets the parent.
    /// </summary>
    public virtual Person? ParentPerson { get; set; }
}

/// <summary>
/// Represents the parent set member map.
/// </summary>
public class ParentSetMemberMap : IEntityTypeConfiguration<ParentSetMember>
{
    /// <summary>
    /// Configures the value.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public void Configure(EntityTypeBuilder<ParentSetMember> builder)
    {
        builder.ToTable(Constants.Tables.ParentSetMembers, Constants.Schema);
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.ParentSet)
            .WithMany(m => m.Members)
            .HasForeignKey(m => m.ParentSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ParentPerson)
            .WithMany()
            .HasForeignKey(m => m.ParentPersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.TreeId);
        builder.HasIndex(m => m.ParentPersonId);
        builder.HasIndex(m => new { m.ParentSetId, m.ParentPersonId }).IsUnique();
    }
}

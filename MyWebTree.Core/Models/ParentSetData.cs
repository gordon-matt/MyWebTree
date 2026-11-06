namespace MyWebTree.Models;

/// <summary>
/// The one or two people who parented a child in one context (birth, adoption, or fostering).
/// A person can have several of these. Exactly one is the primary set, which is the one drawn by default.
/// </summary>
public sealed record ParentSetData
{
    /// <summary>Gets or sets the identifier, unique among the sets of one child.</summary>
    public required string Id { get; init; }

    /// <summary>Gets or sets how the child came to have these parents.</summary>
    public ParentageType Type { get; init; } = ParentageType.Birth;

    /// <summary>Gets or sets a value indicating whether this is the set drawn by default.</summary>
    public bool IsPrimary { get; init; }

    /// <summary>Gets or sets the one or two parents in this set.</summary>
    public IReadOnlyList<string> ParentIds { get; init; } = [];

    /// <summary>Gets or sets the optional date of the adoption or placement.</summary>
    public PartialDate? Date { get; init; }
}

/// <summary>
/// Where a new parent goes. With no value the parent joins the child's primary set, or starts a primary
/// <see cref="ParentageType.Birth"/> set when the child has none.
/// </summary>
public sealed record ParentSetTarget
{
    /// <summary>Gets or sets the set to add to. Null means the primary set.</summary>
    public string? ParentSetId { get; init; }

    /// <summary>Gets or sets a value indicating whether to start a new set of <see cref="Type"/> instead of using an existing one.</summary>
    public bool CreateNew { get; init; }

    /// <summary>Gets or sets the type of the set to create. It is ignored when an existing set is used.</summary>
    public ParentageType Type { get; init; } = ParentageType.Birth;
}

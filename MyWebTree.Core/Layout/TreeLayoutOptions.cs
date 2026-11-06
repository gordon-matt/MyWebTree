namespace MyWebTree.Layout;

/// <summary>
/// Represents the tree layout options.
/// </summary>
public sealed record TreeLayoutOptions
{
    /// <summary>
    /// Gets or sets the focus identifier.
    /// </summary>
    public string? FocusId { get; init; }

    /// <summary>Generations shown above the focus person. Zero hides parents.</summary>
    public int AncestryDepth { get; init; } = 4;

    /// <summary>Generations shown below the focus person.</summary>
    public int ProgenyDepth { get; init; } = 4;

    /// <summary>
    /// Gets or sets a value indicating whether this instance is show siblings.
    /// </summary>
    public bool ShowSiblings { get; init; }

    /// <summary>Both sides of the family, or only the father's line.</summary>
    public LineageScope Lineage { get; init; } = LineageScope.Bilateral;

    /// <summary>Draws dashed Add father / mother / spouse / child cards around the focus person.</summary>
    public bool ShowPlaceholders { get; init; }

    /// <summary>
    /// The set of parents to draw for each person, as person id to set id. A missing or stale entry means the
    /// primary set. Changing it changes the drawing, not the data.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ActiveParentSets { get; init; }

    /// <summary>Hides adopted and foster links, and everyone reachable only through them.</summary>
    public bool BloodRelativesOnly { get; init; }

    /// <summary>
    /// Gets or sets the orientation.
    /// </summary>
    public TreeOrientation Orientation { get; init; } = TreeOrientation.Vertical;
}

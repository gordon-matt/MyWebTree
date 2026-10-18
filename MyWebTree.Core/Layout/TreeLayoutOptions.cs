namespace MyWebTree.Layout;

public sealed record TreeLayoutOptions
{
    public string? FocusId { get; init; }

    /// <summary>Generations shown above the focus person. Zero hides parents.</summary>
    public int AncestryDepth { get; init; } = 4;

    /// <summary>Generations shown below the focus person.</summary>
    public int ProgenyDepth { get; init; } = 4;

    public bool ShowSiblings { get; init; }

    /// <summary>Both sides of the family, or only the father's line.</summary>
    public LineageScope Lineage { get; init; } = LineageScope.Bilateral;

    /// <summary>Draws dashed Add father / mother / spouse / child cards around the focus person.</summary>
    public bool ShowPlaceholders { get; init; }

    public TreeOrientation Orientation { get; init; } = TreeOrientation.Vertical;
}

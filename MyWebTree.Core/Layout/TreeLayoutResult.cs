namespace MyWebTree.Layout;

/// <summary>
/// Represents the tree card.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="X">The x.</param>
/// <param name="Y">The y.</param>
/// <param name="IsPlaceholder">The is placeholder.</param>
/// <param name="Placeholder">The placeholder.</param>
/// <param name="RelatesToId">The relates to identifier.</param>
/// <param name="IsStub">True for a stub: one card under parents the person is not currently shown with.</param>
/// <param name="PersonId">The real person. It is the same as <paramref name="Id"/> except on a stub, and null on a placeholder.</param>
/// <param name="ParentType">The type of the set of parents drawn above this card.</param>
/// <param name="ParentSetId">The set of parents drawn above this card (the stub's own set for a stub), or null.</param>
/// <param name="ParentSetCount">How many sets of parents the person has.</param>
/// <param name="SeeAlso">True for a stub whose real person is also drawn elsewhere in the chart.</param>
public sealed record TreeCard(
    string Id,
    double X,
    double Y,
    bool IsPlaceholder,
    PlaceholderKind? Placeholder,
    string? RelatesToId,
    bool IsStub = false,
    string? PersonId = null,
    ParentageType ParentType = ParentageType.Birth,
    string? ParentSetId = null,
    int ParentSetCount = 0,
    bool SeeAlso = false);

/// <summary>
/// Represents the tree link.
/// </summary>
/// <param name="X1">The x1.</param>
/// <param name="Y1">The y1.</param>
/// <param name="X2">The x2.</param>
/// <param name="Y2">The y2.</param>
/// <param name="Dashed">True when the line leads to or from a placeholder card (a relative who is not in the tree yet).</param>
/// <param name="Type">How the child on this line was parented. Birth is drawn solid, adopted dashed, foster dotted.</param>
public sealed record TreeLink(double X1, double Y1, double X2, double Y2, bool Dashed, ParentageType Type = ParentageType.Birth);

/// <summary>
/// Represents the tree layout result.
/// </summary>
/// <param name="FocusId">The focus identifier.</param>
/// <param name="Cards">The cards.</param>
/// <param name="Links">The links.</param>
/// <param name="Kinship">The kinship.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public sealed record TreeLayoutResult(
    string? FocusId,
    IReadOnlyList<TreeCard> Cards,
    IReadOnlyList<TreeLink> Links,
    IReadOnlyDictionary<string, string> Kinship,
    double Width,
    double Height)
{
    /// <summary>
    /// Gets the empty.
    /// </summary>
    public static TreeLayoutResult Empty { get; } = new(null, [], [], new Dictionary<string, string>(), 0, 0);
}

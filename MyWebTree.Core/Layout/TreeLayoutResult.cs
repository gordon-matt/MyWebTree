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
public sealed record TreeCard(
    string Id,
    double X,
    double Y,
    bool IsPlaceholder,
    PlaceholderKind? Placeholder,
    string? RelatesToId);

/// <summary>
/// Represents the tree link.
/// </summary>
/// <param name="X1">The x1.</param>
/// <param name="Y1">The y1.</param>
/// <param name="X2">The x2.</param>
/// <param name="Y2">The y2.</param>
/// <param name="Dashed">The dashed.</param>
public sealed record TreeLink(double X1, double Y1, double X2, double Y2, bool Dashed);

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

namespace MyWebTree.Layout;

public sealed record TreeCard(
    string Id,
    double X,
    double Y,
    bool IsPlaceholder,
    PlaceholderKind? Placeholder,
    string? RelatesToId);

public sealed record TreeLink(double X1, double Y1, double X2, double Y2, bool Dashed);

public sealed record TreeLayoutResult(
    string? FocusId,
    IReadOnlyList<TreeCard> Cards,
    IReadOnlyList<TreeLink> Links,
    IReadOnlyDictionary<string, string> Kinship,
    double Width,
    double Height)
{
    public static TreeLayoutResult Empty { get; } = new(null, [], [], new Dictionary<string, string>(), 0, 0);
}

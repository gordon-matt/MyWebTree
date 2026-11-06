namespace MyWebTree;

/// <summary>
/// Specifies the gender.
/// </summary>
public enum Gender
{
    /// <summary>
    /// The unknown value.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The male value.
    /// </summary>
    Male = 1,
    /// <summary>
    /// The female value.
    /// </summary>
    Female = 2,
}

/// <summary>A stored link between two people. Spouse rows are kept once; parent rows point from parent to child.</summary>
public enum RelationshipKind
{
    /// <summary>
    /// The parent value.
    /// </summary>
    Parent = 0,
    /// <summary>
    /// The spouse value.
    /// </summary>
    Spouse = 1,
}

/// <summary>Who is being added, from the point of view of the person already in the tree.</summary>
public enum RelativeKind
{
    /// <summary>
    /// The father value.
    /// </summary>
    Father,
    /// <summary>
    /// The mother value.
    /// </summary>
    Mother,
    /// <summary>
    /// The spouse value.
    /// </summary>
    Spouse,
    /// <summary>
    /// The son value.
    /// </summary>
    Son,
    /// <summary>
    /// The daughter value.
    /// </summary>
    Daughter,
    /// <summary>
    /// The child value.
    /// </summary>
    Child,
}

/// <summary>A dashed card the layout inserts so a missing relative can be added in place.</summary>
public enum PlaceholderKind
{
    /// <summary>
    /// The father value.
    /// </summary>
    Father,
    /// <summary>
    /// The mother value.
    /// </summary>
    Mother,
    /// <summary>
    /// The spouse value.
    /// </summary>
    Spouse,
    /// <summary>
    /// The child value.
    /// </summary>
    Child,
}

/// <summary>
/// Specifies the tree orientation.
/// </summary>
public enum TreeOrientation
{
    /// <summary>
    /// The vertical value.
    /// </summary>
    Vertical,
    /// <summary>
    /// The horizontal value.
    /// </summary>
    Horizontal,
}

/// <summary>Which ancestors are drawn above the focus person.</summary>
public enum LineageScope
{
    /// <summary>Both parents, and each of their ancestors.</summary>
    Bilateral,

    /// <summary>The father's line. When there is no father, the other parent is used.</summary>
    Patrilineal,
}

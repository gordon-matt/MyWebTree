namespace MyWebTree;

public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2,
}

/// <summary>A stored link between two people. Spouse rows are kept once; parent rows point from parent to child.</summary>
public enum RelationshipKind
{
    Parent = 0,
    Spouse = 1,
}

/// <summary>Who is being added, from the point of view of the person already in the tree.</summary>
public enum RelativeKind
{
    Father,
    Mother,
    Spouse,
    Son,
    Daughter,
    Child,
}

/// <summary>A dashed card the layout inserts so a missing relative can be added in place.</summary>
public enum PlaceholderKind
{
    Father,
    Mother,
    Spouse,
    Child,
}

public enum TreeOrientation
{
    Vertical,
    Horizontal,
}

namespace MyWebTree.Models;

public sealed record FamilyTreeSummary(int Id, string Name, string? Description, int PeopleCount, DateTime UpdatedAtUtc);

public sealed record FamilyTreeDetail(
    int Id,
    string Name,
    string? Description,
    string? FocusPersonId,
    IReadOnlyList<PersonData> People);

public sealed record PersonWriteModel
{
    public Gender Gender { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    public DateOnly? DeathDate { get; init; }

    public string? BirthPlace { get; init; }

    public string? AvatarUrl { get; init; }

    public string? Notes { get; init; }
}

public sealed record AddRelativeRequest
{
    public required string RelatesToPersonId { get; init; }

    public required RelativeKind Kind { get; init; }

    /// <summary>The other parent, when the new person is a child and more than one spouse could be.</summary>
    public string? OtherParentId { get; init; }

    public required PersonWriteModel Person { get; init; }
}

/// <summary>What the tree UI hands back after an edit, an undo, or a change of focus.</summary>
public sealed record FamilyTreeChange(IReadOnlyList<PersonData> People, string? FocusPersonId);

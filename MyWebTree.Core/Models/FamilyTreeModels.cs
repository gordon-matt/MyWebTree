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

    public PartialDate? BirthDate { get; init; }

    public PartialDate? DeathDate { get; init; }

    public string? BirthPlace { get; init; }

    public string? DeathPlace { get; init; }

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

/// <summary>Why <see cref="FamilyTreeChange"/> was raised, so a host can save one edit instead of replacing the graph.</summary>
public enum FamilyTreeChangeKind
{
    /// <summary>Undo, redo, or any other case where <see cref="FamilyTreeChange.People"/> is the whole graph.</summary>
    Snapshot,

    /// <summary><see cref="FamilyTreeChange.PersonId"/> was added, including the links on that card.</summary>
    Added,

    /// <summary>Names, dates, places, picture, or notes changed on <see cref="FamilyTreeChange.PersonId"/>. Links did not.</summary>
    Updated,

    /// <summary><see cref="FamilyTreeChange.PersonId"/> was removed and unlinked. Relatives stay.</summary>
    Removed,

    /// <summary>
    /// The link between <see cref="FamilyTreeChange.PersonId"/> and <see cref="FamilyTreeChange.OtherPersonId"/> was removed.
    /// Neither person was deleted.
    /// </summary>
    Unlinked,

    /// <summary>The centered person changed. Nobody's data changed.</summary>
    FocusChanged,
}

/// <summary>What the tree UI hands back after an edit, an undo, or a change of focus.</summary>
public sealed record FamilyTreeChange(
    IReadOnlyList<PersonData> People,
    string? FocusPersonId,
    FamilyTreeChangeKind Kind = FamilyTreeChangeKind.Snapshot,
    string? PersonId = null,
    string? OtherPersonId = null);

/// <summary>Passed to a host that wants to supply a picture instead of a pasted address.</summary>
public sealed record AvatarRequest(string? PersonId, string? CurrentUrl);

namespace MyWebTree.Models;

/// <summary>
/// Represents the family tree summary.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="PeopleCount">The people count.</param>
/// <param name="UpdatedAtUtc">The updated at utc.</param>
public sealed record FamilyTreeSummary(int Id, string Name, string? Description, int PeopleCount, DateTime UpdatedAtUtc);

/// <summary>
/// Represents the family tree detail.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="FocusPersonId">The focus person identifier.</param>
/// <param name="People">The people.</param>
public sealed record FamilyTreeDetail(
    int Id,
    string Name,
    string? Description,
    string? FocusPersonId,
    IReadOnlyList<PersonData> People);

/// <summary>
/// Represents the person write model.
/// </summary>
public sealed record PersonWriteModel
{
    /// <summary>
    /// Gets or sets the gender.
    /// </summary>
    public Gender Gender { get; init; }

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the birth date.
    /// </summary>
    public PartialDate? BirthDate { get; init; }

    /// <summary>
    /// Gets or sets the death date.
    /// </summary>
    public PartialDate? DeathDate { get; init; }

    /// <summary>
    /// Gets or sets the birth place.
    /// </summary>
    public string? BirthPlace { get; init; }

    /// <summary>
    /// Gets or sets the death place.
    /// </summary>
    public string? DeathPlace { get; init; }

    /// <summary>
    /// Gets or sets the avatar URL.
    /// </summary>
    public string? AvatarUrl { get; init; }

    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Represents the add relative request.
/// </summary>
public sealed record AddRelativeRequest
{
    /// <summary>
    /// Gets or sets the relates to person identifier.
    /// </summary>
    public required string RelatesToPersonId { get; init; }

    /// <summary>
    /// Gets or sets the kind.
    /// </summary>
    public required RelativeKind Kind { get; init; }

    /// <summary>The other parent, when the new person is a child and more than one spouse could be.</summary>
    public string? OtherParentId { get; init; }

    /// <summary>
    /// Gets or sets the person.
    /// </summary>
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

/// <summary>Generations of ancestors and descendants the tree is showing. Raised when the user changes either.</summary>
public sealed record TreeDepths(int Ancestry, int Progeny);

/// <summary>
/// The toolbar choices a host can remember: how many generations, whether siblings are shown,
/// which side of the family, and whether the chart runs down or across.
/// </summary>
public sealed record TreeView(
    int Ancestry,
    int Progeny,
    bool ShowSiblings,
    LineageScope Lineage,
    TreeOrientation Orientation);

/// <summary>Passed to a host that wants to supply a picture instead of a pasted address.</summary>
public sealed record AvatarRequest(string? PersonId, string? CurrentUrl);

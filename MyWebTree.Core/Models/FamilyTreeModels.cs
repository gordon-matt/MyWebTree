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

    /// <summary>
    /// Which set of parents a new father or mother joins. Leave it null to use the person's primary set,
    /// or to start a primary birth set when they have none.
    /// </summary>
    public ParentSetTarget? ParentSet { get; init; }
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
    /// Neither person was deleted. For a parent link, <see cref="FamilyTreeChange.ParentSetId"/> and
    /// <see cref="FamilyTreeChange.Type"/> name the set the parent was taken out of. A set left with nobody in it
    /// is gone from <see cref="FamilyTreeChange.People"/>.
    /// </summary>
    Unlinked,

    /// <summary>
    /// A link was added between <see cref="FamilyTreeChange.PersonId"/> (the person who was selected)
    /// and <see cref="FamilyTreeChange.OtherPersonId"/> (the person who was clicked).
    /// <see cref="FamilyTreeChange.Link"/> says how the second person relates to the first.
    /// A parent who joined a set the child already had is reported here, with
    /// <see cref="FamilyTreeChange.ParentSetId"/> and <see cref="FamilyTreeChange.Type"/> naming that set.
    /// A parent who started a new set is reported as <see cref="ParentSetAdded"/> instead.
    /// Nobody was created or deleted.
    /// </summary>
    Linked,

    /// <summary>The centered person changed. Nobody's data changed.</summary>
    FocusChanged,

    /// <summary>
    /// A new set of parents was started for <see cref="FamilyTreeChange.PersonId"/> (the child) by linking an
    /// existing person as a parent. <see cref="FamilyTreeChange.OtherPersonId"/> is that parent, and
    /// <see cref="FamilyTreeChange.ParentSetId"/> and <see cref="FamilyTreeChange.Type"/> describe the set.
    /// A new person added as a father or mother is reported as <see cref="Added"/>.
    /// </summary>
    ParentSetAdded,

    /// <summary>
    /// The type, the date, or the primary flag of a set of parents changed. <see cref="FamilyTreeChange.PersonId"/> is the child.
    /// When another set stopped being primary, read it from <see cref="FamilyTreeChange.People"/>.
    /// </summary>
    ParentSetUpdated,

    /// <summary>A whole set of parents was removed from <see cref="FamilyTreeChange.PersonId"/> (the child). The parents stay.</summary>
    ParentSetRemoved,
}

/// <summary>
/// What the tree UI hands back after an edit, an undo, or a change of focus.
/// <see cref="ParentSetId"/> and <see cref="Type"/> are filled for <see cref="FamilyTreeChangeKind.Linked"/> and
/// <see cref="FamilyTreeChangeKind.Unlinked"/> when the link is a parent link, and for the three <c>ParentSet*</c> kinds.
/// A set id is only unique among the sets of one child, so read it together with the child.
/// </summary>
public sealed record FamilyTreeChange(
    IReadOnlyList<PersonData> People,
    string? FocusPersonId,
    FamilyTreeChangeKind Kind = FamilyTreeChangeKind.Snapshot,
    string? PersonId = null,
    string? OtherPersonId = null,
    LinkKind? Link = null,
    string? ParentSetId = null,
    ParentageType? Type = null);
/// <summary>Generations of ancestors and descendants the tree is showing. Raised when the user changes either.</summary>
public sealed record TreeDepths(int Ancestry, int Progeny);

/// <summary>
/// The toolbar choices a host can remember: how many generations, whether siblings are shown,
/// which side of the family, whether the chart runs down or across, and whether adopted and foster links are hidden.
/// </summary>
public sealed record TreeView(
    int Ancestry,
    int Progeny,
    bool ShowSiblings,
    LineageScope Lineage,
    TreeOrientation Orientation,
    bool BloodRelativesOnly = false);

/// <summary>Passed to a host that wants to supply a picture instead of a pasted address.</summary>
public sealed record AvatarRequest(string? PersonId, string? CurrentUrl);

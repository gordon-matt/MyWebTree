namespace MyWebTree.Models;

/// <summary>
/// One person and the ids of the people around them. Ids are strings so a layout can
/// also carry placeholder cards that have never been saved.
/// </summary>
public sealed record PersonData
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public required string Id { get; init; }

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

    /// <summary>
    /// Gets or sets a value indicating whether this instance is placeholder.
    /// </summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>
    /// Gets or sets the placeholder.
    /// </summary>
    public PlaceholderKind? Placeholder { get; init; }

    /// <summary>The person a placeholder card would be attached to.</summary>
    public string? PlaceholderForId { get; init; }

    /// <summary>
    /// Gets or sets the parents. When <see cref="ParentSets"/> is used this always equals the members of the
    /// primary set, and a host should treat it as read-only derived data.
    /// </summary>
    public IReadOnlyList<string> Parents { get; init; } = [];

    /// <summary>
    /// Gets or sets every set of parents this person has. A host that only knows <see cref="Parents"/> can leave
    /// this empty: <see cref="Parents"/> is then read as one primary <see cref="ParentageType.Birth"/> set.
    /// </summary>
    public IReadOnlyList<ParentSetData> ParentSets { get; init; } = [];

    /// <summary>
    /// Gets or sets the spouses.
    /// </summary>
    public IReadOnlyList<string> Spouses { get; init; } = [];

    /// <summary>
    /// Gets or sets the children. This lists every child the person has in any set, whether the child's
    /// set with them is birth, adopted, or foster. The child's own <see cref="ParentSets"/> says which kind.
    /// </summary>
    public IReadOnlyList<string> Children { get; init; } = [];

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName
    {
        get
        {
            string name = $"{FirstName} {LastName}".Trim();
            return name.Length == 0 ? "Unnamed" : name;
        }
    }

    /// <summary>
    /// Gets the initials.
    /// </summary>
    public string Initials
    {
        get
        {
            char first = FirstName.FirstOrDefault(char.IsLetter);
            char last = LastName.FirstOrDefault(char.IsLetter);
            if (first == '\0' && last == '\0')
            {
                return "?";
            }

            return $"{CharOrEmpty(first)}{CharOrEmpty(last)}".ToUpperInvariant();
        }
    }

    /// <summary>
    /// Gets the lifespan.
    /// </summary>
    public string Lifespan
    {
        get
        {
            if (BirthDate is null && DeathDate is null)
            {
                return string.Empty;
            }

            string birth = BirthDate?.Year.ToString() ?? string.Empty;
            return DeathDate is null ? birth : $"{birth} – {DeathDate.Value.Year}";
        }
    }

    private static string CharOrEmpty(char value) => value == '\0' ? string.Empty : value.ToString();
}

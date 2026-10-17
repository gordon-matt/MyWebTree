namespace MyWebTree.Models;

/// <summary>
/// One person and the ids of the people around them. Ids are strings so a layout can
/// also carry placeholder cards that have never been saved.
/// </summary>
public sealed record PersonData
{
    public required string Id { get; init; }

    public Gender Gender { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    public DateOnly? DeathDate { get; init; }

    public string? BirthPlace { get; init; }

    public string? AvatarUrl { get; init; }

    public string? Notes { get; init; }

    public bool IsPlaceholder { get; init; }

    public PlaceholderKind? Placeholder { get; init; }

    /// <summary>The person a placeholder card would be attached to.</summary>
    public string? PlaceholderForId { get; init; }

    public IReadOnlyList<string> Parents { get; init; } = [];

    public IReadOnlyList<string> Spouses { get; init; } = [];

    public IReadOnlyList<string> Children { get; init; } = [];

    public string DisplayName
    {
        get
        {
            string name = $"{FirstName} {LastName}".Trim();
            return name.Length == 0 ? "Unnamed" : name;
        }
    }

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

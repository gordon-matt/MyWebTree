namespace MyWebTree.Models;

/// <summary>
/// How a person's name is shown. The stored given names and family name are left as entered.
/// </summary>
public static class PersonName
{
    /// <summary>
    /// Joins the name parts in display order. When <paramref name="capitalizeFamilyName"/> is set, only the
    /// family name is uppercased, and only in this result.
    /// </summary>
    public static string Format(
        string? givenNames,
        string? familyName,
        bool isFamilyNameFirst,
        bool capitalizeFamilyName,
        string? prefix = null,
        string? suffix = null)
    {
        string given = givenNames?.Trim() ?? string.Empty;
        string family = familyName?.Trim() ?? string.Empty;
        if (capitalizeFamilyName && family.Length > 0)
        {
            family = family.ToUpperInvariant();
        }

        string core = isFamilyNameFirst ? Join(family, given) : Join(given, family);
        return Join(prefix, core, suffix);
    }

    /// <summary>
    /// True when the query is empty or appears in the name in either order. Capitalization is ignored.
    /// </summary>
    public static bool Matches(string? query, string? givenNames, string? familyName)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string term = query.Trim();
        return Format(givenNames, familyName, isFamilyNameFirst: false, capitalizeFamilyName: false)
                .Contains(term, StringComparison.OrdinalIgnoreCase)
            || Format(givenNames, familyName, isFamilyNameFirst: true, capitalizeFamilyName: false)
                .Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static string Join(params string?[] parts) =>
        string.Join(' ', parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}

using MyWebTree.Models;

namespace MyWebTree.Graph;

/// <summary>
/// The pure rules for sets of parents: which one is primary by default, how a host that only knows
/// <see cref="PersonData.Parents"/> is read, and how the active set is chosen.
/// </summary>
public static class ParentSetRules
{
    /// <summary>
    /// Picks the primary set. A set flagged <see cref="ParentSetData.IsPrimary"/> always wins (the first one, if
    /// several are flagged). Otherwise the order is Adopted, then Birth, then Foster, and ties go to the set
    /// that appears first in the list.
    /// </summary>
    /// <param name="sets">The sets of one person.</param>
    /// <returns>The primary set, or null when there are none.</returns>
    public static ParentSetData? ChoosePrimary(IReadOnlyList<ParentSetData> sets)
    {
        ParentSetData? best = null;
        int bestRank = int.MaxValue;
        foreach (var set in sets)
        {
            if (set.IsPrimary)
            {
                return set;
            }

            int rank = DefaultRank(set.Type);
            if (rank < bestRank)
            {
                best = set;
                bestRank = rank;
            }
        }

        return best;
    }

    /// <summary>The sets a person effectively has: their <see cref="PersonData.ParentSets"/>, or one implicit primary birth set made from <see cref="PersonData.Parents"/>.</summary>
    /// <param name="person">The person.</param>
    /// <returns>The sets, with exactly one marked primary when there are any.</returns>
    public static IReadOnlyList<ParentSetData> SetsOf(PersonData person) => NormalizePerson(person).ParentSets;

    /// <summary>The primary set of a person, or null when they have no parents.</summary>
    /// <param name="person">The person.</param>
    /// <returns>The primary set.</returns>
    public static ParentSetData? PrimaryOf(PersonData person) => ChoosePrimary(SetsOf(person));

    /// <summary>
    /// The set drawn for a person: the one the host remembered if it still exists, otherwise the primary set.
    /// </summary>
    /// <param name="person">The person.</param>
    /// <param name="activeSetByPerson">Person id to set id, as the host remembered it. May be null.</param>
    /// <returns>The active set, or null when the person has no parents.</returns>
    public static ParentSetData? ActiveOf(PersonData person, IReadOnlyDictionary<string, string>? activeSetByPerson)
    {
        var sets = SetsOf(person);
        if (activeSetByPerson is not null
            && activeSetByPerson.TryGetValue(person.Id, out string? setId)
            && sets.FirstOrDefault(set => string.Equals(set.Id, setId, StringComparison.Ordinal)) is { } chosen)
        {
            return chosen;
        }

        return ChoosePrimary(sets);
    }

    /// <summary>
    /// Brings every person into one shape: a legacy <see cref="PersonData.Parents"/> list becomes one implicit
    /// primary birth set, empty sets are dropped, exactly one set is primary, and <see cref="PersonData.Parents"/>
    /// equals the members of the primary set. A person who needs no change is returned as is.
    /// </summary>
    /// <param name="people">The people.</param>
    /// <returns>The people in the normalized shape.</returns>
    public static IReadOnlyList<PersonData> Normalize(IReadOnlyList<PersonData> people)
    {
        List<PersonData>? changed = null;
        for (int i = 0; i < people.Count; i++)
        {
            var normalized = NormalizePerson(people[i]);
            if (!ReferenceEquals(normalized, people[i]))
            {
                changed ??= [.. people];
                changed[i] = normalized;
            }
        }

        return changed ?? people;
    }

    /// <summary>Normalizes one person. See <see cref="Normalize"/>.</summary>
    /// <param name="person">The person.</param>
    /// <returns>The person, or a corrected copy.</returns>
    public static PersonData NormalizePerson(PersonData person)
    {
        List<ParentSetData> sets;
        if (person.ParentSets.Count == 0)
        {
            if (person.Parents.Count == 0)
            {
                return person;
            }

            sets =
            [
                new ParentSetData
                {
                    Id = ImplicitSetId(person.Id),
                    Type = ParentageType.Birth,
                    IsPrimary = true,
                    ParentIds = person.Parents,
                },
            ];
        }
        else
        {
            sets = [];
            for (int i = 0; i < person.ParentSets.Count; i++)
            {
                var set = person.ParentSets[i];
                if (set.ParentIds.Count == 0)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(set.Id))
                {
                    set = set with { Id = ImplicitSetId(person.Id) + "-" + i };
                }

                // A birth set takes its date from the child's own birth, so a date stored on the set is dropped.
                if (set.Type == ParentageType.Birth && set.Date is not null)
                {
                    set = set with { Date = null };
                }

                sets.Add(set);
            }
        }

        var primary = ChoosePrimary(sets);
        for (int i = 0; i < sets.Count; i++)
        {
            bool isPrimary = ReferenceEquals(sets[i], primary);
            if (sets[i].IsPrimary != isPrimary)
            {
                sets[i] = sets[i] with { IsPrimary = isPrimary };
            }
        }

        var parents = primary is null ? [] : primary.ParentIds;
        if (SameSets(person.ParentSets, sets) && person.Parents.SequenceEqual(parents, StringComparer.Ordinal))
        {
            return person;
        }

        return person with { ParentSets = sets, Parents = parents.ToList() };
    }

    /// <summary>Every parent a person has, across all of their sets.</summary>
    /// <param name="person">The person.</param>
    /// <returns>The distinct parent ids.</returns>
    public static IEnumerable<string> AllParentIds(PersonData person) =>
        SetsOf(person).SelectMany(set => set.ParentIds).Distinct(StringComparer.Ordinal);

    /// <summary>The id given to the set made from a legacy <see cref="PersonData.Parents"/> list.</summary>
    /// <param name="personId">The child.</param>
    /// <returns>A stable set id.</returns>
    public static string ImplicitSetId(string personId) => "set-" + personId;

    internal static string NewSetId() => "ps-" + Guid.NewGuid().ToString("N");

    /// <summary>True when two sets have the same members (in any order) and the same type.</summary>
    internal static bool SameFamily(ParentSetData left, ParentSetData right) =>
        left.Type == right.Type
        && left.ParentIds.Count == right.ParentIds.Count
        && left.ParentIds.All(id => right.ParentIds.Contains(id, StringComparer.Ordinal));

    private static int DefaultRank(ParentageType type) => type switch
    {
        ParentageType.Adopted => 0,
        ParentageType.Birth => 1,
        _ => 2,
    };

    private static bool SameSets(IReadOnlyList<ParentSetData> left, IReadOnlyList<ParentSetData> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int i = 0; i < left.Count; i++)
        {
            if (!ReferenceEquals(left[i], right[i]))
            {
                return false;
            }
        }

        return true;
    }
}

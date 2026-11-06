using MyWebTree.Models;

namespace MyWebTree.Graph;

internal static class FamilyLinks
{
    public static void Spouse(Dictionary<string, PersonData> people, string a, string b)
    {
        if (a == b || !people.ContainsKey(a) || !people.ContainsKey(b))
        {
            return;
        }

        Add(people, a, b, static (person, id) => person.Spouses.Contains(id)
            ? person
            : person with { Spouses = [.. person.Spouses, id] });
        Add(people, b, a, static (person, id) => person.Spouses.Contains(id)
            ? person
            : person with { Spouses = [.. person.Spouses, id] });
    }

    public static void Parent(Dictionary<string, PersonData> people, string parentId, string childId)
    {
        if (parentId == childId || !people.ContainsKey(parentId) || !people.ContainsKey(childId))
        {
            return;
        }

        Add(people, parentId, childId, static (person, id) => person.Children.Contains(id)
            ? person
            : person with { Children = [.. person.Children, id] });
        Add(people, childId, parentId, static (person, id) => person.Parents.Contains(id)
            ? person
            : person with { Parents = [.. person.Parents, id] });
    }

    public static void Unspouse(Dictionary<string, PersonData> people, string a, string b)
    {
        if (!people.ContainsKey(a) || !people.ContainsKey(b))
        {
            return;
        }

        people[a] = people[a] with { Spouses = people[a].Spouses.Where(id => id != b).ToArray() };
        people[b] = people[b] with { Spouses = people[b].Spouses.Where(id => id != a).ToArray() };
    }

    public static void Unparent(Dictionary<string, PersonData> people, string parentId, string childId)
    {
        if (!people.ContainsKey(parentId) || !people.ContainsKey(childId))
        {
            return;
        }

        people[parentId] = people[parentId] with { Children = people[parentId].Children.Where(id => id != childId).ToArray() };
        people[childId] = people[childId] with { Parents = people[childId].Parents.Where(id => id != parentId).ToArray() };
    }

    /// <summary>
    /// Replaces a child's sets and keeps every parent's <see cref="PersonData.Children"/> in step: a parent who
    /// is in no set of the child any more stops listing them, and a new parent starts. Empty sets are dropped
    /// and a primary set is chosen when none is flagged.
    /// </summary>
    public static void SetParentSets(Dictionary<string, PersonData> people, string childId, IReadOnlyList<ParentSetData> sets)
    {
        var child = people[childId];
        var before = ParentSetRules.AllParentIds(child).ToHashSet(StringComparer.Ordinal);
        var next = ParentSetRules.NormalizePerson(child with { ParentSets = sets, Parents = [] });
        people[childId] = next;
        var after = ParentSetRules.AllParentIds(next).ToHashSet(StringComparer.Ordinal);

        foreach (string parentId in before.Except(after))
        {
            if (people.TryGetValue(parentId, out var parent))
            {
                people[parentId] = parent with { Children = parent.Children.Where(item => item != childId).ToArray() };
            }
        }

        foreach (string parentId in after.Except(before))
        {
            if (people.TryGetValue(parentId, out var parent) && !parent.Children.Contains(childId))
            {
                people[parentId] = parent with { Children = [.. parent.Children, childId] };
            }
        }
    }

    public static void Unlink(Dictionary<string, PersonData> people, string id)
    {
        foreach (string otherId in people.Keys.ToList())
        {
            if (otherId == id)
            {
                continue;
            }

            var other = people[otherId];
            var sets = ParentSetRules.SetsOf(other)
                .Select(set => set with { ParentIds = set.ParentIds.Where(item => item != id).ToArray() })
                .Where(set => set.ParentIds.Count > 0)
                .ToList();
            MergeIdentical(sets);
            people[otherId] = ParentSetRules.NormalizePerson(other with
            {
                ParentSets = sets,
                Parents = [],
                Spouses = other.Spouses.Where(item => item != id).ToArray(),
                Children = other.Children.Where(item => item != id).ToArray(),
            });
        }

        people.Remove(id);
    }

    /// <summary>
    /// Removing a parent can leave two sets with the same members and type. They are one family, so the later one
    /// is dropped, and the survivor stays primary if either was.
    /// </summary>
    private static void MergeIdentical(List<ParentSetData> sets)
    {
        for (int i = 0; i < sets.Count; i++)
        {
            for (int j = sets.Count - 1; j > i; j--)
            {
                if (!ParentSetRules.SameFamily(sets[i], sets[j]))
                {
                    continue;
                }

                if (sets[j].IsPrimary && !sets[i].IsPrimary)
                {
                    sets[i] = sets[i] with { IsPrimary = true };
                }

                sets.RemoveAt(j);
            }
        }
    }

    private static void Add(
        Dictionary<string, PersonData> people,
        string ownerId,
        string otherId,
        Func<PersonData, string, PersonData> include)
    {
        people[ownerId] = include(people[ownerId], otherId);
    }
}

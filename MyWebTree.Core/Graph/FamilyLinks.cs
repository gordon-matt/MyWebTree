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

    public static void Unlink(Dictionary<string, PersonData> people, string id)
    {
        foreach (string otherId in people.Keys.ToList())
        {
            if (otherId == id)
            {
                continue;
            }

            var other = people[otherId];
            people[otherId] = other with
            {
                Parents = other.Parents.Where(item => item != id).ToArray(),
                Spouses = other.Spouses.Where(item => item != id).ToArray(),
                Children = other.Children.Where(item => item != id).ToArray(),
            };
        }

        people.Remove(id);
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

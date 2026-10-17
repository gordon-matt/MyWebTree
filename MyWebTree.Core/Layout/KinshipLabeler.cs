using MyWebTree.Models;

namespace MyWebTree.Layout;

/// <summary>Short labels ("Sister", "Grandfather") for the people drawn around the focus person.</summary>
public static class KinshipLabeler
{
    public static IReadOnlyDictionary<string, string> Label(IReadOnlyDictionary<string, PersonData> people, string focusId)
    {
        var labels = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!people.TryGetValue(focusId, out var focus))
        {
            return labels;
        }

        foreach (string id in people.Keys)
        {
            if (id == focusId || people[id].IsPlaceholder)
            {
                continue;
            }

            string label = Describe(people, focus, people[id]);
            if (label.Length > 0)
            {
                labels[id] = label;
            }
        }

        return labels;
    }

    private static string Describe(IReadOnlyDictionary<string, PersonData> people, PersonData focus, PersonData person)
    {
        if (focus.Spouses.Contains(person.Id))
        {
            return Word(person.Gender, "Husband", "Wife", "Spouse");
        }

        if (focus.Parents.Contains(person.Id))
        {
            return Word(person.Gender, "Father", "Mother", "Parent");
        }

        if (focus.Children.Contains(person.Id))
        {
            return Word(person.Gender, "Son", "Daughter", "Child");
        }

        if (Siblings(people, focus.Id).Contains(person.Id))
        {
            return Word(person.Gender, "Brother", "Sister", "Sibling");
        }

        if (focus.Parents.Any(parentId => people.TryGetValue(parentId, out var parent) && parent.Parents.Contains(person.Id)))
        {
            return Word(person.Gender, "Grandfather", "Grandmother", "Grandparent");
        }

        if (focus.Children.Any(childId => people.TryGetValue(childId, out var child) && child.Children.Contains(person.Id)))
        {
            return Word(person.Gender, "Grandson", "Granddaughter", "Grandchild");
        }

        var aunts = focus.Parents.SelectMany(parentId => Siblings(people, parentId)).ToHashSet(StringComparer.Ordinal);
        if (aunts.Contains(person.Id))
        {
            return Word(person.Gender, "Uncle", "Aunt", "Aunt or uncle");
        }

        if (Siblings(people, focus.Id).Any(siblingId => people.TryGetValue(siblingId, out var sibling) && sibling.Children.Contains(person.Id)))
        {
            return Word(person.Gender, "Nephew", "Niece", "Nibling");
        }

        if (aunts.Any(auntId => people.TryGetValue(auntId, out var aunt) && aunt.Children.Contains(person.Id)))
        {
            return "Cousin";
        }

        if (focus.Children.Any(childId => people.TryGetValue(childId, out var child) && child.Spouses.Contains(person.Id)))
        {
            return Word(person.Gender, "Son-in-law", "Daughter-in-law", "Child-in-law");
        }

        if (focus.Spouses.Any(spouseId => people.TryGetValue(spouseId, out var spouse) && spouse.Parents.Contains(person.Id)))
        {
            return Word(person.Gender, "Father-in-law", "Mother-in-law", "Parent-in-law");
        }

        if (Siblings(people, focus.Id).Any(siblingId => people.TryGetValue(siblingId, out var sibling) && sibling.Spouses.Contains(person.Id)))
        {
            return Word(person.Gender, "Brother-in-law", "Sister-in-law", "Sibling-in-law");
        }

        return string.Empty;
    }

    private static HashSet<string> Siblings(IReadOnlyDictionary<string, PersonData> people, string id)
    {
        var siblings = new HashSet<string>(StringComparer.Ordinal);
        if (!people.TryGetValue(id, out var person))
        {
            return siblings;
        }

        foreach (string parentId in person.Parents)
        {
            if (!people.TryGetValue(parentId, out var parent))
            {
                continue;
            }

            foreach (string childId in parent.Children)
            {
                if (childId != id)
                {
                    siblings.Add(childId);
                }
            }
        }

        return siblings;
    }

    private static string Word(Gender gender, string male, string female, string neutral) => gender switch
    {
        Gender.Male => male,
        Gender.Female => female,
        _ => neutral,
    };
}

using MyWebTree.Models;

namespace MyWebTree.Graph;

/// <summary>A fictional four-generation family used by the demo and the layout tests.</summary>
public static class SampleFamily
{
    public const string FocusId = "david";

    public const string Name = "Ward family";

    public static IReadOnlyList<PersonData> Create()
    {
        var people = new Dictionary<string, PersonData>(StringComparer.Ordinal);

        Add(people, "arthur", Gender.Male, "Arthur", "Ward", 1924, 2001, "Bristol");
        Add(people, "helen", Gender.Female, "Helen", "Ward", 1928, 2014, "Bath");
        Add(people, "margaret", Gender.Female, "Margaret", "Hale", 1950, null, "Bristol");
        Add(people, "robert", Gender.Male, "Robert", "Hale", 1948, 2019, "Cardiff");
        Add(people, "david", Gender.Male, "David", "Ward", 1953, null, "Bristol");
        Add(people, "priya", Gender.Female, "Priya", "Ward", 1956, null, "Pune");
        Add(people, "ellen", Gender.Female, "Ellen", "Ward", 1958, null, "Bristol");
        Add(people, "claire", Gender.Female, "Claire", "Hale", 1976, null, "Cardiff");
        Add(people, "simon", Gender.Male, "Simon", "Hale", 1979, null, "Cardiff");
        Add(people, "noah", Gender.Male, "Noah", "Bennett", 1981, null, "London");
        Add(people, "aisha", Gender.Female, "Aisha", "Ward", 1982, null, "Bristol");
        Add(people, "omar", Gender.Male, "Omar", "Ward", 1985, null, "Bristol");
        Add(people, "lena", Gender.Female, "Lena", "Ortiz", 1987, null, "Málaga");
        Add(people, "maya", Gender.Female, "Maya", "Hale", 2012, null, "London");
        Add(people, "leo", Gender.Male, "Leo", "Ward", 2016, null, "Bristol");
        Add(people, "sofia", Gender.Female, "Sofia", "Ward", 2019, null, "Bristol");

        people["david"] = people["david"] with { Notes = "Keeps the family photographs and the old parish copies." };

        FamilyLinks.Spouse(people, "arthur", "helen");
        FamilyLinks.Spouse(people, "margaret", "robert");
        FamilyLinks.Spouse(people, "david", "priya");
        FamilyLinks.Spouse(people, "simon", "noah");
        FamilyLinks.Spouse(people, "omar", "lena");

        FamilyLinks.Parent(people, "arthur", "margaret");
        FamilyLinks.Parent(people, "helen", "margaret");
        FamilyLinks.Parent(people, "arthur", "david");
        FamilyLinks.Parent(people, "helen", "david");
        FamilyLinks.Parent(people, "arthur", "ellen");
        FamilyLinks.Parent(people, "helen", "ellen");

        FamilyLinks.Parent(people, "margaret", "claire");
        FamilyLinks.Parent(people, "robert", "claire");
        FamilyLinks.Parent(people, "margaret", "simon");
        FamilyLinks.Parent(people, "robert", "simon");
        FamilyLinks.Parent(people, "simon", "maya");
        FamilyLinks.Parent(people, "noah", "maya");

        FamilyLinks.Parent(people, "david", "aisha");
        FamilyLinks.Parent(people, "priya", "aisha");
        FamilyLinks.Parent(people, "david", "omar");
        FamilyLinks.Parent(people, "priya", "omar");
        FamilyLinks.Parent(people, "omar", "leo");
        FamilyLinks.Parent(people, "lena", "leo");
        FamilyLinks.Parent(people, "omar", "sofia");
        FamilyLinks.Parent(people, "lena", "sofia");

        return people.Values.ToList();
    }

    private static void Add(
        Dictionary<string, PersonData> people,
        string id,
        Gender gender,
        string first,
        string last,
        int? birth,
        int? death,
        string? place)
    {
        people[id] = new PersonData
        {
            Id = id,
            Gender = gender,
            FirstName = first,
            LastName = last,
            BirthDate = birth is null ? null : new PartialDate(birth.Value),
            DeathDate = death is null ? null : new PartialDate(death.Value),
            BirthPlace = place,
        };
    }
}

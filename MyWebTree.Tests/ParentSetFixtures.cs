namespace MyWebTree.Tests;

/// <summary>Small families with more than one set of parents, written out by hand so a test can see exactly what it is checking.</summary>
internal static class ParentSetFixtures
{
    public const string ChildId = "kim";

    public const string BirthSetId = "kim-birth";

    public const string AdoptedSetId = "kim-adopted";

    /// <summary>
    /// Kim was born to Bob and Beth Ross and adopted by Alan and Amy Cole. Kim's own record says the adopted
    /// set is a plain, unflagged set, so the default ordering decides which one is primary.
    /// </summary>
    public static List<PersonData> BirthAndAdopted(bool flagBirthPrimary = false) =>
    [
        Person("bob", "Bob", "Ross", Gender.Male, 1960, spouses: ["beth"], children: [ChildId]),
        Person("beth", "Beth", "Ross", Gender.Female, 1962, spouses: ["bob"], children: [ChildId]),
        Person("alan", "Alan", "Cole", Gender.Male, 1958, spouses: ["amy"], children: [ChildId]),
        Person("amy", "Amy", "Cole", Gender.Female, 1961, spouses: ["alan"], children: [ChildId]),
        Person(ChildId, "Kim", "Ross", Gender.Female, 1985, parents: ["bob", "beth"]) with
        {
            ParentSets =
            [
                new ParentSetData { Id = BirthSetId, Type = ParentageType.Birth, IsPrimary = flagBirthPrimary, ParentIds = ["bob", "beth"] },
                new ParentSetData { Id = AdoptedSetId, Type = ParentageType.Adopted, ParentIds = ["alan", "amy"], Date = new PartialDate(1987) },
            ],
        },
    ];

    /// <summary>Grandparent Gus has two children, Pat and Ann. Pat's child is Cal.</summary>
    public static List<PersonData> Relatives() =>
    [
        Person("gus", "Gus", "Hale", Gender.Male, 1930, children: ["pat", "ann"]),
        Person("pat", "Pat", "Hale", Gender.Male, 1955, parents: ["gus"], children: ["cal"]),
        Person("ann", "Ann", "Hale", Gender.Female, 1958, parents: ["gus"]),
        Person("cal", "Cal", "Hale", Gender.Male, 1985, parents: ["pat"]),
    ];

    public static PersonData Person(
        string id,
        string first,
        string last,
        Gender gender,
        int born,
        string[]? parents = null,
        string[]? spouses = null,
        string[]? children = null) => new()
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Gender = gender,
            BirthDate = new PartialDate(born),
            Parents = parents ?? [],
            Spouses = spouses ?? [],
            Children = children ?? [],
        };

    public static PersonData Find(IReadOnlyList<PersonData> people, string id) => people.Single(person => person.Id == id);
}

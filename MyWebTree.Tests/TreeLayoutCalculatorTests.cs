namespace MyWebTree.Tests;

public class TreeLayoutCalculatorTests
{
    [Fact]
    public void Spouses_share_a_row_and_children_sit_below()
    {
        var layout = TreeLayoutCalculator.Calculate(SampleFamily.Create(), new TreeLayoutOptions
        {
            FocusId = SampleFamily.FocusId,
            ShowSiblings = true,
            AncestryDepth = 4,
            ProgenyDepth = 4,
        });

        var arthur = Card(layout, "arthur");
        var helen = Card(layout, "helen");
        var david = Card(layout, "david");
        var omar = Card(layout, "omar");

        Assert.Equal(arthur.Y, helen.Y, precision: 1);
        Assert.True(helen.X > arthur.X);
        Assert.True(david.Y > arthur.Y);
        Assert.True(omar.Y > david.Y);
        Assert.Equal("Father", layout.Kinship["arthur"]);
        Assert.Equal("Sister", layout.Kinship["margaret"]);
        Assert.Equal("Wife", layout.Kinship["priya"]);
        Assert.Equal("Grandson", layout.Kinship["leo"]);
        Assert.Equal("Brother-in-law", layout.Kinship["robert"]);
    }

    [Fact]
    public void Cards_do_not_overlap_vertically_or_horizontally()
    {
        var people = SampleFamily.Create();
        AssertNoOverlap(TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions { FocusId = SampleFamily.FocusId, ShowPlaceholders = true }));
        AssertNoOverlap(TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions
        {
            FocusId = SampleFamily.FocusId,
            Orientation = TreeOrientation.Horizontal,
            ShowPlaceholders = true,
        }));
    }

    [Fact]
    public void Placeholders_offer_a_missing_parent_and_an_extra_spouse()
    {
        var layout = TreeLayoutCalculator.Calculate(SampleFamily.Create(), new TreeLayoutOptions
        {
            FocusId = "ellen",
            ShowPlaceholders = true,
            ShowSiblings = false,
        });

        Assert.Contains(layout.Cards, card => card.Placeholder == PlaceholderKind.Spouse && card.RelatesToId == "ellen");
        Assert.Contains(layout.Cards, card => card.Placeholder == PlaceholderKind.Child && card.RelatesToId == "ellen");
        Assert.DoesNotContain(layout.Cards, card => card.Placeholder == PlaceholderKind.Father);
    }

    [Fact]
    public void Bilateral_draws_the_mother_and_father_lines()
    {
        var people = TwoSidedFamily();
        var layout = TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions
        {
            FocusId = "child",
            AncestryDepth = 2,
            ProgenyDepth = 0,
            ShowSiblings = true,
            Lineage = LineageScope.Bilateral,
        });

        Assert.Contains(layout.Cards, card => card.Id == "paternal-grandfather");
        Assert.Contains(layout.Cards, card => card.Id == "maternal-grandmother");
        AssertNoOverlap(layout);
    }

    [Fact]
    public void Patrilineal_stops_at_the_fathers_line()
    {
        var people = TwoSidedFamily();
        var layout = TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions
        {
            FocusId = "child",
            AncestryDepth = 2,
            ProgenyDepth = 0,
            ShowSiblings = true,
            Lineage = LineageScope.Patrilineal,
        });

        Assert.Contains(layout.Cards, card => card.Id == "paternal-grandfather");
        Assert.DoesNotContain(layout.Cards, card => card.Id == "maternal-grandmother");
    }

    [Fact]
    public void A_wife_with_no_ancestors_sits_beside_her_husband()
    {
        var layout = TreeLayoutCalculator.Calculate(GordonFamily(), new TreeLayoutOptions
        {
            FocusId = "matthew",
            AncestryDepth = 4,
            ProgenyDepth = 2,
            Lineage = LineageScope.Bilateral,
        });

        var rupert = Card(layout, "rupert");
        var francesa = Card(layout, "francesa");
        var george = Card(layout, "george");
        var bruce = Card(layout, "bruce");
        var christina = Card(layout, "christina");

        Assert.Equal(rupert.Y, francesa.Y, precision: 1);
        Assert.Equal(TreeLayoutCalculator.SpouseGap, SpouseGap(rupert, francesa), precision: 1);
        Assert.True(george.Y < rupert.Y);
        Assert.Equal(bruce.Y, christina.Y, precision: 1);
        Assert.True(bruce.Y > rupert.Y);
        Assert.DoesNotContain(layout.Cards, card => card.Id == "christopher");
        AssertNoOverlap(layout);
    }

    [Fact]
    public void Ancestor_blocks_on_the_same_row_stay_apart()
    {
        var layout = TreeLayoutCalculator.Calculate(WideAncestors(), new TreeLayoutOptions
        {
            FocusId = "child",
            AncestryDepth = 3,
            ProgenyDepth = 0,
        });

        Assert.Equal(Card(layout, "dad").Y, Card(layout, "mom").Y, precision: 1);
        Assert.Equal(Card(layout, "pat-gf").Y, Card(layout, "mat-gf").Y, precision: 1);
        AssertNoOverlap(layout);
    }

    private static List<PersonData> TwoSidedFamily() =>
    [
        new()
        {
            Id = "child", Gender = Gender.Male, FirstName = "Child", LastName = "Family",
            Parents = ["father", "mother"],
        },
        new()
        {
            Id = "father", Gender = Gender.Male, FirstName = "Father", LastName = "Family",
            Parents = ["paternal-grandfather"], Spouses = ["mother"], Children = ["child"],
        },
        new()
        {
            Id = "mother", Gender = Gender.Female, FirstName = "Mother", LastName = "Family",
            Parents = ["maternal-grandmother"], Spouses = ["father"], Children = ["child"],
        },
        new()
        {
            Id = "paternal-grandfather", Gender = Gender.Male, FirstName = "Paternal", LastName = "Family",
            Children = ["father"],
        },
        new()
        {
            Id = "maternal-grandmother", Gender = Gender.Female, FirstName = "Maternal", LastName = "Family",
            Children = ["mother"],
        },
    ];

    private static List<PersonData> GordonFamily() =>
    [
        Person("matthew", Gender.Male, ["bruce", "christina"], ["ha"], ["aaron", "ariella"]),
        Person("ha", Gender.Female, [], ["matthew"], ["aaron", "ariella"]),
        Person("aaron", Gender.Male, ["matthew", "ha"], [], []),
        Person("ariella", Gender.Female, ["matthew", "ha"], [], []),
        Person("christopher", Gender.Male, ["bruce", "christina"], [], []),
        Person("bruce", Gender.Male, ["rupert", "francesa"], ["christina"], ["matthew", "christopher"]),
        Person("christina", Gender.Female, ["james", "phyllis"], ["bruce"], ["matthew", "christopher"]),
        Person("rupert", Gender.Male, ["alexander", "grace"], ["francesa"], ["bruce"]),
        Person("francesa", Gender.Female, [], ["rupert"], ["bruce"]),
        Person("james", Gender.Male, [], ["phyllis"], ["christina"]),
        Person("phyllis", Gender.Female, [], ["james"], ["christina"]),
        Person("alexander", Gender.Male, ["george", "catherine"], ["grace"], ["rupert"]),
        Person("grace", Gender.Female, ["rhode", "lund"], ["alexander"], ["rupert"]),
        Person("george", Gender.Male, [], ["catherine"], ["alexander"]),
        Person("catherine", Gender.Female, [], ["george"], ["alexander"]),
        Person("rhode", Gender.Male, [], ["lund"], ["grace"]),
        Person("lund", Gender.Female, [], ["rhode"], ["grace"]),
    ];

    private static List<PersonData> WideAncestors() =>
    [
        Person("child", Gender.Male, ["dad", "mom"], [], []),
        Person("dad", Gender.Male, ["pat-gf", "pat-gm"], ["mom"], ["child"]),
        Person("mom", Gender.Female, ["mat-gf", "mat-gm"], ["dad"], ["child"]),
        Person("pat-gf", Gender.Male, ["pat-ggf", "pat-ggm"], ["pat-gm"], ["dad"]),
        Person("pat-gm", Gender.Female, [], ["pat-gf"], ["dad"]),
        Person("pat-ggf", Gender.Male, [], ["pat-ggm"], ["pat-gf"]),
        Person("pat-ggm", Gender.Female, [], ["pat-ggf"], ["pat-gf"]),
        Person("mat-gf", Gender.Male, ["mat-ggf", "mat-ggm"], ["mat-gm"], ["mom"]),
        Person("mat-gm", Gender.Female, [], ["mat-gf"], ["mom"]),
        Person("mat-ggf", Gender.Male, [], ["mat-ggm"], ["mat-gf"]),
        Person("mat-ggm", Gender.Female, [], ["mat-ggf"], ["mat-gf"]),
    ];

    private static PersonData Person(
        string id,
        Gender gender,
        string[] parents,
        string[] spouses,
        string[] children) => new()
        {
            Id = id,
            Gender = gender,
            FirstName = id,
            Parents = [.. parents],
            Spouses = [.. spouses],
            Children = [.. children],
        };

    private static double SpouseGap(TreeCard left, TreeCard right) =>
        Math.Min(left.X, right.X) == left.X
            ? right.X - (left.X + TreeLayoutCalculator.CardWidth)
            : left.X - (right.X + TreeLayoutCalculator.CardWidth);

    private static void AssertNoOverlap(TreeLayoutResult layout)
    {
        var cards = layout.Cards;
        for (int i = 0; i < cards.Count; i++)
        {
            for (int j = i + 1; j < cards.Count; j++)
            {
                bool separated = cards[i].X + TreeLayoutCalculator.CardWidth <= cards[j].X + 0.5
                    || cards[j].X + TreeLayoutCalculator.CardWidth <= cards[i].X + 0.5
                    || cards[i].Y + TreeLayoutCalculator.CardHeight <= cards[j].Y + 0.5
                    || cards[j].Y + TreeLayoutCalculator.CardHeight <= cards[i].Y + 0.5;
                Assert.True(separated, $"{cards[i].Id} overlaps {cards[j].Id}");
            }
        }
    }

    private static TreeCard Card(TreeLayoutResult layout, string id) =>
        Assert.Single(layout.Cards, card => card.Id == id);
}

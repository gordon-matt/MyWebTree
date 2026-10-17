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

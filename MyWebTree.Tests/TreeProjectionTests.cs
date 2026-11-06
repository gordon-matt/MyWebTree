using static MyWebTree.Tests.ParentSetFixtures;

namespace MyWebTree.Tests;

public class TreeProjectionTests
{
    private static ProjectedNode Node(IReadOnlyList<ProjectedNode> nodes, string id) => nodes.Single(node => node.Person.Id == id);

    private static Dictionary<string, string> Active(string setId) => new() { [ChildId] = setId };

    [Fact]
    public void Projection_of_legacy_data_is_the_same_shape_as_the_input()
    {
        var people = SampleFamily.Create(withParentSets: false);

        var nodes = TreeProjection.Build(people, null);

        Assert.Equal(people.Count, nodes.Count);
        Assert.All(nodes, node => Assert.False(node.IsStub));
        foreach (var original in people)
        {
            var projected = Node(nodes, original.Id).Person;
            Assert.Equal(original.Parents, projected.Parents);
            Assert.Equal(original.Children, projected.Children);
            Assert.Equal(original.Spouses, projected.Spouses);
        }
    }

    [Fact]
    public void Each_person_takes_their_parents_from_the_active_set_only()
    {
        var people = BirthAndAdopted();

        var byDefault = TreeProjection.Build(people, null);
        var chosen = TreeProjection.Build(people, Active(BirthSetId));

        Assert.Equal(["alan", "amy"], Node(byDefault, ChildId).Person.Parents);
        Assert.Equal(AdoptedSetId, Node(byDefault, ChildId).ParentSetId);
        Assert.Equal(ParentageType.Adopted, Node(byDefault, ChildId).ParentType);
        Assert.Equal(2, Node(byDefault, ChildId).ParentSetCount);
        Assert.Equal(["bob", "beth"], Node(chosen, ChildId).Person.Parents);
        Assert.Equal(ParentageType.Birth, Node(chosen, ChildId).ParentType);
    }

    [Fact]
    public void A_non_active_set_becomes_a_stub_under_those_parents()
    {
        var nodes = TreeProjection.Build(BirthAndAdopted(), Active(BirthSetId));

        var stub = Assert.Single(nodes, node => node.IsStub);
        Assert.Equal(ChildId, stub.PersonId);
        Assert.Equal(AdoptedSetId, stub.ParentSetId);
        Assert.Equal(ParentageType.Adopted, stub.ParentType);
        Assert.Equal(["alan", "amy"], stub.Person.Parents);
        Assert.Empty(stub.Person.Children);
        Assert.Empty(stub.Person.Spouses);
        Assert.Contains(stub.Person.Id, Node(nodes, "alan").Person.Children);
        Assert.Contains(stub.Person.Id, Node(nodes, "amy").Person.Children);
        Assert.DoesNotContain(ChildId, Node(nodes, "alan").Person.Children);
        Assert.Contains(ChildId, Node(nodes, "bob").Person.Children);
    }

    [Fact]
    public void Switching_the_active_set_swaps_which_parents_hold_the_real_node_and_which_hold_the_stub()
    {
        var nodes = TreeProjection.Build(BirthAndAdopted(), null);

        var stub = Assert.Single(nodes, node => node.IsStub);
        Assert.Equal(ParentageType.Birth, stub.ParentType);
        Assert.Contains(ChildId, Node(nodes, "alan").Person.Children);
        Assert.DoesNotContain(ChildId, Node(nodes, "bob").Person.Children);
    }

    [Fact]
    public void A_parent_in_both_sets_keeps_the_real_child_and_gets_no_stub_of_their_own()
    {
        var people = BirthAndAdopted().Select(person => person.Id == ChildId
            ? person with
            {
                ParentSets =
                [
                    new ParentSetData { Id = "birth", Type = ParentageType.Birth, ParentIds = ["bob", "beth"] },
                    new ParentSetData { Id = "step", Type = ParentageType.Adopted, ParentIds = ["bob", "amy"] },
                ],
            }
            : person.Id == "amy" ? person with { Spouses = ["alan", "bob"] } : person).ToList();

        var nodes = TreeProjection.Build(people, Active("step"));

        var stub = Assert.Single(nodes, node => node.IsStub);
        Assert.Equal("birth", stub.ParentSetId);
        Assert.Contains(stub.Person.Id, Node(nodes, "beth").Person.Children);
        Assert.DoesNotContain(stub.Person.Id, Node(nodes, "bob").Person.Children);
        Assert.Contains(ChildId, Node(nodes, "bob").Person.Children);
    }

    [Fact]
    public void Blood_relatives_only_draws_the_birth_set_and_no_adopted_stubs()
    {
        var nodes = TreeProjection.Build(BirthAndAdopted(), Active(AdoptedSetId), new ProjectionOptions { BloodRelativesOnly = true });

        Assert.Equal(["bob", "beth"], Node(nodes, ChildId).Person.Parents);
        Assert.DoesNotContain(nodes, node => node.IsStub);
        Assert.DoesNotContain(ChildId, Node(nodes, "alan").Person.Children);
    }

    [Fact]
    public void Blood_relatives_only_hides_a_person_with_no_birth_set_from_their_adopters()
    {
        var people = Success(FamilyGraphEditor.RemoveParentSet(BirthAndAdopted(), ChildId, BirthSetId));

        var nodes = TreeProjection.Build(people, null, new ProjectionOptions { BloodRelativesOnly = true });

        Assert.Empty(Node(nodes, ChildId).Person.Parents);
        Assert.DoesNotContain(ChildId, Node(nodes, "alan").Person.Children);
    }

    [Fact]
    public void Projection_terminates_on_data_that_loops()
    {
        var people = new List<PersonData>
        {
            Person("a", "A", "Loop", Gender.Male, 1900, parents: ["b"], children: ["b"]),
            Person("b", "B", "Loop", Gender.Male, 1900, parents: ["a"], children: ["a"]),
        };

        var nodes = TreeProjection.Build(people, null);
        var layout = TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions { FocusId = "a" });

        Assert.Equal(2, nodes.Count);
        Assert.NotEmpty(layout.Cards);
        Assert.Equal(layout.Cards.Count, layout.Cards.Select(card => card.Id).Distinct().Count());
    }

    private static IReadOnlyList<PersonData> Success(Result<IReadOnlyList<PersonData>> result)
    {
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}

public class TreeLayoutParentSetTests
{
    private static TreeCard Card(TreeLayoutResult layout, string id) => layout.Cards.Single(card => card.Id == id);

    [Fact]
    public void A_person_with_two_sets_is_drawn_once_under_the_active_one_with_a_badge_type_and_count()
    {
        var layout = TreeLayoutCalculator.Calculate(BirthAndAdopted(), new TreeLayoutOptions
        {
            FocusId = ParentSetFixtures.ChildId,
            ProgenyDepth = 0,
        });

        var kim = Card(layout, ParentSetFixtures.ChildId);
        Assert.Equal(ParentageType.Adopted, kim.ParentType);
        Assert.Equal(ParentSetFixtures.AdoptedSetId, kim.ParentSetId);
        Assert.Equal(2, kim.ParentSetCount);
        Assert.False(kim.IsStub);
        Assert.Contains(layout.Cards, card => card.Id == "alan");
        Assert.DoesNotContain(layout.Cards, card => card.Id == "bob");
        Assert.Single(layout.Cards, card => card.PersonId == ParentSetFixtures.ChildId);
    }

    [Fact]
    public void Adopted_links_carry_their_type_and_birth_links_stay_solid()
    {
        var adopted = TreeLayoutCalculator.Calculate(BirthAndAdopted(), new TreeLayoutOptions { FocusId = ParentSetFixtures.ChildId, ProgenyDepth = 0 });
        var birth = TreeLayoutCalculator.Calculate(
            BirthAndAdopted(),
            new TreeLayoutOptions
            {
                FocusId = ParentSetFixtures.ChildId,
                ProgenyDepth = 0,
                ActiveParentSets = new Dictionary<string, string> { [ParentSetFixtures.ChildId] = ParentSetFixtures.BirthSetId },
            });

        Assert.Contains(adopted.Links, link => link.Type == ParentageType.Adopted);
        Assert.DoesNotContain(adopted.Links, link => link.Type == ParentageType.Foster);
        Assert.All(birth.Links, link => Assert.Equal(ParentageType.Birth, link.Type));
    }

    [Fact]
    public void Foster_links_are_typed_foster()
    {
        var people = Success(FamilyGraphEditor.SetParentSetType(BirthAndAdopted(), ParentSetFixtures.ChildId, ParentSetFixtures.AdoptedSetId, ParentageType.Foster));

        var layout = TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions { FocusId = ParentSetFixtures.ChildId, ProgenyDepth = 0 });

        Assert.Equal(ParentageType.Foster, Card(layout, ParentSetFixtures.ChildId).ParentType);
        Assert.Contains(layout.Links, link => link.Type == ParentageType.Foster);
    }

    [Fact]
    public void Centered_on_an_adoptive_parent_a_child_whose_active_set_is_birth_is_a_stub()
    {
        var layout = TreeLayoutCalculator.Calculate(
            BirthAndAdopted(),
            new TreeLayoutOptions
            {
                FocusId = "alan",
                AncestryDepth = 1,
                ProgenyDepth = 2,
                ActiveParentSets = new Dictionary<string, string> { [ParentSetFixtures.ChildId] = ParentSetFixtures.BirthSetId },
            });

        var stub = Assert.Single(layout.Cards, card => card.IsStub);
        Assert.Equal(ParentSetFixtures.ChildId, stub.PersonId);
        Assert.Equal(ParentageType.Adopted, stub.ParentType);
        Assert.Equal(ParentSetFixtures.AdoptedSetId, stub.ParentSetId);
        Assert.False(stub.SeeAlso);
        var alan = Card(layout, "alan");
        Assert.True(stub.Y > alan.Y);
        Assert.Contains(layout.Links, link => link.Type == ParentageType.Adopted);
        AssertNoOverlap(layout);
    }

    [Fact]
    public void Centered_on_an_adoptive_parent_the_child_with_an_active_adopted_set_is_a_real_card()
    {
        var layout = TreeLayoutCalculator.Calculate(BirthAndAdopted(), new TreeLayoutOptions { FocusId = "alan", ProgenyDepth = 2 });

        var kim = Card(layout, ParentSetFixtures.ChildId);
        Assert.False(kim.IsStub);
        Assert.Equal(ParentageType.Adopted, kim.ParentType);
        Assert.DoesNotContain(layout.Cards, card => card.IsStub && card.PersonId == ParentSetFixtures.ChildId && card.ParentSetId == ParentSetFixtures.AdoptedSetId);
    }

    [Fact]
    public void A_stub_whose_real_person_is_also_drawn_says_see_also()
    {
        // Cal is Pat's son and is also adopted by his aunt Ann. Centered on Gus, Cal is real under Pat and a stub under Ann.
        var people = Success(FamilyGraphEditor.AddParentSet(Relatives(), "cal", ParentageType.Adopted, ["ann"]));

        var layout = TreeLayoutCalculator.Calculate(
            people,
            new TreeLayoutOptions
            {
                FocusId = "gus",
                AncestryDepth = 0,
                ProgenyDepth = 3,
                ActiveParentSets = new Dictionary<string, string> { ["cal"] = Find(people, "cal").ParentSets.Single(set => set.Type == ParentageType.Birth).Id },
            });

        var stub = Assert.Single(layout.Cards, card => card.IsStub);
        Assert.Equal("cal", stub.PersonId);
        Assert.True(stub.SeeAlso);
        Assert.Single(layout.Cards, card => !card.IsStub && card.PersonId == "cal");
        Assert.Equal(layout.Cards.Count, layout.Cards.Select(card => card.Id).Distinct().Count());
    }

    [Fact]
    public void A_stub_has_no_descendants_drawn_from_it()
    {
        var people = BirthAndAdopted().Concat(
        [
            Person("grandkid", "Grand", "Kid", Gender.Male, 2010, parents: ["kim"]),
        ]).Select(person => person.Id == ChildId ? person with { Children = ["grandkid"] } : person).ToList();

        var layout = TreeLayoutCalculator.Calculate(
            people,
            new TreeLayoutOptions
            {
                FocusId = "alan",
                ProgenyDepth = 3,
                ActiveParentSets = new Dictionary<string, string> { [ChildId] = BirthSetId },
            });

        Assert.Single(layout.Cards, card => card.IsStub);
        Assert.DoesNotContain(layout.Cards, card => card.Id == "grandkid");
    }

    [Fact]
    public void Siblings_are_those_of_the_active_set_only()
    {
        // Kim was born to Bob and Beth, who also have Ben. Alan and Amy adopted Kim and also have Abby.
        var people = BirthAndAdopted().Concat(
        [
            Person("ben", "Ben", "Ross", Gender.Male, 1988, parents: ["bob", "beth"]),
            Person("abby", "Abby", "Cole", Gender.Female, 1990, parents: ["alan", "amy"]),
        ]).Select(person => person.Id switch
        {
            "bob" or "beth" => person with { Children = [ChildId, "ben"] },
            "alan" or "amy" => person with { Children = [ChildId, "abby"] },
            _ => person,
        }).ToList();

        var adoptive = TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions { FocusId = ChildId, ShowSiblings = true, ProgenyDepth = 0 });
        var birth = TreeLayoutCalculator.Calculate(
            people,
            new TreeLayoutOptions
            {
                FocusId = ChildId,
                ShowSiblings = true,
                ProgenyDepth = 0,
                ActiveParentSets = new Dictionary<string, string> { [ChildId] = BirthSetId },
            });

        Assert.Contains(adoptive.Cards, card => card.Id == "abby");
        Assert.DoesNotContain(adoptive.Cards, card => card.Id == "ben");
        Assert.Contains(birth.Cards, card => card.Id == "ben");
        Assert.DoesNotContain(birth.Cards, card => card.Id == "abby");
    }

    [Fact]
    public void A_couple_with_a_birth_child_and_an_adopted_child_gets_a_separate_bus_for_each_type()
    {
        var people = BirthAndAdopted().Concat(
        [
            Person("ada", "Ada", "Cole", Gender.Female, 1980, parents: ["alan", "amy"]),
        ]).Select(person => person.Id is "alan" or "amy" ? person with { Children = [ChildId, "ada"] } : person).ToList();

        var layout = TreeLayoutCalculator.Calculate(people, new TreeLayoutOptions { FocusId = "alan", ProgenyDepth = 2 });

        Assert.Contains(layout.Links, link => link.Type == ParentageType.Birth && !link.Dashed);
        Assert.Contains(layout.Links, link => link.Type == ParentageType.Adopted);
        AssertNoOverlap(layout);
    }

    [Fact]
    public void Layout_of_legacy_data_has_no_stubs_and_only_birth_links()
    {
        var layout = TreeLayoutCalculator.Calculate(SampleFamily.Create(withParentSets: false), new TreeLayoutOptions { FocusId = SampleFamily.FocusId, ShowSiblings = true });

        Assert.DoesNotContain(layout.Cards, card => card.IsStub);
        Assert.All(layout.Links, link => Assert.Equal(ParentageType.Birth, link.Type));
        Assert.All(layout.Cards, card => Assert.Equal(ParentageType.Birth, card.ParentType));
    }

    private static IReadOnlyList<PersonData> Success(Result<IReadOnlyList<PersonData>> result)
    {
        Assert.True(result.IsSuccess, result.IsSuccess ? string.Empty : result.GetDisplayMessage());
        return result.Value;
    }

    private static void AssertNoOverlap(TreeLayoutResult layout)
    {
        foreach (var a in layout.Cards)
        {
            foreach (var b in layout.Cards.Where(other => other.Id != a.Id))
            {
                bool overlapX = a.X < b.X + TreeLayoutCalculator.CardWidth - 1 && b.X < a.X + TreeLayoutCalculator.CardWidth - 1;
                bool overlapY = a.Y < b.Y + TreeLayoutCalculator.CardHeight - 1 && b.Y < a.Y + TreeLayoutCalculator.CardHeight - 1;
                Assert.False(overlapX && overlapY, $"{a.Id} overlaps {b.Id}");
            }
        }
    }
}

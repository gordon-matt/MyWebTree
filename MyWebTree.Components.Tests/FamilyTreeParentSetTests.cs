using Bunit;
using Microsoft.AspNetCore.Components;
using MyWebTree.Components;
using MyWebTree.Graph;
using MyWebTree.Models;

namespace MyWebTree.Components.Tests;

/// <summary>
/// Drives the component the way a host does: it renders with a list of people, collects what the component
/// raises, and passes the new list back after each change.
/// </summary>
public sealed class FamilyTreeParentSetTests : BunitContext
{
    private const string Kim = "kim";
    private const string BirthSet = "kim-birth";
    private const string AdoptedSet = "kim-adopted";

    private readonly List<FamilyTreeChange> changes = [];
    private readonly List<IReadOnlyDictionary<string, string>> activeChanges = [];

    public FamilyTreeParentSetTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<FamilyTree> Draw(
        IReadOnlyList<PersonData> people,
        string focus,
        bool editable = true,
        IReadOnlyDictionary<string, string>? active = null) =>
        Render<FamilyTree>(parameters => parameters
            .Add(tree => tree.People, people)
            .Add(tree => tree.FocusPersonId, focus)
            .Add(tree => tree.Editable, editable)
            .Add(tree => tree.ActiveParentSets, active)
            .Add(tree => tree.Changed, EventCallback.Factory.Create<FamilyTreeChange>(this, change => changes.Add(change)))
            .Add(tree => tree.ActiveParentSetsChanged, EventCallback.Factory.Create<IReadOnlyDictionary<string, string>>(this, sets => activeChanges.Add(sets))));

    /// <summary>What a host does with a change: keep the new people and render again.</summary>
    private static void HostAccepts(IRenderedComponent<FamilyTree> tree, FamilyTreeChange change) =>
        tree.Render(parameters => parameters.Add(component => component.People, change.People));

    private static AngleSharp.Dom.IElement ButtonText(IRenderedComponent<FamilyTree> tree, string text, int index = 0) =>
        tree.FindAll("button").Where(button => button.TextContent.Trim() == text).ElementAt(index);

    private static IEnumerable<string> CardNames(IRenderedComponent<FamilyTree> tree) =>
        tree.FindAll(".mwt-card .mwt-card-name").Select(name => name.TextContent.Trim());

    private static PersonData Person(IReadOnlyList<PersonData> people, string id) => people.Single(person => person.Id == id);

    // ---- drawing -----------------------------------------------------------------------------------------------

    [Fact]
    public void An_adopted_link_has_a_badge_a_dashed_edge_and_a_families_chip()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        Assert.Single(tree.FindAll(".mwt-badge-adopted"));
        Assert.Contains("Adopted", tree.Find(".mwt-badge").TextContent);
        Assert.NotEmpty(tree.FindAll("line.mwt-adopted"));
        Assert.Empty(tree.FindAll("line.mwt-foster"));
        Assert.Contains("2 families", tree.Find(".mwt-families").TextContent);

        // Kim is drawn under her primary set, which is the adoptive one.
        Assert.Contains("Alan Cole", CardNames(tree));
        Assert.DoesNotContain("Bob Ross", CardNames(tree));
    }

    [Fact]
    public void A_foster_link_has_an_F_badge_and_a_dotted_edge()
    {
        var people = BirthAndAdopted();
        var changed = FamilyGraphEditor.SetParentSetType(people, Kim, AdoptedSet, ParentageType.Foster);
        Assert.True(changed.IsSuccess);

        var tree = Draw(changed.Value, Kim);

        Assert.Single(tree.FindAll(".mwt-badge-foster"));
        Assert.Contains("F", tree.Find(".mwt-badge").TextContent);
        Assert.NotEmpty(tree.FindAll("line.mwt-foster"));
    }

    [Fact]
    public void A_legacy_host_that_only_sets_Parents_sees_no_badge_chip_or_styled_edge()
    {
        var tree = Draw(Relatives(), "cal");

        Assert.Empty(tree.FindAll(".mwt-badge"));
        Assert.Empty(tree.FindAll(".mwt-families"));
        Assert.Empty(tree.FindAll("line.mwt-adopted"));
        Assert.Empty(tree.FindAll("line.mwt-foster"));
        Assert.Contains("Gus Hale", CardNames(tree));
    }

    [Fact]
    public void A_host_supplied_active_set_decides_which_parents_are_drawn()
    {
        var tree = Draw(BirthAndAdopted(), Kim, active: new Dictionary<string, string> { [Kim] = BirthSet });

        Assert.Contains("Bob Ross", CardNames(tree));
        Assert.DoesNotContain("Alan Cole", CardNames(tree));
        Assert.Empty(tree.FindAll(".mwt-badge"));
    }

    // ---- the active set is view state ----------------------------------------------------------------------------

    [Fact]
    public void Choosing_another_family_raises_ActiveParentSetsChanged_and_not_Changed()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        tree.Find(".mwt-families").Click();
        var items = tree.FindAll(".mwt-popover-item");
        Assert.Equal(2, items.Count);
        Assert.Contains("Birth", items[0].TextContent);
        Assert.Equal("true", items[1].GetAttribute("aria-checked"));
        items[0].Click();

        Assert.Empty(changes);
        var raised = Assert.Single(activeChanges);
        Assert.Equal(BirthSet, raised[Kim]);
        Assert.Contains("Bob Ross", CardNames(tree));
        Assert.DoesNotContain("Alan Cole", CardNames(tree));
    }

    [Fact]
    public void Choosing_the_primary_family_again_forgets_the_override()
    {
        var tree = Draw(BirthAndAdopted(), Kim, active: new Dictionary<string, string> { [Kim] = BirthSet });

        tree.Find(".mwt-families").Click();
        tree.FindAll(".mwt-popover-item")[1].Click();

        Assert.Empty(changes);
        Assert.Empty(Assert.Single(activeChanges));
        Assert.Contains("Alan Cole", CardNames(tree));
    }

    [Fact]
    public void A_read_only_tree_hides_editing_but_still_switches_the_active_family()
    {
        var tree = Draw(BirthAndAdopted(), Kim, editable: false);

        Assert.Empty(tree.FindAll("select.mwt-set-type"));
        Assert.DoesNotContain(tree.FindAll("button"), button => button.TextContent.Trim() is "Remove" or "Make primary" or "Unlink");

        tree.Find(".mwt-families").Click();
        tree.FindAll(".mwt-popover-item")[0].Click();

        Assert.Empty(changes);
        Assert.Equal(BirthSet, Assert.Single(activeChanges)[Kim]);
    }

    [Fact]
    public void A_stub_switches_to_its_family_from_its_own_button()
    {
        // Centered on Alan, Kim is drawn under the Birth set, so Kim shows under Alan as a stub.
        var people = BirthAndAdopted();
        var tree = Draw(people, "alan", active: new Dictionary<string, string> { [Kim] = BirthSet });

        Assert.Single(tree.FindAll(".mwt-card.mwt-stub"));
        ButtonText(tree, "Switch to this family").Click();

        // The adoptive set is Kim's primary one, so choosing it clears the override, and the stub goes away.
        Assert.Empty(changes);
        Assert.Empty(Assert.Single(activeChanges));
        Assert.Empty(tree.FindAll(".mwt-card.mwt-stub"));
        Assert.Contains("Kim Ross", CardNames(tree));
    }

    // ---- editing sets ----------------------------------------------------------------------------------------------

    [Fact]
    public void Make_primary_raises_ParentSetUpdated_for_the_child()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        // The adoptive set is primary, so the only "Make primary" button belongs to the birth set.
        ButtonText(tree, "Make primary").Click();

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.ParentSetUpdated, change.Kind);
        Assert.Equal(Kim, change.PersonId);
        Assert.Equal(BirthSet, change.ParentSetId);
        Assert.Equal(ParentageType.Birth, change.Type);
        Assert.True(Person(change.People, Kim).ParentSets.Single(set => set.Id == BirthSet).IsPrimary);
        Assert.False(Person(change.People, Kim).ParentSets.Single(set => set.Id == AdoptedSet).IsPrimary);
    }

    [Fact]
    public void Changing_the_type_raises_ParentSetUpdated_with_the_new_type()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        // Sets are listed in the person's own order: birth, then adopted.
        tree.FindAll("select.mwt-set-type")[1].Change(nameof(ParentageType.Foster));

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.ParentSetUpdated, change.Kind);
        Assert.Equal(AdoptedSet, change.ParentSetId);
        Assert.Equal(ParentageType.Foster, change.Type);
        Assert.Equal(ParentageType.Foster, Person(change.People, Kim).ParentSets.Single(set => set.Id == AdoptedSet).Type);
    }

    [Fact]
    public void Changing_the_date_raises_ParentSetUpdated_and_a_bad_date_raises_nothing()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        // A birth set has no date box. The only one belongs to the adopted set.
        Assert.Single(tree.FindAll(".mwt-set-date input"));
        tree.Find(".mwt-set-date input").Change("1990-02-30");
        Assert.Empty(changes);
        Assert.Contains("Use a date like", tree.Find(".mwt-banner").TextContent);

        // The banner re-rendered the panel, so the input is looked up again.
        tree.Find(".mwt-set-date input").Change("1988-04");

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.ParentSetUpdated, change.Kind);
        Assert.Equal(new PartialDate(1988, 4, null), Person(change.People, Kim).ParentSets.Single(set => set.Id == AdoptedSet).Date);
    }

    [Fact]
    public void Removing_a_set_asks_first_and_then_raises_ParentSetRemoved()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        ButtonText(tree, "Remove", 1).Click();
        Assert.Empty(changes);
        Assert.Contains("The parents stay in the tree", tree.Markup);

        // The first "Remove" is now the set's own button, and the confirm button comes after both.
        ButtonText(tree, "Remove", 2).Click();

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.ParentSetRemoved, change.Kind);
        Assert.Equal(Kim, change.PersonId);
        Assert.Equal(AdoptedSet, change.ParentSetId);
        Assert.Equal(ParentageType.Adopted, change.Type);
        var left = Assert.Single(Person(change.People, Kim).ParentSets);
        Assert.Equal(BirthSet, left.Id);
        Assert.True(left.IsPrimary);
        Assert.Contains(change.People, person => person.Id == "alan");
    }

    [Fact]
    public void Unlinking_one_parent_from_a_set_raises_Unlinked_naming_the_set()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        // Birth set: Bob, Beth. Adopted set: Alan, Amy. Alan is the third "Unlink" in the panel.
        ButtonText(tree, "Unlink", 2).Click();
        ButtonText(tree, "Unlink", 3).Click();

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.Unlinked, change.Kind);
        Assert.Equal(Kim, change.PersonId);
        Assert.Equal("alan", change.OtherPersonId);
        Assert.Equal(LinkKind.Parent, change.Link);
        Assert.Equal(AdoptedSet, change.ParentSetId);
        Assert.Equal(ParentageType.Adopted, change.Type);
        Assert.Equal(["amy"], Person(change.People, Kim).ParentSets.Single(set => set.Id == AdoptedSet).ParentIds);
    }

    // ---- adding and linking parents ------------------------------------------------------------------------------

    [Fact]
    public void Adding_a_father_in_a_new_foster_family_raises_Added_and_creates_the_set()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        ButtonText(tree, "Father").Click();

        // Both of Kim's families are full, so the only choice is a new family, and its type.
        var selects = tree.FindAll(".mwt-form select");
        var family = selects.Single(select => select.InnerHtml.Contains("A new family"));
        Assert.Equal("new", family.GetAttribute("value") ?? "new");
        var type = tree.FindAll(".mwt-form select").Single(select => select.InnerHtml.Contains("Foster") && !select.InnerHtml.Contains("A new family"));
        type.Change(nameof(ParentageType.Foster));
        tree.FindAll(".mwt-form label input").First().Change("Ned");
        ButtonText(tree, "Save").Click();

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.Added, change.Kind);
        var ned = change.People.Single(person => person.GivenNames == "Ned");
        var kim = Person(change.People, Kim);
        Assert.Equal(3, kim.ParentSets.Count);
        var added = kim.ParentSets.Single(set => set.ParentIds.Contains(ned.Id));
        Assert.Equal(ParentageType.Foster, added.Type);
        Assert.Equal([ned.Id], added.ParentIds);
        Assert.False(added.IsPrimary);
    }

    [Fact]
    public void Linking_a_parent_into_a_new_family_raises_ParentSetAdded()
    {
        var tree = Draw(Relatives(), "cal");

        ButtonText(tree, "Parent").Click();
        tree.Find("input[type=search]").Input("Ann");
        tree.Find(".mwt-result").Click();

        // Cal has room in Pat's family, so the default is to join it. Ask for a new foster family instead.
        var family = tree.FindAll(".mwt-confirm select").First();
        family.Change("new");
        tree.FindAll(".mwt-confirm select").Last().Change(nameof(ParentageType.Foster));
        ButtonText(tree, "Link").Click();

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.ParentSetAdded, change.Kind);
        Assert.Equal("cal", change.PersonId);
        Assert.Equal("ann", change.OtherPersonId);
        Assert.Equal(ParentageType.Foster, change.Type);
        var cal = Person(change.People, "cal");
        Assert.Equal(2, cal.ParentSets.Count);
        Assert.Equal(change.ParentSetId, cal.ParentSets.Single(set => set.ParentIds.Contains("ann")).Id);
        Assert.Equal(["pat"], cal.Parents);
    }

    [Fact]
    public void Linking_a_parent_into_an_existing_family_raises_Linked_naming_the_set()
    {
        var tree = Draw(Relatives(), "cal");

        ButtonText(tree, "Parent").Click();
        tree.Find("input[type=search]").Input("Ann");
        tree.Find(".mwt-result").Click();
        ButtonText(tree, "Link").Click();

        var change = Assert.Single(changes);
        Assert.Equal(FamilyTreeChangeKind.Linked, change.Kind);
        Assert.Equal("cal", change.PersonId);
        Assert.Equal("ann", change.OtherPersonId);
        Assert.Equal(LinkKind.Parent, change.Link);
        Assert.Equal(ParentageType.Birth, change.Type);
        var cal = Person(change.People, "cal");
        Assert.Single(cal.ParentSets);
        Assert.Equal(cal.ParentSets[0].Id, change.ParentSetId);
        Assert.Equal(["pat", "ann"], cal.ParentSets[0].ParentIds);
    }

    // ---- undo and redo ---------------------------------------------------------------------------------------------

    [Fact]
    public void Undo_and_redo_raise_Snapshot_and_carry_the_parent_sets()
    {
        var tree = Draw(BirthAndAdopted(), Kim);

        ButtonText(tree, "Make primary").Click();
        var edit = Assert.Single(changes);
        HostAccepts(tree, edit);

        tree.Find("button[title=Undo]").Click();
        var undone = changes[^1];
        Assert.Equal(FamilyTreeChangeKind.Snapshot, undone.Kind);
        Assert.Equal(AdoptedSet, Person(undone.People, Kim).ParentSets.Single(set => set.IsPrimary).Id);
        HostAccepts(tree, undone);

        tree.Find("button[title=Redo]").Click();
        var redone = changes[^1];
        Assert.Equal(FamilyTreeChangeKind.Snapshot, redone.Kind);
        Assert.Equal(BirthSet, Person(redone.People, Kim).ParentSets.Single(set => set.IsPrimary).Id);
        Assert.Equal(3, changes.Count);
    }

    // ---- fixtures ----------------------------------------------------------------------------------------------------

    private static List<PersonData> BirthAndAdopted() =>
    [
        Make("bob", "Bob", "Ross", Gender.Male, 1960, spouses: ["beth"], children: [Kim]),
        Make("beth", "Beth", "Ross", Gender.Female, 1962, spouses: ["bob"], children: [Kim]),
        Make("alan", "Alan", "Cole", Gender.Male, 1958, spouses: ["amy"], children: [Kim]),
        Make("amy", "Amy", "Cole", Gender.Female, 1961, spouses: ["alan"], children: [Kim]),
        Make(Kim, "Kim", "Ross", Gender.Female, 1985, parents: ["bob", "beth"]) with
        {
            ParentSets =
            [
                new ParentSetData { Id = BirthSet, Type = ParentageType.Birth, ParentIds = ["bob", "beth"] },
                new ParentSetData { Id = AdoptedSet, Type = ParentageType.Adopted, ParentIds = ["alan", "amy"], Date = new PartialDate(1987) },
            ],
        },
    ];

    private static List<PersonData> Relatives() =>
    [
        Make("gus", "Gus", "Hale", Gender.Male, 1930, children: ["pat", "ann"]),
        Make("pat", "Pat", "Hale", Gender.Male, 1955, parents: ["gus"], children: ["cal"]),
        Make("ann", "Ann", "Hale", Gender.Female, 1958, parents: ["gus"]),
        Make("cal", "Cal", "Hale", Gender.Male, 1985, parents: ["pat"]),
    ];

    private static PersonData Make(
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
            GivenNames = first,
            FamilyName = last,
            Gender = gender,
            BirthDate = new PartialDate(born),
            Parents = parents ?? [],
            Spouses = spouses ?? [],
            Children = children ?? [],
        };
}

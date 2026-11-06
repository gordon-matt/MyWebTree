using static MyWebTree.Tests.ParentSetFixtures;

namespace MyWebTree.Tests;

public class ParentSetRulesTests
{
    private static ParentSetData Set(string id, ParentageType type, bool primary = false, params string[] parents) => new()
    {
        Id = id,
        Type = type,
        IsPrimary = primary,
        ParentIds = parents.Length == 0 ? ["p"] : parents,
    };

    [Fact]
    public void ChoosePrimary_prefers_adopted_then_birth_then_foster()
    {
        var foster = Set("f", ParentageType.Foster);
        var birth = Set("b", ParentageType.Birth);
        var adopted = Set("a", ParentageType.Adopted);

        Assert.Same(adopted, ParentSetRules.ChoosePrimary([foster, birth, adopted]));
        Assert.Same(birth, ParentSetRules.ChoosePrimary([foster, birth]));
        Assert.Same(foster, ParentSetRules.ChoosePrimary([foster]));
    }

    [Fact]
    public void ChoosePrimary_breaks_ties_by_list_order()
    {
        var first = Set("first", ParentageType.Adopted);
        var second = Set("second", ParentageType.Adopted);

        Assert.Same(first, ParentSetRules.ChoosePrimary([first, second]));
        Assert.Same(second, ParentSetRules.ChoosePrimary([second, first]));
    }

    [Fact]
    public void ChoosePrimary_lets_an_explicit_flag_beat_the_defaults()
    {
        var adopted = Set("a", ParentageType.Adopted);
        var birth = Set("b", ParentageType.Birth, primary: true);

        Assert.Same(birth, ParentSetRules.ChoosePrimary([adopted, birth]));
    }

    [Fact]
    public void ChoosePrimary_returns_null_for_no_sets()
    {
        Assert.Null(ParentSetRules.ChoosePrimary([]));
    }

    [Fact]
    public void Normalize_reads_a_legacy_parents_list_as_one_primary_birth_set()
    {
        var legacy = Person("kid", "Kid", "Test", Gender.Male, 2000, parents: ["mum", "dad"]);

        var normalized = ParentSetRules.NormalizePerson(legacy);

        var set = Assert.Single(normalized.ParentSets);
        Assert.Equal(ParentageType.Birth, set.Type);
        Assert.True(set.IsPrimary);
        Assert.Equal(["mum", "dad"], set.ParentIds);
        Assert.Equal(["mum", "dad"], normalized.Parents);
    }

    [Fact]
    public void Normalize_returns_a_person_with_no_parents_unchanged()
    {
        var alone = Person("solo", "Solo", "Test", Gender.Male, 1990);

        Assert.Same(alone, ParentSetRules.NormalizePerson(alone));
    }

    [Fact]
    public void Normalize_makes_parents_equal_the_primary_set_and_flags_exactly_one()
    {
        var people = BirthAndAdopted();

        var kim = Find(ParentSetRules.Normalize(people), ChildId);

        Assert.Equal(["alan", "amy"], kim.Parents);
        var primary = Assert.Single(kim.ParentSets, set => set.IsPrimary);
        Assert.Equal(AdoptedSetId, primary.Id);
    }

    [Fact]
    public void Normalize_honours_an_explicit_primary_over_the_default_order()
    {
        var kim = Find(ParentSetRules.Normalize(BirthAndAdopted(flagBirthPrimary: true)), ChildId);

        Assert.Equal(["bob", "beth"], kim.Parents);
        Assert.Equal(BirthSetId, Assert.Single(kim.ParentSets, set => set.IsPrimary).Id);
    }

    [Fact]
    public void Normalize_is_stable_when_applied_twice()
    {
        var once = ParentSetRules.Normalize(BirthAndAdopted());
        var twice = ParentSetRules.Normalize(once);

        Assert.Same(once, twice);
    }

    [Fact]
    public void ActiveOf_uses_the_remembered_set_when_it_exists_and_the_primary_otherwise()
    {
        var kim = Find(BirthAndAdopted(), ChildId);

        Assert.Equal(BirthSetId, ParentSetRules.ActiveOf(kim, new Dictionary<string, string> { [ChildId] = BirthSetId })!.Id);
        Assert.Equal(AdoptedSetId, ParentSetRules.ActiveOf(kim, new Dictionary<string, string> { [ChildId] = "gone" })!.Id);
        Assert.Equal(AdoptedSetId, ParentSetRules.ActiveOf(kim, null)!.Id);
    }
}

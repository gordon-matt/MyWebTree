using static MyWebTree.Tests.ParentSetFixtures;

namespace MyWebTree.Tests;

public class ParentSetEditorTests
{
    private static IReadOnlyList<PersonData> Success(Result<IReadOnlyList<PersonData>> result)
    {
        Assert.True(result.IsSuccess, result.IsSuccess ? string.Empty : result.GetDisplayMessage());
        return result.Value;
    }

    private static PersonData Alone(string id, string first, Gender gender, int born) =>
        Person(id, first, "Test", gender, born);

    // ---- AddParentSet ----

    [Fact]
    public void AddParentSet_adds_an_adopted_set_and_lists_the_child_on_both_parents()
    {
        var people = BirthAndAdopted().Where(person => person.Id != ChildId).Append(
            Person(ChildId, "Kim", "Ross", Gender.Female, 1985, parents: ["bob", "beth"]))
            .Select(person => person.Id is "alan" or "amy" ? person with { Children = [] } : person)
            .ToList();

        var result = Success(FamilyGraphEditor.AddParentSet(people, ChildId, ParentageType.Adopted, ["alan", "amy"], new PartialDate(1987)));

        var kim = Find(result, ChildId);
        Assert.Equal(2, kim.ParentSets.Count);
        var adopted = Assert.Single(kim.ParentSets, set => set.Type == ParentageType.Adopted);
        Assert.Equal(new PartialDate(1987), adopted.Date);
        Assert.False(adopted.IsPrimary);
        Assert.Equal(["bob", "beth"], kim.Parents);
        Assert.Contains(ChildId, Find(result, "alan").Children);
        Assert.Contains(ChildId, Find(result, "amy").Children);
        Assert.Contains("amy", Find(result, "alan").Spouses);
    }

    [Fact]
    public void AddParentSet_makes_the_first_set_primary()
    {
        var people = new List<PersonData>
        {
            Alone("mum", "Mum", Gender.Female, 1960),
            Alone("kid", "Kid", Gender.Male, 1990),
        };

        var result = Success(FamilyGraphEditor.AddParentSet(people, "kid", ParentageType.Foster, ["mum"]));

        var kid = Find(result, "kid");
        Assert.True(Assert.Single(kid.ParentSets).IsPrimary);
        Assert.Equal(["mum"], kid.Parents);
    }

    [Fact]
    public void AddParentSet_rejects_a_second_birth_set()
    {
        var result = FamilyGraphEditor.AddParentSet(BirthAndAdopted(), ChildId, ParentageType.Birth, ["alan"]);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("birth parents", result.GetDisplayMessage());
    }

    [Fact]
    public void AddParentSet_rejects_a_duplicate_set_of_the_same_type()
    {
        var result = FamilyGraphEditor.AddParentSet(BirthAndAdopted(), ChildId, ParentageType.Birth, ["beth", "bob"]);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public void AddParentSet_allows_the_same_people_as_a_different_type()
    {
        var result = FamilyGraphEditor.AddParentSet(BirthAndAdopted(), ChildId, ParentageType.Foster, ["bob", "beth"]);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void AddParentSet_rejects_a_set_with_no_parents_or_more_than_two(int count)
    {
        var people = BirthAndAdopted();
        string[] ids = ["alan", "amy", "bob"];

        var result = FamilyGraphEditor.AddParentSet(people, ChildId, ParentageType.Foster, ids.Take(count).ToList());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void AddParentSet_rejects_a_child_born_before_a_parent_in_any_set()
    {
        var people = BirthAndAdopted().Append(Alone("late", "Late", Gender.Male, 2001)).ToList();

        var result = FamilyGraphEditor.AddParentSet(people, ChildId, ParentageType.Foster, ["late"]);

        Assert.False(result.IsSuccess);
        Assert.Contains("born before", result.GetDisplayMessage());
    }

    // ---- ancestry across sets ----

    [Fact]
    public void AddParentSet_rejects_a_cycle_through_a_non_primary_set()
    {
        // Cal is Gus's grandchild. Making Cal a parent of Gus, in any set, would make Gus his own ancestor.
        var people = Relatives().Select(person => person.Id == "gus" ? person with { BirthDate = null } : person).ToList();

        var result = FamilyGraphEditor.AddParentSet(people, "gus", ParentageType.Adopted, ["cal"]);

        Assert.False(result.IsSuccess);
        Assert.Contains("own ancestor", result.GetDisplayMessage());
    }

    [Fact]
    public void AddParentSet_accepts_a_niece_adopted_by_her_aunt()
    {
        var result = FamilyGraphEditor.AddParentSet(Relatives(), "cal", ParentageType.Adopted, ["ann"]);

        var cal = Find(Success(result), "cal");
        Assert.Equal(2, cal.ParentSets.Count);
        Assert.Contains("cal", Find(result.Value, "ann").Children);
    }

    [Fact]
    public void Validate_rejects_a_loop_that_only_exists_through_a_non_primary_set()
    {
        var people = Relatives().Select(person => person.Id == "gus"
            ? person with
            {
                BirthDate = null,
                Parents = ["cal"],
                ParentSets = [new ParentSetData { Id = "g1", ParentIds = ["cal"], Type = ParentageType.Adopted }],
            }
            : person.Id == "cal" ? person with { Children = ["gus"] } : person).ToList();

        var result = FamilyGraphEditor.Validate(people);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Validate_rejects_a_spouse_who_is_a_descendant_through_an_adopted_set()
    {
        var people = Relatives().Select(person => person.Id switch
        {
            "ann" => person with { Spouses = ["cal"], Children = [] },
            "cal" => person with { Spouses = ["ann"], Children = [] },
            _ => person,
        }).ToList();

        var adopted = FamilyGraphEditor.AddParentSet(people, "cal", ParentageType.Adopted, ["ann"]);

        Assert.False(adopted.IsSuccess);
    }

    // ---- dates ----

    [Fact]
    public void Validate_applies_the_birth_date_rule_to_every_set()
    {
        var people = BirthAndAdopted().Select(person => person.Id == "alan" ? person with { BirthDate = new PartialDate(2000) } : person).ToList();

        var result = FamilyGraphEditor.Validate(people);

        Assert.False(result.IsSuccess);
        Assert.Contains("Kim Ross was born before Alan Cole", result.GetDisplayMessage());
    }

    [Fact]
    public void SetParentSetDate_rejects_a_date_before_the_childs_birth_and_accepts_a_later_one()
    {
        var early = FamilyGraphEditor.SetParentSetDate(BirthAndAdopted(), ChildId, AdoptedSetId, new PartialDate(1970));
        var later = FamilyGraphEditor.SetParentSetDate(BirthAndAdopted(), ChildId, AdoptedSetId, new PartialDate(1990, 5));
        var cleared = FamilyGraphEditor.SetParentSetDate(BirthAndAdopted(), ChildId, AdoptedSetId, null);

        Assert.False(early.IsSuccess);
        Assert.Equal(new PartialDate(1990, 5), Find(Success(later), ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).Date);
        Assert.Null(Find(Success(cleared), ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).Date);
    }

    // ---- type, primary, remove ----

    [Fact]
    public void SetParentSetType_changes_the_type()
    {
        var result = Success(FamilyGraphEditor.SetParentSetType(BirthAndAdopted(), ChildId, AdoptedSetId, ParentageType.Foster));

        Assert.Equal(ParentageType.Foster, Find(result, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).Type);
    }

    [Fact]
    public void SetParentSetType_rejects_a_second_birth_set_and_clears_a_date_when_a_set_becomes_birth()
    {
        var secondBirth = FamilyGraphEditor.SetParentSetType(BirthAndAdopted(), ChildId, AdoptedSetId, ParentageType.Birth);
        Assert.Equal(ResultStatus.Conflict, secondBirth.Status);
        Assert.Contains("birth parents", secondBirth.GetDisplayMessage());

        // Once the birth set is no longer birth, the adopted set can take its place, and its own date is cleared.
        var people = Success(FamilyGraphEditor.SetParentSetType(BirthAndAdopted(), ChildId, BirthSetId, ParentageType.Foster));
        var dated = Success(FamilyGraphEditor.SetParentSetDate(people, ChildId, AdoptedSetId, new PartialDate(1990)));
        var moved = Success(FamilyGraphEditor.SetParentSetType(dated, ChildId, AdoptedSetId, ParentageType.Birth));
        Assert.Null(Find(moved, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).Date);
        Assert.Equal(ParentageType.Birth, Find(moved, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).Type);
    }

    [Fact]
    public void SetParentSetType_rejects_making_two_identical_sets()
    {
        var people = Success(FamilyGraphEditor.AddParentSet(BirthAndAdopted(), ChildId, ParentageType.Foster, ["alan", "amy"]));
        string fosterId = Find(people, ChildId).ParentSets.Single(set => set.Type == ParentageType.Foster).Id;

        var result = FamilyGraphEditor.SetParentSetType(people, ChildId, fosterId, ParentageType.Adopted);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public void SetPrimaryParentSet_moves_the_flag_and_the_parents_list()
    {
        var result = Success(FamilyGraphEditor.SetPrimaryParentSet(BirthAndAdopted(), ChildId, BirthSetId));

        var kim = Find(result, ChildId);
        Assert.Equal(["bob", "beth"], kim.Parents);
        Assert.Equal(BirthSetId, Assert.Single(kim.ParentSets, set => set.IsPrimary).Id);
    }

    [Fact]
    public void RemoveParentSet_removes_the_set_and_promotes_the_next_one_by_default_order()
    {
        var people = Success(FamilyGraphEditor.SetPrimaryParentSet(BirthAndAdopted(), ChildId, AdoptedSetId));
        people = Success(FamilyGraphEditor.AddParentSet(people, ChildId, ParentageType.Foster, ["bob"]));

        var result = Success(FamilyGraphEditor.RemoveParentSet(people, ChildId, AdoptedSetId));

        var kim = Find(result, ChildId);
        Assert.Equal(2, kim.ParentSets.Count);
        Assert.Equal(ParentageType.Birth, Assert.Single(kim.ParentSets, set => set.IsPrimary).Type);
        Assert.Equal(["bob", "beth"], kim.Parents);
        Assert.DoesNotContain(ChildId, Find(result, "alan").Children);
        Assert.DoesNotContain(ChildId, Find(result, "amy").Children);
        Assert.Contains(ChildId, Find(result, "bob").Children);
    }

    [Fact]
    public void RemoveParentSet_keeps_a_parent_listed_when_they_are_in_another_set()
    {
        var people = Success(FamilyGraphEditor.AddParentSet(BirthAndAdopted(), ChildId, ParentageType.Foster, ["bob"]));
        string fosterId = Find(people, ChildId).ParentSets.Single(set => set.Type == ParentageType.Foster).Id;

        var result = Success(FamilyGraphEditor.RemoveParentSet(people, ChildId, fosterId));

        Assert.Contains(ChildId, Find(result, "bob").Children);
    }

    [Fact]
    public void RemoveParentSet_rejects_an_unknown_set()
    {
        var result = FamilyGraphEditor.RemoveParentSet(BirthAndAdopted(), ChildId, "nope");

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    // ---- unlink / remove person ----

    [Fact]
    public void Unlink_with_a_set_removes_one_member_and_leaves_a_single_parent_set()
    {
        var result = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), ChildId, "amy", AdoptedSetId));

        var adopted = Find(result, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId);
        Assert.Equal(["alan"], adopted.ParentIds);
        Assert.DoesNotContain(ChildId, Find(result, "amy").Children);
        Assert.Contains(ChildId, Find(result, "alan").Children);
    }

    [Fact]
    public void Unlink_the_last_member_deletes_the_empty_set_and_promotes_another()
    {
        var people = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), ChildId, "amy", AdoptedSetId));

        var result = Success(FamilyGraphEditor.Unlink(people, ChildId, "alan", AdoptedSetId));

        var kim = Find(result, ChildId);
        var remaining = Assert.Single(kim.ParentSets);
        Assert.Equal(BirthSetId, remaining.Id);
        Assert.True(remaining.IsPrimary);
        Assert.Equal(["bob", "beth"], kim.Parents);
    }

    [Fact]
    public void Unlink_from_the_parents_side_works_too()
    {
        var result = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), "alan", ChildId, AdoptedSetId));

        Assert.Equal(["amy"], Find(result, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).ParentIds);
    }

    [Fact]
    public void Unlink_without_a_set_is_refused_when_the_parent_is_in_two_sets()
    {
        var people = Success(FamilyGraphEditor.AddParentSet(BirthAndAdopted(), ChildId, ParentageType.Foster, ["bob"]));

        var ambiguous = FamilyGraphEditor.Unlink(people, ChildId, "bob");
        var named = FamilyGraphEditor.Unlink(people, ChildId, "bob", Find(people, ChildId).ParentSets.Single(set => set.Type == ParentageType.Foster).Id);

        Assert.Equal(ResultStatus.Invalid, ambiguous.Status);
        Assert.True(named.IsSuccess);
        Assert.Contains(ChildId, Find(named.Value, "bob").Children);
    }

    [Fact]
    public void Unlink_without_a_set_works_when_there_is_only_one_candidate()
    {
        var result = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), ChildId, "alan"));

        Assert.Equal(["amy"], Find(result, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId).ParentIds);
    }

    [Fact]
    public void Remove_takes_the_person_out_of_every_set_and_drops_sets_left_empty()
    {
        var people = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), ChildId, "amy", AdoptedSetId));

        var result = Success(FamilyGraphEditor.Remove(people, "alan"));

        var kim = Find(result, ChildId);
        var remaining = Assert.Single(kim.ParentSets);
        Assert.Equal(BirthSetId, remaining.Id);
        Assert.Equal(["bob", "beth"], kim.Parents);
    }

    [Fact]
    public void Remove_leaves_a_set_with_one_member_valid()
    {
        var result = Success(FamilyGraphEditor.Remove(BirthAndAdopted(), "amy"));

        var adopted = Find(result, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId);
        Assert.Equal(["alan"], adopted.ParentIds);
        Assert.True(FamilyGraphEditor.Validate(result).IsSuccess);
    }

    // ---- Add father / mother / Link parent ----

    [Fact]
    public void AddRelative_father_creates_a_primary_birth_set_when_the_person_has_none()
    {
        var people = new List<PersonData> { Alone("kid", "Kid", Gender.Male, 1990) };

        var result = Success(FamilyGraphEditor.AddRelative(people, new AddRelativeRequest
        {
            RelatesToPersonId = "kid",
            Kind = RelativeKind.Father,
            Person = new PersonWriteModel { GivenNames = "Dad", FamilyName = "Test" },
        }));

        var set = Assert.Single(Find(result, "kid").ParentSets);
        Assert.Equal(ParentageType.Birth, set.Type);
        Assert.True(set.IsPrimary);
    }

    [Fact]
    public void AddRelative_father_can_start_a_new_adopted_set()
    {
        var result = Success(FamilyGraphEditor.AddRelative(BirthAndAdopted(), new AddRelativeRequest
        {
            RelatesToPersonId = ChildId,
            Kind = RelativeKind.Father,
            Person = new PersonWriteModel { GivenNames = "Foster", FamilyName = "Dad", BirthDate = new PartialDate(1955) },
            ParentSet = new ParentSetTarget { CreateNew = true, Type = ParentageType.Foster },
        }));

        var kim = Find(result, ChildId);
        Assert.Equal(3, kim.ParentSets.Count);
        var foster = Assert.Single(kim.ParentSets, set => set.Type == ParentageType.Foster);
        Assert.False(foster.IsPrimary);
        Assert.Single(foster.ParentIds);
        Assert.Equal(["alan", "amy"], kim.Parents);
    }

    [Fact]
    public void AddRelative_mother_joins_the_named_set_and_becomes_a_spouse_of_the_father_there()
    {
        var people = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), ChildId, "amy", AdoptedSetId));
        people = Success(FamilyGraphEditor.Unlink(people, "alan", "amy"));

        var result = Success(FamilyGraphEditor.AddRelative(people, new AddRelativeRequest
        {
            RelatesToPersonId = ChildId,
            Kind = RelativeKind.Mother,
            Person = new PersonWriteModel { GivenNames = "New", FamilyName = "Mum" },
            ParentSet = new ParentSetTarget { ParentSetId = AdoptedSetId },
        }));

        var adopted = Find(result, ChildId).ParentSets.Single(set => set.Id == AdoptedSetId);
        Assert.Equal(2, adopted.ParentIds.Count);
        string motherId = adopted.ParentIds.Single(id => id != "alan");
        Assert.Contains("alan", Find(result, motherId).Spouses);
        Assert.Equal(2, Find(result, ChildId).ParentSets.Count);
    }

    [Fact]
    public void AddRelative_father_is_refused_by_a_full_set_and_a_set_with_a_father()
    {
        var full = FamilyGraphEditor.AddRelative(BirthAndAdopted(), new AddRelativeRequest
        {
            RelatesToPersonId = ChildId,
            Kind = RelativeKind.Father,
            Person = new PersonWriteModel { GivenNames = "Extra", FamilyName = "Dad" },
            ParentSet = new ParentSetTarget { ParentSetId = BirthSetId },
        });
        var people = Success(FamilyGraphEditor.Unlink(BirthAndAdopted(), ChildId, "amy", AdoptedSetId));
        var hasFather = FamilyGraphEditor.AddRelative(people, new AddRelativeRequest
        {
            RelatesToPersonId = ChildId,
            Kind = RelativeKind.Father,
            Person = new PersonWriteModel { GivenNames = "Extra", FamilyName = "Dad" },
            ParentSet = new ParentSetTarget { ParentSetId = AdoptedSetId },
        });

        Assert.Contains("already has two parents", full.GetDisplayMessage());
        Assert.Contains("already has a father", hasFather.GetDisplayMessage());
    }

    [Fact]
    public void Link_parent_targets_the_given_set_or_a_new_one()
    {
        var people = new List<PersonData>
        {
            Alone("kid", "Kid", Gender.Male, 1990),
            Alone("mum", "Mum", Gender.Female, 1960),
            Alone("aunt", "Aunt", Gender.Female, 1962),
        };
        people = [.. Success(FamilyGraphEditor.Link(people, "kid", "mum", LinkKind.Parent))];

        var adopted = Success(FamilyGraphEditor.Link(people, "kid", "aunt", LinkKind.Parent, new ParentSetTarget { CreateNew = true, Type = ParentageType.Adopted }));
        var sameSet = Success(FamilyGraphEditor.Link(people, "kid", "aunt", LinkKind.Parent));

        Assert.Equal(2, Find(adopted, "kid").ParentSets.Count);
        Assert.Equal(["mum"], Find(adopted, "kid").Parents);
        Assert.Single(Find(sameSet, "kid").ParentSets);
        Assert.Equal(["mum", "aunt"], Find(sameSet, "kid").Parents);
    }

    [Fact]
    public void Link_child_uses_the_childs_set()
    {
        var people = new List<PersonData>
        {
            Alone("kid", "Kid", Gender.Male, 1990),
            Alone("mum", "Mum", Gender.Female, 1960),
            Alone("aunt", "Aunt", Gender.Female, 1962),
        };
        people = [.. Success(FamilyGraphEditor.Link(people, "kid", "mum", LinkKind.Parent))];

        var result = Success(FamilyGraphEditor.Link(people, "aunt", "kid", LinkKind.Child, new ParentSetTarget { CreateNew = true, Type = ParentageType.Foster }));

        Assert.Contains(Find(result, "kid").ParentSets, set => set.Type == ParentageType.Foster && set.ParentIds.SequenceEqual(["aunt"]));
    }

    [Fact]
    public void Link_parent_into_a_set_that_already_has_that_person_conflicts()
    {
        var result = FamilyGraphEditor.Link(BirthAndAdopted(), ChildId, "alan", LinkKind.Parent, new ParentSetTarget { ParentSetId = AdoptedSetId });

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    // ---- legacy compatibility ----

    [Fact]
    public void Legacy_people_with_only_parents_keep_working_and_come_out_normalized()
    {
        var people = SampleFamily.Create();

        var result = Success(FamilyGraphEditor.Update(people, "david", new PersonWriteModel { GivenNames = "David", FamilyName = "Ward", Gender = Gender.Male }));

        var david = Find(result, "david");
        var set = Assert.Single(david.ParentSets);
        Assert.Equal(ParentageType.Birth, set.Type);
        Assert.True(set.IsPrimary);
        Assert.Equal(david.Parents, set.ParentIds);
        Assert.Equal(Find(people, "david").Parents, david.Parents);
    }

    [Fact]
    public void Validate_flags_a_third_parent_in_one_set()
    {
        var people = BirthAndAdopted().Select(person => person.Id == ChildId
            ? person with { ParentSets = [new ParentSetData { Id = "x", ParentIds = ["bob", "beth", "alan"] }] }
            : person).ToList();

        var result = FamilyGraphEditor.Validate(people);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Validate_flags_the_same_set_twice()
    {
        var people = BirthAndAdopted().Select(person => person.Id == ChildId
            ? person with
            {
                ParentSets =
                [
                    new ParentSetData { Id = "a", ParentIds = ["bob", "beth"] },
                    new ParentSetData { Id = "b", ParentIds = ["beth", "bob"] },
                ],
            }
            : person).ToList();

        Assert.False(FamilyGraphEditor.Validate(people).IsSuccess);
    }
}

namespace MyWebTree.Tests;

public class FamilyGraphEditorTests
{
    [Fact]
    public void AddFather_links_both_parents_when_a_mother_exists()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "helen", Gender = Gender.Female, FirstName = "Helen", LastName = "Ward", Children = ["david"] },
            new() { Id = "david", Gender = Gender.Male, FirstName = "David", LastName = "Ward", Parents = ["helen"] },
        ];

        var added = FamilyGraphEditor.AddRelative(people, new AddRelativeRequest
        {
            RelatesToPersonId = "david",
            Kind = RelativeKind.Father,
            Person = new PersonWriteModel { FirstName = "Arthur", LastName = "Ward", Gender = Gender.Female },
        });

        Assert.True(added.IsSuccess);
        var father = added.Value.Single(person => person.FirstName == "Arthur" && person.Id != "arthur");
        var child = added.Value.Single(person => person.Id == "david");
        var mother = added.Value.Single(person => person.Id == "helen");
        Assert.Equal(Gender.Male, father.Gender);
        Assert.Contains(father.Id, child.Parents);
        Assert.Contains(child.Id, father.Children);
        Assert.Contains(mother.Id, father.Spouses);
        Assert.Contains(father.Id, mother.Spouses);
    }

    [Fact]
    public void AddFather_rejects_a_third_parent()
    {
        var result = FamilyGraphEditor.AddRelative(SampleFamily.Create(), new AddRelativeRequest
        {
            RelatesToPersonId = "david",
            Kind = RelativeKind.Father,
            Person = new PersonWriteModel { FirstName = "Someone", LastName = "Else" },
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("already has", result.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Remove_drops_the_person_from_every_list()
    {
        var removed = FamilyGraphEditor.Remove(SampleFamily.Create(), "david");

        Assert.True(removed.IsSuccess);
        Assert.DoesNotContain(removed.Value, person => person.Id == "david");
        Assert.All(removed.Value, person =>
        {
            Assert.DoesNotContain("david", person.Parents);
            Assert.DoesNotContain("david", person.Spouses);
            Assert.DoesNotContain("david", person.Children);
        });
    }

    [Fact]
    public void Update_rejects_a_death_before_the_birth()
    {
        var result = FamilyGraphEditor.Update(SampleFamily.Create(), "david", new PersonWriteModel
        {
            FirstName = "David",
            LastName = "Ward",
            Gender = Gender.Male,
            BirthDate = new PartialDate(1953, 1, 1),
            DeathDate = new PartialDate(1950, 1, 1),
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void Update_keeps_a_year_only_death_in_the_same_year_as_a_full_birth()
    {
        var result = FamilyGraphEditor.Update(SampleFamily.Create(), "david", new PersonWriteModel
        {
            FirstName = "David",
            LastName = "Ward",
            Gender = Gender.Male,
            BirthDate = new PartialDate(1953, 6, 1),
            DeathDate = new PartialDate(1953),
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Unlink_removes_a_spouse_and_leaves_both_people()
    {
        var people = SampleFamily.Create();
        var unlinked = FamilyGraphEditor.Unlink(people, "david", "priya");

        Assert.True(unlinked.IsSuccess);
        var david = unlinked.Value.Single(person => person.Id == "david");
        var priya = unlinked.Value.Single(person => person.Id == "priya");
        Assert.DoesNotContain("priya", david.Spouses);
        Assert.DoesNotContain("david", priya.Spouses);
        Assert.Contains(unlinked.Value, person => person.Id == "david");
        Assert.Contains(unlinked.Value, person => person.Id == "priya");
    }

    [Fact]
    public void Link_child_connects_both_ways_without_assigning_the_spouse()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", Gender = Gender.Female, FirstName = "Ada", Spouses = ["ben"] },
            new() { Id = "ben", Gender = Gender.Male, FirstName = "Ben", Spouses = ["ada"] },
            new() { Id = "cora", Gender = Gender.Female, FirstName = "Cora" },
        ];

        var linked = FamilyGraphEditor.Link(people, "ada", "cora", LinkKind.Child);

        Assert.True(linked.IsSuccess);
        var ada = linked.Value.Single(person => person.Id == "ada");
        var ben = linked.Value.Single(person => person.Id == "ben");
        var cora = linked.Value.Single(person => person.Id == "cora");
        Assert.Contains("cora", ada.Children);
        Assert.Equal(["ada"], cora.Parents);
        Assert.DoesNotContain("cora", ben.Children);
    }

    [Fact]
    public void Link_second_parent_also_records_the_parents_as_spouses()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "cora", Gender = Gender.Female, FirstName = "Cora", Parents = ["ada"] },
            new() { Id = "ada", Gender = Gender.Female, FirstName = "Ada", Children = ["cora"] },
            new() { Id = "ben", Gender = Gender.Male, FirstName = "Ben" },
        ];

        var linked = FamilyGraphEditor.Link(people, "cora", "ben", LinkKind.Parent);

        Assert.True(linked.IsSuccess);
        var cora = linked.Value.Single(person => person.Id == "cora");
        var ada = linked.Value.Single(person => person.Id == "ada");
        var ben = linked.Value.Single(person => person.Id == "ben");
        Assert.Contains("ben", cora.Parents);
        Assert.Contains("cora", ben.Children);
        Assert.Contains("ben", ada.Spouses);
        Assert.Contains("ada", ben.Spouses);
    }

    [Fact]
    public void Link_rejects_a_child_born_before_the_parent()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", FirstName = "Ada", BirthDate = new PartialDate(1990) },
            new() { Id = "cora", FirstName = "Cora", BirthDate = new PartialDate(1980) },
        ];

        var linked = FamilyGraphEditor.Link(people, "ada", "cora", LinkKind.Child);

        Assert.False(linked.IsSuccess);
        Assert.Contains("born before", linked.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Link_allows_a_child_born_in_the_same_year_as_a_year_only_parent()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", FirstName = "Ada", BirthDate = new PartialDate(1980) },
            new() { Id = "cora", FirstName = "Cora", BirthDate = new PartialDate(1980, 6, 1) },
        ];

        var linked = FamilyGraphEditor.Link(people, "ada", "cora", LinkKind.Child);

        Assert.True(linked.IsSuccess);
    }

    [Fact]
    public void Link_rejects_a_third_parent()
    {
        var linked = FamilyGraphEditor.Link(SampleFamily.Create(), "david", "margaret", LinkKind.Parent);

        Assert.False(linked.IsSuccess);
        Assert.Contains("two parents", linked.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Link_allows_two_fathers()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "maya", Gender = Gender.Female, FirstName = "Maya", Parents = ["simon"] },
            new() { Id = "simon", Gender = Gender.Male, FirstName = "Simon", Children = ["maya"] },
            new() { Id = "noah", Gender = Gender.Male, FirstName = "Noah" },
        ];

        var linked = FamilyGraphEditor.Link(people, "maya", "noah", LinkKind.Parent);

        Assert.True(linked.IsSuccess);
        Assert.Contains("noah", linked.Value.Single(person => person.Id == "maya").Parents);
        Assert.Contains("simon", linked.Value.Single(person => person.Id == "noah").Spouses);
    }

    [Fact]
    public void Link_rejects_linking_a_person_to_themself()
    {
        var linked = FamilyGraphEditor.Link(SampleFamily.Create(), "david", "david", LinkKind.Spouse);

        Assert.False(linked.IsSuccess);
        Assert.Contains("themself", linked.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Link_spouse_is_recorded_both_ways()
    {
        var linked = FamilyGraphEditor.Link(SampleFamily.Create(), "ellen", "noah", LinkKind.Spouse);

        Assert.True(linked.IsSuccess);
        var ellen = linked.Value.Single(person => person.Id == "ellen");
        var noah = linked.Value.Single(person => person.Id == "noah");
        Assert.Contains("noah", ellen.Spouses);
        Assert.Contains("ellen", noah.Spouses);
    }

    [Fact]
    public void Link_rejects_spouses_who_could_not_have_met()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", FirstName = "Ada", DeathDate = new PartialDate(1900) },
            new() { Id = "ben", FirstName = "Ben", BirthDate = new PartialDate(1950) },
        ];

        var linked = FamilyGraphEditor.Link(people, "ada", "ben", LinkKind.Spouse);

        Assert.False(linked.IsSuccess);
        Assert.Contains("died before", linked.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Link_rejects_a_cycle()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", FirstName = "Ada", Children = ["ben"] },
            new() { Id = "ben", FirstName = "Ben", Parents = ["ada"] },
        ];

        var linked = FamilyGraphEditor.Link(people, "ada", "ben", LinkKind.Parent);

        Assert.False(linked.IsSuccess);
        Assert.Contains("ancestor", linked.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Link_rejects_marrying_a_descendant()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", FirstName = "Ada", Children = ["ben"] },
            new() { Id = "ben", FirstName = "Ben", Parents = ["ada"], Children = ["cora"] },
            new() { Id = "cora", FirstName = "Cora", Parents = ["ben"] },
        ];

        var linked = FamilyGraphEditor.Link(people, "ada", "cora", LinkKind.Spouse);

        Assert.False(linked.IsSuccess);
        Assert.Contains("descended", linked.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Link_rejects_a_parent_link_that_already_exists()
    {
        var linked = FamilyGraphEditor.Link(SampleFamily.Create(), "arthur", "david", LinkKind.Child);

        Assert.Equal(ResultStatus.Conflict, linked.Status);
        Assert.Contains("already", string.Join(' ', linked.Errors), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddChild_rejects_a_birth_before_the_parent()
    {
        IReadOnlyList<PersonData> people =
        [
            new() { Id = "ada", FirstName = "Ada", BirthDate = new PartialDate(1950) },
        ];

        var added = FamilyGraphEditor.AddRelative(people, new AddRelativeRequest
        {
            RelatesToPersonId = "ada",
            Kind = RelativeKind.Child,
            Person = new PersonWriteModel { FirstName = "Cora", BirthDate = new PartialDate(1900) },
        });

        Assert.False(added.IsSuccess);
        Assert.Contains("born before", added.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Update_rejects_a_birth_before_a_parents_birth()
    {
        var result = FamilyGraphEditor.Update(SampleFamily.Create(), "david", new PersonWriteModel
        {
            FirstName = "David",
            LastName = "Ward",
            Gender = Gender.Male,
            BirthDate = new PartialDate(1920),
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("born before", result.ValidationErrors.Single().ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}

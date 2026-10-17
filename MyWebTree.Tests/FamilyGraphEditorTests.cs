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
            BirthDate = new DateOnly(1953, 1, 1),
            DeathDate = new DateOnly(1950, 1, 1),
        });

        Assert.False(result.IsSuccess);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MyWebTree.Services;
using static MyWebTree.Tests.ParentSetFixtures;

namespace MyWebTree.Tests;

public class ParentSetPersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<FamilyTreeDetail> SaveAsync(TestDatabase database, List<PersonData> people)
    {
        var created = await database.Trees.CreateAsync("Ross and Cole", null, Token);
        Assert.True(created.IsSuccess);
        var saved = await database.Trees.SaveAsync(created.Value.Id, people, null, Token);
        Assert.True(saved.IsSuccess, string.Join("; ", saved.Errors));
        return saved.Value;
    }

    private static PersonData Kim(FamilyTreeDetail tree) => tree.People.Single(person => person.FirstName == "Kim");

    [Fact]
    public async Task Birth_and_adopted_sets_round_trip_with_type_date_and_default_primary()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tree = await SaveAsync(database, BirthAndAdopted());

        var kim = Kim(tree);
        Assert.Equal(2, kim.ParentSets.Count);
        var adopted = kim.ParentSets.Single(set => set.Type == ParentageType.Adopted);
        var birth = kim.ParentSets.Single(set => set.Type == ParentageType.Birth);

        // Adopted sets come first by default, so Kim's primary set is the adoptive one.
        Assert.True(adopted.IsPrimary);
        Assert.False(birth.IsPrimary);
        Assert.Equal(new PartialDate(1987), adopted.Date);
        Assert.Null(birth.Date);
        Assert.Equal(adopted.ParentIds, kim.Parents);
        Assert.Equal(["Alan", "Amy"], adopted.ParentIds.Select(id => tree.People.Single(person => person.Id == id).FirstName));
        Assert.Equal(["Bob", "Beth"], birth.ParentIds.Select(id => tree.People.Single(person => person.Id == id).FirstName));

        // Every parent, in any set, lists Kim as a child.
        foreach (string parentId in adopted.ParentIds.Concat(birth.ParentIds))
        {
            Assert.Contains(kim.Id, tree.People.Single(person => person.Id == parentId).Children);
        }
    }

    [Fact]
    public async Task A_flagged_primary_set_survives_a_save()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tree = await SaveAsync(database, BirthAndAdopted(flagBirthPrimary: true));

        var kim = Kim(tree);
        Assert.Equal(ParentageType.Birth, kim.ParentSets.Single(set => set.IsPrimary).Type);
        Assert.Equal(kim.ParentSets.Single(set => set.IsPrimary).ParentIds, kim.Parents);
    }

    [Fact]
    public async Task Saving_again_keeps_set_ids_and_applies_edits_and_removals()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tree = await SaveAsync(database, BirthAndAdopted());
        var kim = Kim(tree);
        string birthId = kim.ParentSets.Single(set => set.Type == ParentageType.Birth).Id;
        string adoptedId = kim.ParentSets.Single(set => set.Type == ParentageType.Adopted).Id;
        var amy = tree.People.Single(person => person.FirstName == "Amy");

        // Re-type the adoptive set as foster, make the birth set primary, and take Amy out of the set.
        var edited = FamilyGraphEditor.SetParentSetType(tree.People, kim.Id, adoptedId, ParentageType.Foster);
        Assert.True(edited.IsSuccess);
        edited = FamilyGraphEditor.SetPrimaryParentSet(edited.Value, kim.Id, birthId);
        Assert.True(edited.IsSuccess);
        edited = FamilyGraphEditor.Unlink(edited.Value, kim.Id, amy.Id, adoptedId);
        Assert.True(edited.IsSuccess);

        var saved = await database.Trees.SaveAsync(tree.Id, edited.Value, null, Token);
        Assert.True(saved.IsSuccess, string.Join("; ", saved.Errors));

        var after = Kim(saved.Value);
        Assert.Equal([birthId, adoptedId], after.ParentSets.Select(set => set.Id).Order());
        var foster = after.ParentSets.Single(set => set.Id == adoptedId);
        Assert.Equal(ParentageType.Foster, foster.Type);
        Assert.Equal(["Alan"], foster.ParentIds.Select(id => saved.Value.People.Single(person => person.Id == id).FirstName));
        Assert.True(after.ParentSets.Single(set => set.Id == birthId).IsPrimary);
        Assert.DoesNotContain(after.Id, saved.Value.People.Single(person => person.Id == amy.Id).Children);
    }

    [Fact]
    public async Task Removing_a_set_removes_it_and_removing_a_parent_person_shrinks_the_set()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tree = await SaveAsync(database, BirthAndAdopted());
        var kim = Kim(tree);
        string adoptedId = kim.ParentSets.Single(set => set.Type == ParentageType.Adopted).Id;
        var bob = tree.People.Single(person => person.FirstName == "Bob");

        var withoutSet = FamilyGraphEditor.RemoveParentSet(tree.People, kim.Id, adoptedId);
        Assert.True(withoutSet.IsSuccess);
        var withoutBob = FamilyGraphEditor.Remove(withoutSet.Value, bob.Id);
        Assert.True(withoutBob.IsSuccess);

        var saved = await database.Trees.SaveAsync(tree.Id, withoutBob.Value, null, Token);
        Assert.True(saved.IsSuccess, string.Join("; ", saved.Errors));

        var after = Kim(saved.Value);
        var only = Assert.Single(after.ParentSets);
        Assert.Equal(ParentageType.Birth, only.Type);
        Assert.True(only.IsPrimary);
        Assert.Equal(["Beth"], only.ParentIds.Select(id => saved.Value.People.Single(person => person.Id == id).FirstName));
        Assert.DoesNotContain(saved.Value.People, person => person.FirstName == "Bob");
    }

    [Fact]
    public async Task Deleting_the_tree_removes_its_sets()
    {
        await using var database = await TestDatabase.CreateAsync();
        var tree = await SaveAsync(database, BirthAndAdopted());

        Assert.True((await database.Trees.DeleteAsync(tree.Id, Token)).IsSuccess);

        await using var context = database.GetContext();
        Assert.Equal(0, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"MyWebTree_ParentSets\"").SingleAsync(Token));
        Assert.Equal(0, await context.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"MyWebTree_ParentSetMembers\"").SingleAsync(Token));
    }

    [Fact]
    public async Task Migration_turns_existing_parent_links_into_primary_birth_sets()
    {
        // The migration before parent sets, with a legacy database: two parents for Ann, one for Ben.
        await using var database = await TestDatabase.CreateAsync(migrateTo: "20261017070557_PersonDetails");
        await using (var context = database.GetContext())
        {
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO "Trees" ("Id", "Name", "CreatedAtUtc", "UpdatedAtUtc") VALUES (1, 'Legacy', '2026-01-01', '2026-01-01');
                INSERT INTO "People" ("Id", "TreeId", "Gender", "FirstName", "LastName") VALUES
                    (1, 1, 1, 'Mum', 'Lee'), (2, 1, 0, 'Dad', 'Lee'), (3, 1, 0, 'Ann', 'Lee'), (4, 1, 0, 'Ben', 'Lee');
                INSERT INTO "Relationships" ("TreeId", "FromPersonId", "ToPersonId", "Kind") VALUES
                    (1, 1, 3, 0), (1, 2, 3, 0), (1, 1, 4, 0), (1, 1, 2, 1);
                """, Token);
        }

        await using (var context = database.GetContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(null, Token);
        }

        var detail = await database.Trees.GetAsync(1, Token);
        Assert.True(detail.IsSuccess);

        var ann = detail.Value.People.Single(person => person.FirstName == "Ann");
        var annSet = Assert.Single(ann.ParentSets);
        Assert.Equal(ParentageType.Birth, annSet.Type);
        Assert.True(annSet.IsPrimary);
        Assert.Equal(["Mum", "Dad"], annSet.ParentIds.Select(id => detail.Value.People.Single(person => person.Id == id).FirstName));
        Assert.Equal(annSet.ParentIds, ann.Parents);

        var ben = detail.Value.People.Single(person => person.FirstName == "Ben");
        Assert.Equal(["Mum"], Assert.Single(ben.ParentSets).ParentIds.Select(id => detail.Value.People.Single(person => person.Id == id).FirstName));

        // The spouse link stays where it was; the parent links are gone from the old table.
        var mum = detail.Value.People.Single(person => person.FirstName == "Mum");
        Assert.Equal(["2"], mum.Spouses);
        Assert.Equal(["3", "4"], mum.Children.Order());

        await using var check = database.GetContext();
        Assert.Equal(0, await check.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS \"Value\" FROM \"MyWebTree_Relationships\" WHERE \"Kind\" = 0").SingleAsync(Token));
    }
}

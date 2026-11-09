using Extenso.Data.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyWebTree.Data;
using MyWebTree.Data.Sqlite;
using MyWebTree.Services;

namespace MyWebTree.Tests;

public class FamilyTreeServiceTests
{
    [Fact]
    public async Task Sample_round_trip_keeps_parents_spouses_and_a_new_child()
    {
        await using var database = await TestDatabase.CreateAsync();
        var created = await database.Trees.CreateSampleAsync(TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess);
        var david = created.Value.People.Single(person => person.GivenNames == "David");
        Assert.Equal(2, david.Parents.Count);
        Assert.Contains(created.Value.People, person => person.GivenNames == "Priya" && person.Spouses.Contains(david.Id));

        var withSon = FamilyGraphEditor.AddRelative(created.Value.People, new AddRelativeRequest
        {
            RelatesToPersonId = david.Id,
            Kind = RelativeKind.Son,
            Person = new PersonWriteModel { GivenNames = "Jonah", FamilyName = "Ward" },
        });
        var saved = await database.Trees.SaveAsync(created.Value.Id, withSon.Value, david.Id, TestContext.Current.CancellationToken);

        Assert.True(saved.IsSuccess);
        var again = saved.Value.People.Single(person => person.GivenNames == "David");
        var jonah = saved.Value.People.Single(person => person.GivenNames == "Jonah");
        Assert.Contains(jonah.Id, again.Children);
        Assert.Contains(again.Id, jonah.Parents);
        Assert.Equal(again.Id, saved.Value.FocusPersonId);
        Assert.Equal(new PartialDate(1953), again.BirthDate);
        var arthur = saved.Value.People.Single(person => person.GivenNames == "Arthur");
        Assert.Equal(new PartialDate(1924), arthur.BirthDate);
        Assert.Equal(new PartialDate(2001), arthur.DeathDate);

        var deleted = await database.Trees.DeleteAsync(created.Value.Id, TestContext.Current.CancellationToken);
        Assert.True(deleted.IsSuccess);
        var missing = await database.Trees.GetAsync(created.Value.Id, TestContext.Current.CancellationToken);
        Assert.Equal(ResultStatus.NotFound, missing.Status);
    }
}

public sealed class TestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection keepAlive;
    private readonly ServiceProvider provider;
    private readonly AsyncServiceScope scope;

    private TestDatabase(SqliteConnection keepAlive, ServiceProvider provider)
    {
        this.keepAlive = keepAlive;
        this.provider = provider;
        scope = provider.CreateAsyncScope();
    }

    public IFamilyTreeService Trees => scope.ServiceProvider.GetRequiredService<IFamilyTreeService>();

    /// <summary>Opens a new in-memory database, migrated to the given migration (or to the latest when none is given).</summary>
    public static async Task<TestDatabase> CreateAsync(string? migrateTo = null)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        string connectionString = $"Data Source=mwt-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddMyWebTreeServices().AddMyWebTreeSqlite(connectionString);

        var provider = services.BuildServiceProvider(validateScopes: true);
        await using (var context = provider.GetRequiredService<IMyWebTreeDbContextFactory>().GetContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(migrateTo, cancellationToken);
        }

        return new TestDatabase(keepAlive, provider);
    }

    public DbContext GetContext() => provider.GetRequiredService<IMyWebTreeDbContextFactory>().GetContext();

    public async ValueTask DisposeAsync()
    {
        await scope.DisposeAsync();
        await provider.DisposeAsync();
        await keepAlive.DisposeAsync();
    }
}

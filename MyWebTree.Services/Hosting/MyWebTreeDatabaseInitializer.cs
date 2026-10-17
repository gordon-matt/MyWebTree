using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyWebTree.Services;

namespace MyWebTree.Services.Hosting;

/// <summary>Applies MyWebTree migrations at startup and, when asked, seeds the sample family.</summary>
internal sealed class MyWebTreeDatabaseInitializer(
    ILogger<MyWebTreeDatabaseInitializer> logger,
    IOptions<MyWebTreeOptions> options,
    IServiceScopeFactory scopes) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var contextFactory = scope.ServiceProvider.GetRequiredService<IMyWebTreeDbContextFactory>();

        if (options.Value.AutoMigrate)
        {
            try
            {
                using var context = contextFactory.GetContext();
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("MyWebTree database is up to date ({Provider})", contextFactory.ProviderName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Could not migrate the MyWebTree database ({Provider})", contextFactory.ProviderName);
                return;
            }
        }

        if (!options.Value.SeedSample)
        {
            return;
        }

        try
        {
            var trees = scope.ServiceProvider.GetRequiredService<IRepository<FamilyTree>>();
            var service = scope.ServiceProvider.GetRequiredService<IFamilyTreeService>();
            var existing = await trees.FindAsync(new SearchOptions<FamilyTree>
            {
                PageSize = 1,
                CancellationToken = cancellationToken,
            });

            if (existing.Any())
            {
                return;
            }

            var seeded = await service.CreateSampleAsync(cancellationToken);
            if (seeded.IsSuccess)
            {
                logger.LogInformation("Seeded the {Name} sample", SampleFamily.Name);
            }
            else
            {
                logger.LogWarning("Could not seed the sample family: {Message}", seeded.GetDisplayMessage());
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not seed the MyWebTree sample family");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

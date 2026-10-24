using MyWebTree;
using MyWebTree.Data.MySql;
using MyWebTree.Data.Npgsql;
using MyWebTree.Data.Sql;
using MyWebTree.Data.Sqlite;
using MyWebTree.Demo.Blazor.Components;
using MyWebTree.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMyWebTreeServices(options => options.SeedSample = true);
AddDatabase(builder.Services, builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

static void AddDatabase(IServiceCollection services, IConfiguration configuration)
{
    string provider = configuration["Database:Provider"] ?? Constants.DatabaseProviders.Sqlite;
    string? connectionString = configuration.GetConnectionString("MyWebTree");

    if (provider.Equals(Constants.DatabaseProviders.Sqlite, StringComparison.OrdinalIgnoreCase))
    {
        services.AddMyWebTreeSqlite(connectionString);
        return;
    }

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException($"ConnectionStrings:MyWebTree is required when Database:Provider is {provider}.");
    }

    if (provider.Equals(Constants.DatabaseProviders.SqlServer, StringComparison.OrdinalIgnoreCase))
    {
        services.AddMyWebTreeSqlServer(connectionString);
        return;
    }

    if (provider.Equals(Constants.DatabaseProviders.Npgsql, StringComparison.OrdinalIgnoreCase))
    {
        services.AddMyWebTreeNpgsql(connectionString);
        return;
    }

    if (provider.Equals(Constants.DatabaseProviders.MySql, StringComparison.OrdinalIgnoreCase))
    {
        services.AddMyWebTreeMySql(connectionString);
        return;
    }

    throw new InvalidOperationException(
        $"Unknown Database:Provider '{provider}'. Valid values: Sqlite, SqlServer, Npgsql, MySql.");
}

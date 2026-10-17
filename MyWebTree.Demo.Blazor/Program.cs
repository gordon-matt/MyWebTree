using MyWebTree.Data.Sqlite;
using MyWebTree.Demo.Blazor.Components;
using MyWebTree.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services
    .AddMyWebTreeServices(options => options.SeedSample = true)
    .AddMyWebTreeSqlite(builder.Configuration.GetConnectionString("MyWebTree"));

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

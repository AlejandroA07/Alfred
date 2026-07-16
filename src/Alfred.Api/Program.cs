using Alfred.Modules.Finance;
using Alfred.Modules.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AlfredDb")
    ?? throw new InvalidOperationException("Connection string 'AlfredDb' is not configured.");

builder.Services.AddIdentityModule(connectionString, builder.Configuration["Identity:InviteCode"]);
builder.Services.AddFinanceModule(connectionString);

var app = builder.Build();

// Dev convenience: create/update the schema on startup. Replaced by a proper
// migration step before any shared/public deployment.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AlfredIdentityDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<AlfredFinanceDbContext>().Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapIdentityModule();
app.MapFinanceModule();

// Serve the built SPA (web/dist copied to wwwroot in deployment). In dev the
// Vite dev server proxies /api here instead.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();

/// <summary>Top-level statements make Program internal; tests need it to boot the host.</summary>
public partial class Program
{
    protected Program() { }
}

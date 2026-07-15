using Alfred.Modules.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AlfredDb")
    ?? throw new InvalidOperationException("Connection string 'AlfredDb' is not configured.");

builder.Services.AddIdentityModule(connectionString, builder.Configuration["Identity:InviteCode"]);

var app = builder.Build();

// Dev convenience: create/update the schema on startup. Replaced by a proper
// migration step before any shared/public deployment.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AlfredIdentityDbContext>().Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapIdentityModule();

// Serve the built SPA (web/dist copied to wwwroot in deployment). In dev the
// Vite dev server proxies /api here instead.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();

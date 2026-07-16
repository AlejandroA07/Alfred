using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Alfred.Tests;

/// <summary>
/// Boots the real host in Production against a database that isn't there (a closed
/// port). Suitable for anything decided before a handler runs — middleware, limits,
/// error shape — and for provoking a genuine unhandled exception on demand.
/// Production also keeps the developer exception page out of the way.
/// </summary>
internal sealed class NoDatabaseAppFactory(params (string Key, string Value)[] settings)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting(
            "ConnectionStrings:AlfredDb",
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused;Timeout=1");
        builder.UseSetting("Identity:InviteCode", "unused");

        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }
}

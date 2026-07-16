using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Alfred.Tests;

/// <summary>
/// Runs the real API against a throwaway Postgres container, so endpoint tests
/// exercise actual Npgsql, real indexes and real column types. Migrations are
/// applied by the app itself on startup (Development only).
/// </summary>
public sealed class AlfredAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string InviteCode = "test-invite-code";
    private const string Password = "Str0ngPassw0rd!";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async Task InitializeAsync() => await _db.StartAsync();

    // Explicit implementation: xunit's IAsyncLifetime.DisposeAsync returns Task
    // while WebApplicationFactory's returns ValueTask — they can't share a name.
    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:AlfredDb", _db.GetConnectionString());
        builder.UseSetting("Identity:InviteCode", InviteCode);
    }

    /// <summary>
    /// A client logged in as a fresh user, via the real register + login flow.
    /// Each call gets its own cookie jar, so two clients are two distinct users.
    /// </summary>
    public async Task<HttpClient> CreateLoggedInClientAsync()
    {
        var client = CreateClient();
        var email = $"user-{Guid.CreateVersion7():n}@example.com";

        using var register = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(new { email, password = Password }),
        };
        register.Headers.Add("X-Invite-Code", InviteCode);
        (await client.SendAsync(register)).EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync(
            "/api/auth/login?useCookies=true&useSessionCookies=true", new { email, password = Password });
        login.EnsureSuccessStatusCode();

        return client;
    }
}

/// <summary>Shares one container + host across every endpoint test class.</summary>
[CollectionDefinition(Name)]
public class AlfredApp : ICollectionFixture<AlfredAppFactory>
{
    public const string Name = "alfred-app";
}

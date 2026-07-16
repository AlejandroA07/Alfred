using System.Net;
using System.Text;

namespace Alfred.Tests;

/// <summary>
/// The ProblemDetails handler is deliberately off in Development (the developer
/// exception page wins there), so these run the host as Production.
/// </summary>
public class ProblemDetailsTests
{
    private const string InviteCode = "test-invite";

    [Fact]
    public async Task Unhandled_exception_returns_problem_details_and_leaks_no_internals()
    {
        // A high limit keeps the rate limiter out of this test's way.
        using var app = new NoDatabaseAppFactory(
            ("Identity:InviteCode", InviteCode), ("Identity:AuthRequestsPerMinute", "1000"));
        var client = app.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = new StringContent(
                """{"email":"probe@example.com","password":"Str0ngPassw0rd!"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Invite-Code", InviteCode);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        // The failure is a Postgres connection error; none of that may reach the client.
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at Microsoft.AspNetCore", body, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.Ordinal);
    }
}

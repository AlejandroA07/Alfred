using System.Net;

namespace Alfred.Tests;

/// <summary>
/// Uses unauthenticated /api/auth/logout: the limiter sits ahead of authentication,
/// so each call is rejected as 401 while still spending a permit — which exercises
/// the limit without needing a database or real credentials.
/// </summary>
public class AuthRateLimitTests
{
    private const int Limit = 3;

    private static NoDatabaseAppFactory CreateApp() =>
        new(("Identity:AuthRequestsPerMinute", Limit.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public async Task Auth_requests_beyond_the_limit_are_rejected_with_429()
    {
        using var app = CreateApp();
        var client = app.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < Limit + 1; i++)
        {
            var response = await client.PostAsync("/api/auth/logout", content: null);
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(Limit), status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[^1]);
    }

    [Fact]
    public async Task Endpoints_outside_the_auth_group_are_not_rate_limited()
    {
        using var app = CreateApp();
        var client = app.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < Limit + 3; i++)
        {
            var response = await client.GetAsync("/api/health");
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses, status => Assert.Equal(HttpStatusCode.OK, status));
    }
}

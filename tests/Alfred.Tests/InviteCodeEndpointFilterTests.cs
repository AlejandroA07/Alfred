using Alfred.Modules.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Alfred.Tests;

public class InviteCodeEndpointFilterTests
{
    private const string Configured = "alfred-dev-invite";

    private static async Task<object?> RunFilter(
        string method, string path, string? headerValue, string? configured = Configured)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        if (headerValue is not null)
        {
            httpContext.Request.Headers[InviteCodeEndpointFilter.HeaderName] = headerValue;
        }

        var filter = new InviteCodeEndpointFilter(configured);
        var context = new DefaultEndpointFilterInvocationContext(httpContext);
        return await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("next-called"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-code")]
    public async Task Register_without_valid_code_is_rejected_with_validation_problem(string? headerValue)
    {
        var result = await RunFilter("POST", "/api/auth/register", headerValue);

        var problem = Assert.IsType<ValidationProblem>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("inviteCode", problem.ProblemDetails.Errors.Keys);
    }

    [Fact]
    public async Task Register_with_correct_code_passes_through()
    {
        var result = await RunFilter("POST", "/api/auth/register", Configured);

        Assert.Equal("next-called", result);
    }

    [Theory]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/auth/logout")]
    [InlineData("GET", "/api/auth/manage/info")]
    public async Task Other_auth_endpoints_pass_through_without_a_code(string method, string path)
    {
        var result = await RunFilter(method, path, null);

        Assert.Equal("next-called", result);
    }

    [Fact]
    public async Task Register_is_rejected_when_no_code_is_configured_even_if_one_is_sent()
    {
        var result = await RunFilter("POST", "/api/auth/register", "anything", configured: null);

        Assert.IsType<ValidationProblem>(result);
    }
}

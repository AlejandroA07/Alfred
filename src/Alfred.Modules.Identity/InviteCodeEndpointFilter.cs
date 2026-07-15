using Microsoft.AspNetCore.Http;

namespace Alfred.Modules.Identity;

/// <summary>
/// Gates POST /register behind the X-Invite-Code header. Applied to the whole
/// auth group (MapIdentityApi offers no per-endpoint hook); every other
/// endpoint passes through untouched. All failures return the same response so
/// callers can't tell a wrong code from closed registration.
/// </summary>
public sealed class InviteCodeEndpointFilter(string? configuredInviteCode) : IEndpointFilter
{
    public const string HeaderName = "X-Invite-Code";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var isRegister = HttpMethods.IsPost(request.Method)
            && request.Path.Value?.EndsWith("/register", StringComparison.OrdinalIgnoreCase) == true;

        if (!isRegister)
        {
            return await next(context);
        }

        var provided = request.Headers[HeaderName].ToString();
        if (!RegistrationInviteCode.Matches(configuredInviteCode, provided))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["inviteCode"] = ["Invalid or missing invite code."],
            });
        }

        return await next(context);
    }
}

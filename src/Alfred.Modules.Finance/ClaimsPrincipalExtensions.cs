using System.Security.Claims;

namespace Alfred.Modules.Finance;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// The authenticated caller's Identity user id — the value every Finance query
    /// filters on. The endpoint group requires authorization, so a missing claim is
    /// a bug, not an expected anonymous request.
    /// </summary>
    internal static string RequireUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated request has no user id claim.");
}

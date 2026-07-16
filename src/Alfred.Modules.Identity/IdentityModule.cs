using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Modules.Identity;

public static class IdentityModule
{
    /// <summary>Rate-limit policy guarding the auth endpoints.</summary>
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        string connectionString,
        string? registrationInviteCode,
        int authRequestsPerMinute)
    {
        services.AddSingleton(new IdentityModuleOptions(registrationInviteCode, authRequestsPerMinute));
        services.AddDbContext<AlfredIdentityDbContext>(o => o.UseNpgsql(connectionString));

        // Per-account lockout already blunts brute force against one account; this
        // covers what lockout cannot — spraying many accounts from one source, and
        // plain request flooding.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // No remote IP (in-process hosting) collapses to one shared bucket,
                    // which fails closed rather than opening a hole.
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        services.AddAuthorization();
        services
            .AddIdentityApiEndpoints<AlfredUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 10;
            })
            .AddEntityFrameworkStores<AlfredIdentityDbContext>();

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityModule(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IdentityModuleOptions>();

        var group = endpoints.MapGroup("/api/auth")
            .AddEndpointFilter(new InviteCodeEndpointFilter(options.RegistrationInviteCode))
            .RequireRateLimiting(AuthRateLimitPolicy);
        group.MapIdentityApi<AlfredUser>();

        // MapIdentityApi has no sign-out endpoint of its own.
        group.MapPost("/logout", async (SignInManager<AlfredUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        return endpoints;
    }
}

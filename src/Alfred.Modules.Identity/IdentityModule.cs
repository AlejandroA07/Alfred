using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services, string connectionString, string? registrationInviteCode)
    {
        services.AddSingleton(new IdentityModuleOptions(registrationInviteCode));
        services.AddDbContext<AlfredIdentityDbContext>(o => o.UseNpgsql(connectionString));

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
            .AddEndpointFilter(new InviteCodeEndpointFilter(options.RegistrationInviteCode));
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

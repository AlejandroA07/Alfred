using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, string connectionString)
    {
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
        endpoints.MapGroup("/api/auth").MapIdentityApi<AlfredUser>();
        return endpoints;
    }
}

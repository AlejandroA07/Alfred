using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alfred.Modules.Finance;

public static class FinanceModule
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AlfredFinanceDbContext>(o => o.UseNpgsql(connectionString));
        return services;
    }

    public static IEndpointRouteBuilder MapFinanceModule(this IEndpointRouteBuilder endpoints)
    {
        // Everything under /api/finance is private user data: authenticated by
        // default here, and every query additionally filters on the caller's id.
        var group = endpoints.MapGroup("/api/finance").RequireAuthorization();

        group.MapCategoryEndpoints();
        group.MapExpenseEndpoints();
        group.MapMoneyMapEndpoints();

        return endpoints;
    }
}

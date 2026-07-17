using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Finance;

/// <summary>
/// The money map: a read-only view of a month's spend per category against each
/// category's monthly budget, plus the month totals. No writable state of its own.
/// </summary>
public static class MoneyMapEndpoints
{
    public record MoneyMapCategory(Guid CategoryId, string Name, string Color, decimal? MonthlyBudget, decimal Spent);

    public record MoneyMapResponse(
        string Month, decimal TotalSpent, decimal? TotalBudget, IReadOnlyList<MoneyMapCategory> Categories);

    internal static IEndpointRouteBuilder MapMoneyMapEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/money-map", async (
            string? month, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();

            DateOnly start, end;
            if (string.IsNullOrEmpty(month))
            {
                (start, end) = MonthRange.Current();
            }
            else if (!MonthRange.TryParse(month, out start, out end))
            {
                return MonthInvalid();
            }

            // Sum this month's spend per category (owner-scoped). Expenses whose
            // category was later deleted have no live row here and so fall out of the
            // map — the map is a view over the user's current categories.
            var spendByCategory = await db.Expenses
                .Where(e => e.UserId == userId && e.Date >= start && e.Date < end)
                .GroupBy(e => e.CategoryId)
                .Select(g => new { CategoryId = g.Key, Spent = g.Sum(e => e.Amount) })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Spent, ct);

            var categories = await db.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.Color, c.MonthlyBudget })
                .ToListAsync(ct);

            var rows = categories
                .Select(c => new MoneyMapCategory(
                    c.Id, c.Name, c.Color, c.MonthlyBudget,
                    spendByCategory.GetValueOrDefault(c.Id)))
                .ToList();

            var totalSpent = rows.Sum(r => r.Spent);
            var totalBudget = categories.Any(c => c.MonthlyBudget is not null)
                ? categories.Sum(c => c.MonthlyBudget ?? 0m)
                : (decimal?)null;

            return Results.Ok(new MoneyMapResponse(
                start.ToString("yyyy-MM", CultureInfo.InvariantCulture), totalSpent, totalBudget, rows));
        });

        return group;
    }

    private static IResult MonthInvalid() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["month"] = ["Month must be in YYYY-MM format."],
    });
}

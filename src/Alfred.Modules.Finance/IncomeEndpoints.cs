using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Finance;

public static class IncomeEndpoints
{
    public record IncomeRequest(decimal? Amount, DateOnly? Date, string? Source);

    public record IncomeResponse(Guid Id, decimal Amount, DateOnly Date, string Source);

    internal static IEndpointRouteBuilder MapIncomeEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/incomes", async (
            string? month, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();

            var query = db.Incomes.Where(i => i.UserId == userId);

            if (month is not null)
            {
                if (!MonthRange.TryParse(month, out var start, out var end))
                {
                    return MonthInvalid();
                }

                query = query.Where(i => i.Date >= start && i.Date < end);
            }

            var incomes = await query
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.CreatedAt)
                .Select(i => new IncomeResponse(i.Id, i.Amount, i.Date, i.Source))
                .ToListAsync(ct);

            return Results.Ok(incomes);
        });

        group.MapPost("/incomes", async (
            IncomeRequest request, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var income = new Income
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Amount = request.Amount!.Value,
                Date = request.Date!.Value,
                Source = request.Source!.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Incomes.Add(income);
            await db.SaveChangesAsync(ct);

            var response = new IncomeResponse(income.Id, income.Amount, income.Date, income.Source);
            return Results.Created($"/api/finance/incomes/{income.Id}", response);
        });

        group.MapPut("/incomes/{id:guid}", async (
            Guid id, IncomeRequest request, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var income = await db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);
            if (income is null)
            {
                return Results.NotFound();
            }

            income.Amount = request.Amount!.Value;
            income.Date = request.Date!.Value;
            income.Source = request.Source!.Trim();
            await db.SaveChangesAsync(ct);

            return Results.Ok(new IncomeResponse(income.Id, income.Amount, income.Date, income.Source));
        });

        group.MapDelete("/incomes/{id:guid}", async (
            Guid id, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            var deleted = await db.Incomes
                .Where(i => i.Id == id && i.UserId == userId)
                .ExecuteDeleteAsync(ct);

            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }

    /// <summary>Largest amount we accept — guards the numeric(12,2) column from overflow.</summary>
    private const decimal MaxAmount = 9_999_999_999m;

    private static IResult? Validate(IncomeRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Amount is not { } amount || amount <= 0 || amount > MaxAmount)
        {
            errors["amount"] = ["Amount must be between 0 and 9,999,999,999."];
        }

        if (request.Date is null)
        {
            errors["date"] = ["Date is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Source))
        {
            errors["source"] = ["Source is required."];
        }
        else if (request.Source.Trim().Length > Income.SourceMaxLength)
        {
            errors["source"] = [$"Source must be at most {Income.SourceMaxLength} characters."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult MonthInvalid() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["month"] = ["Month must be in YYYY-MM format."],
    });
}

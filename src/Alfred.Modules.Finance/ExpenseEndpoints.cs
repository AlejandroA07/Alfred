using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Finance;

public static class ExpenseEndpoints
{
    public record ExpenseRequest(Guid? CategoryId, decimal? Amount, DateOnly? Date, string? Note);

    public record ExpenseResponse(Guid Id, Guid CategoryId, decimal Amount, DateOnly Date, string? Note);

    internal static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/expenses", async (
            string? month, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();

            var query = db.Expenses.Where(e => e.UserId == userId);

            if (month is not null)
            {
                if (!TryParseMonth(month, out var start, out var end))
                {
                    return MonthInvalid();
                }

                query = query.Where(e => e.Date >= start && e.Date < end);
            }

            var expenses = await query
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .Select(e => new ExpenseResponse(e.Id, e.CategoryId, e.Amount, e.Date, e.Note))
                .ToListAsync(ct);

            return Results.Ok(expenses);
        });

        group.MapPost("/expenses", async (
            ExpenseRequest request, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            if (!await CategoryBelongsToUserAsync(db, userId, request.CategoryId!.Value, ct))
            {
                return UnknownCategory();
            }

            var expense = new Expense
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                CategoryId = request.CategoryId!.Value,
                Amount = request.Amount!.Value,
                Date = request.Date!.Value,
                Note = NormaliseNote(request.Note),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Expenses.Add(expense);
            await db.SaveChangesAsync(ct);

            var response = new ExpenseResponse(
                expense.Id, expense.CategoryId, expense.Amount, expense.Date, expense.Note);
            return Results.Created($"/api/finance/expenses/{expense.Id}", response);
        });

        group.MapPut("/expenses/{id:guid}", async (
            Guid id, ExpenseRequest request, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
            if (expense is null)
            {
                return Results.NotFound();
            }

            if (!await CategoryBelongsToUserAsync(db, userId, request.CategoryId!.Value, ct))
            {
                return UnknownCategory();
            }

            expense.CategoryId = request.CategoryId!.Value;
            expense.Amount = request.Amount!.Value;
            expense.Date = request.Date!.Value;
            expense.Note = NormaliseNote(request.Note);
            await db.SaveChangesAsync(ct);

            return Results.Ok(new ExpenseResponse(
                expense.Id, expense.CategoryId, expense.Amount, expense.Date, expense.Note));
        });

        group.MapDelete("/expenses/{id:guid}", async (
            Guid id, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            var deleted = await db.Expenses
                .Where(e => e.Id == id && e.UserId == userId)
                .ExecuteDeleteAsync(ct);

            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }

    /// <summary>Largest amount we accept — guards the numeric(12,2) column from overflow.</summary>
    private const decimal MaxAmount = 9_999_999_999m;

    private static IResult? Validate(ExpenseRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CategoryId is null || request.CategoryId.Value == Guid.Empty)
        {
            errors["categoryId"] = ["Category is required."];
        }

        if (request.Amount is not { } amount || amount <= 0 || amount > MaxAmount)
        {
            errors["amount"] = ["Amount must be between 0 and 9,999,999,999."];
        }

        if (request.Date is null)
        {
            errors["date"] = ["Date is required."];
        }

        if (request.Note is { } note && note.Trim().Length > Expense.NoteMaxLength)
        {
            errors["note"] = [$"Note must be at most {Expense.NoteMaxLength} characters."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    /// <summary>
    /// The IDOR guard: an expense may only reference a category the caller owns. A
    /// foreign or unknown category id is rejected as invalid input, never attached.
    /// </summary>
    private static Task<bool> CategoryBelongsToUserAsync(
        AlfredFinanceDbContext db, string userId, Guid categoryId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => c.Id == categoryId && c.UserId == userId, ct);

    private static IResult UnknownCategory() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["categoryId"] = ["That category does not exist."],
    });

    private static IResult MonthInvalid() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["month"] = ["Month must be in YYYY-MM format."],
    });

    private static string? NormaliseNote(string? note)
    {
        var trimmed = note?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool TryParseMonth(string month, out DateOnly start, out DateOnly end)
    {
        end = default;

        if (!DateOnly.TryParseExact(
            month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out start))
        {
            return false;
        }

        end = start.AddMonths(1);
        return true;
    }
}

using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Finance;

public static partial class CategoryEndpoints
{
    public record CategoryRequest(string? Name, string? Color, decimal? MonthlyBudget);

    public record CategoryResponse(Guid Id, string Name, string Color, decimal? MonthlyBudget);

    internal static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/categories", async (ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            var categories = await db.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryResponse(c.Id, c.Name, c.Color, c.MonthlyBudget))
                .ToListAsync(ct);

            return Results.Ok(categories);
        });

        group.MapPost("/categories", async (
            CategoryRequest request, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var name = request.Name!.Trim();
            if (await db.Categories.AnyAsync(c => c.UserId == userId && c.Name == name, ct))
            {
                return NameTaken();
            }

            var category = new Category
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                Name = name,
                Color = request.Color!.ToLowerInvariant(),
                MonthlyBudget = request.MonthlyBudget,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);

            var response = new CategoryResponse(category.Id, category.Name, category.Color, category.MonthlyBudget);
            return Results.Created($"/api/finance/categories/{category.Id}", response);
        });

        group.MapPut("/categories/{id:guid}", async (
            Guid id, CategoryRequest request, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            if (Validate(request) is { } problem)
            {
                return problem;
            }

            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
            if (category is null)
            {
                return Results.NotFound();
            }

            var name = request.Name!.Trim();
            if (await db.Categories.AnyAsync(c => c.UserId == userId && c.Name == name && c.Id != id, ct))
            {
                return NameTaken();
            }

            category.Name = name;
            category.Color = request.Color!.ToLowerInvariant();
            category.MonthlyBudget = request.MonthlyBudget;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new CategoryResponse(category.Id, category.Name, category.Color, category.MonthlyBudget));
        });

        group.MapDelete("/categories/{id:guid}", async (
            Guid id, ClaimsPrincipal user, AlfredFinanceDbContext db, CancellationToken ct) =>
        {
            var userId = user.RequireUserId();
            var deleted = await db.Categories
                .Where(c => c.Id == id && c.UserId == userId)
                .ExecuteDeleteAsync(ct);

            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return group;
    }

    /// <summary>Largest budget we accept — guards the numeric(12,2) column from overflow.</summary>
    private const decimal MaxBudget = 9_999_999_999m;

    private static IResult? Validate(CategoryRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (name.Length > Category.NameMaxLength)
        {
            errors["name"] = [$"Name must be at most {Category.NameMaxLength} characters."];
        }

        if (string.IsNullOrEmpty(request.Color) || !HexColor().IsMatch(request.Color))
        {
            errors["color"] = ["Color must be a hex value like #4f46e5."];
        }

        if (request.MonthlyBudget is { } budget && (budget < 0 || budget > MaxBudget))
        {
            errors["monthlyBudget"] = ["Monthly budget must be between 0 and 9,999,999,999."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult NameTaken() => Results.ValidationProblem(new Dictionary<string, string[]>
    {
        ["name"] = ["You already have a category with that name."],
    });

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}

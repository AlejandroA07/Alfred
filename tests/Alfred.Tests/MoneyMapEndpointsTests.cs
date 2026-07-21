using System.Net;
using System.Net.Http.Json;
using Alfred.Modules.Finance;

namespace Alfred.Tests;

[Collection(AlfredApp.Name)]
public class MoneyMapEndpointsTests(AlfredAppFactory app)
{
    private static async Task<Guid> CreateCategoryAsync(
        HttpClient client, string name = "Food", decimal? monthlyBudget = 6000m)
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/categories", new { name, color = "#4f46e5", monthlyBudget });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CategoryEndpoints.CategoryResponse>();
        return created!.Id;
    }

    private static async Task CreateExpenseAsync(
        HttpClient client, Guid categoryId, decimal amount, string date)
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/expenses", new { categoryId, amount, date, note = (string?)null });
        response.EnsureSuccessStatusCode();
    }

    private static async Task CreateIncomeAsync(
        HttpClient client, decimal amount, string date, string source = "Salary")
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/incomes", new { amount, date, source });
        response.EnsureSuccessStatusCode();
    }

    private static Task<MoneyMapEndpoints.MoneyMapResponse?> GetMoneyMapAsync(HttpClient client, string month) =>
        client.GetFromJsonAsync<MoneyMapEndpoints.MoneyMapResponse>($"/api/finance/money-map?month={month}");

    [Fact]
    public async Task Money_map_requires_authentication()
    {
        var anonymous = app.CreateClient();

        var response = await anonymous.GetAsync("/api/finance/money-map");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sums_a_months_spend_per_category_against_its_budget()
    {
        var client = await app.CreateLoggedInClientAsync();
        var foodId = await CreateCategoryAsync(client, "Food", monthlyBudget: 400m);
        var transportId = await CreateCategoryAsync(client, "Transport", monthlyBudget: 100m);

        await CreateExpenseAsync(client, foodId, 30m, "2026-07-05");
        await CreateExpenseAsync(client, foodId, 12.50m, "2026-07-20");
        await CreateExpenseAsync(client, transportId, 8m, "2026-07-15");

        var map = await GetMoneyMapAsync(client, "2026-07");

        Assert.Equal("2026-07", map!.Month);
        Assert.Equal(50.50m, map.TotalSpent);
        Assert.Equal(500m, map.TotalBudget);

        // Categories come back ordered by name: Food, then Transport.
        Assert.Collection(
            map.Categories,
            food =>
            {
                Assert.Equal(foodId, food.CategoryId);
                Assert.Equal(42.50m, food.Spent);
                Assert.Equal(400m, food.MonthlyBudget);
            },
            transport =>
            {
                Assert.Equal(transportId, transport.CategoryId);
                Assert.Equal(8m, transport.Spent);
                Assert.Equal(100m, transport.MonthlyBudget);
            });
    }

    [Fact]
    public async Task Excludes_other_months_and_lists_categories_with_no_spend_as_zero()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client);
        await CreateExpenseAsync(client, categoryId, 40m, "2026-07-10");
        await CreateExpenseAsync(client, categoryId, 99m, "2026-06-30");

        var map = await GetMoneyMapAsync(client, "2026-07");

        Assert.Equal(40m, map!.TotalSpent);
        var only = Assert.Single(map.Categories);
        Assert.Equal(40m, only.Spent);
    }

    [Fact]
    public async Task Another_users_expenses_and_categories_are_never_included()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var ownerCategoryId = await CreateCategoryAsync(owner, "Owner");
        await CreateExpenseAsync(owner, ownerCategoryId, 25m, "2026-07-01");

        var map = await GetMoneyMapAsync(stranger, "2026-07");

        Assert.Equal(0m, map!.TotalSpent);
        Assert.Empty(map.Categories);
    }

    [Fact]
    public async Task Total_budget_is_null_when_no_category_has_a_budget()
    {
        var client = await app.CreateLoggedInClientAsync();
        await CreateCategoryAsync(client, "Untracked", monthlyBudget: null);

        var map = await GetMoneyMapAsync(client, "2026-07");

        Assert.Null(map!.TotalBudget);
        var only = Assert.Single(map.Categories);
        Assert.Null(only.MonthlyBudget);
        Assert.Equal(0m, only.Spent);
    }

    [Fact]
    public async Task Sums_the_months_income_and_leaves_the_rest_unallocated()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client, "Food", monthlyBudget: 400m);
        await CreateIncomeAsync(client, 2000m, "2026-07-25");
        await CreateIncomeAsync(client, 150.50m, "2026-07-28", source: "Side project");
        await CreateExpenseAsync(client, categoryId, 42.50m, "2026-07-05");

        var map = await GetMoneyMapAsync(client, "2026-07");

        Assert.Equal(2150.50m, map!.TotalIncome);
        Assert.Equal(42.50m, map.TotalSpent);
        Assert.Equal(2108m, map.Unallocated);
    }

    [Fact]
    public async Task Income_from_another_month_is_excluded()
    {
        var client = await app.CreateLoggedInClientAsync();
        await CreateIncomeAsync(client, 2000m, "2026-07-25");
        await CreateIncomeAsync(client, 999m, "2026-06-25");

        var map = await GetMoneyMapAsync(client, "2026-07");

        Assert.Equal(2000m, map!.TotalIncome);
        Assert.Equal(2000m, map.Unallocated);
    }

    [Fact]
    public async Task Another_users_income_is_never_included()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        await CreateIncomeAsync(owner, 2000m, "2026-07-25");

        var map = await GetMoneyMapAsync(stranger, "2026-07");

        Assert.Equal(0m, map!.TotalIncome);
        Assert.Equal(0m, map.Unallocated);
    }

    [Fact]
    public async Task Unallocated_goes_negative_when_spending_exceeds_income()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client, "Food", monthlyBudget: 400m);
        await CreateIncomeAsync(client, 100m, "2026-07-01");
        await CreateExpenseAsync(client, categoryId, 250m, "2026-07-10");

        var map = await GetMoneyMapAsync(client, "2026-07");

        Assert.Equal(-150m, map!.Unallocated);
    }

    [Fact]
    public async Task Invalid_month_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.GetAsync("/api/finance/money-map?month=2026-13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

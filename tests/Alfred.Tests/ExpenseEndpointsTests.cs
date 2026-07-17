using System.Net;
using System.Net.Http.Json;
using Alfred.Modules.Finance;

namespace Alfred.Tests;

[Collection(AlfredApp.Name)]
public class ExpenseEndpointsTests(AlfredAppFactory app)
{
    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name = "Food")
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/categories", new { name, color = "#4f46e5", monthlyBudget = 6000m });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CategoryEndpoints.CategoryResponse>();
        return created!.Id;
    }

    private static async Task<Guid> CreateExpenseAsync(
        HttpClient client, Guid categoryId, decimal amount = 25m, string date = "2026-07-10", string? note = "Lunch")
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/expenses", new { categoryId, amount, date, note });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<ExpenseEndpoints.ExpenseResponse>();
        return created!.Id;
    }

    [Fact]
    public async Task Expenses_require_authentication()
    {
        var anonymous = app.CreateClient();

        var response = await anonymous.GetAsync("/api/finance/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logged_expense_is_returned_in_the_owners_list()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client);

        var id = await CreateExpenseAsync(client, categoryId, amount: 42.50m, note: "Groceries");
        var expenses = await client.GetFromJsonAsync<List<ExpenseEndpoints.ExpenseResponse>>(
            "/api/finance/expenses");

        var expense = Assert.Single(expenses!);
        Assert.Equal(id, expense.Id);
        Assert.Equal(categoryId, expense.CategoryId);
        Assert.Equal(42.50m, expense.Amount);
        Assert.Equal(new DateOnly(2026, 7, 10), expense.Date);
        Assert.Equal("Groceries", expense.Note);
    }

    [Fact]
    public async Task Expenses_are_not_visible_to_another_user()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(owner);
        await CreateExpenseAsync(owner, categoryId);

        var visible = await stranger.GetFromJsonAsync<List<ExpenseEndpoints.ExpenseResponse>>(
            "/api/finance/expenses");

        Assert.Empty(visible!);
    }

    [Fact]
    public async Task Cannot_log_an_expense_against_another_users_category()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var foreignCategoryId = await CreateCategoryAsync(owner, "Private");

        // The IDOR guard: a category id the caller does not own must be rejected,
        // never attached to their expense.
        var response = await stranger.PostAsJsonAsync(
            "/api/finance/expenses",
            new { categoryId = foreignCategoryId, amount = 10m, date = "2026-07-01", note = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var strangerExpenses = await stranger.GetFromJsonAsync<List<ExpenseEndpoints.ExpenseResponse>>(
            "/api/finance/expenses");
        Assert.Empty(strangerExpenses!);
    }

    [Fact]
    public async Task Unknown_category_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/finance/expenses",
            new { categoryId = Guid.NewGuid(), amount = 10m, date = "2026-07-01", note = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Another_user_cannot_update_or_delete_someone_elses_expense()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(owner);
        var id = await CreateExpenseAsync(owner, categoryId);

        // Give the stranger a valid category of their own, so a well-formed payload
        // isolates the expense-ownership check as the only thing stopping them.
        var strangerCategoryId = await CreateCategoryAsync(stranger);
        var payload = new { categoryId = strangerCategoryId, amount = 99m, date = "2026-07-05", note = "hijack" };

        var update = await stranger.PutAsJsonAsync($"/api/finance/expenses/{id}", payload);
        var delete = await stranger.DeleteAsync($"/api/finance/expenses/{id}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var stillThere = await owner.GetFromJsonAsync<List<ExpenseEndpoints.ExpenseResponse>>(
            "/api/finance/expenses");
        Assert.Single(stillThere!);
    }

    [Fact]
    public async Task Cannot_move_an_expense_onto_another_users_category()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var ownerCategoryId = await CreateCategoryAsync(owner);
        var id = await CreateExpenseAsync(owner, ownerCategoryId);
        var strangerCategoryId = await CreateCategoryAsync(stranger, "Stranger");

        var response = await owner.PutAsJsonAsync(
            $"/api/finance/expenses/{id}",
            new { categoryId = strangerCategoryId, amount = 10m, date = "2026-07-02", note = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_expense()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client);
        var otherCategoryId = await CreateCategoryAsync(client, "Transport");
        var id = await CreateExpenseAsync(client, categoryId);

        var response = await client.PutAsJsonAsync(
            $"/api/finance/expenses/{id}",
            new { categoryId = otherCategoryId, amount = 12.34m, date = "2026-06-30", note = "  Bus  " });
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<ExpenseEndpoints.ExpenseResponse>();
        Assert.Equal(otherCategoryId, updated!.CategoryId);
        Assert.Equal(12.34m, updated.Amount);
        Assert.Equal(new DateOnly(2026, 6, 30), updated.Date);
        Assert.Equal("Bus", updated.Note);
    }

    [Fact]
    public async Task Delete_removes_the_expense()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client);
        var id = await CreateExpenseAsync(client, categoryId);

        var deleted = await client.DeleteAsync($"/api/finance/expenses/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<ExpenseEndpoints.ExpenseResponse>>(
            "/api/finance/expenses");
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Month_filter_returns_only_that_months_expenses()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client);
        await CreateExpenseAsync(client, categoryId, date: "2026-07-10", note: "July");
        await CreateExpenseAsync(client, categoryId, date: "2026-06-28", note: "June");

        var july = await client.GetFromJsonAsync<List<ExpenseEndpoints.ExpenseResponse>>(
            "/api/finance/expenses?month=2026-07");

        var only = Assert.Single(july!);
        Assert.Equal("July", only.Note);
    }

    [Fact]
    public async Task Invalid_month_filter_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.GetAsync("/api/finance/expenses?month=2026-13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(null, 10, "2026-07-01")]
    [InlineData("category", 0, "2026-07-01")]
    [InlineData("category", -5, "2026-07-01")]
    [InlineData("category", 10, null)]
    public async Task Invalid_input_is_rejected(string? categoryKind, int amount, string? date)
    {
        var client = await app.CreateLoggedInClientAsync();
        // "category" means substitute a real owned category id; null means omit it.
        object? categoryId = categoryKind is null ? null : await CreateCategoryAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/finance/expenses",
            new { categoryId, amount = (decimal)amount, date, note = (string?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Note_longer_than_the_limit_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();
        var categoryId = await CreateCategoryAsync(client);

        var response = await client.PostAsJsonAsync(
            "/api/finance/expenses",
            new
            {
                categoryId,
                amount = 10m,
                date = "2026-07-01",
                note = new string('x', Expense.NoteMaxLength + 1),
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

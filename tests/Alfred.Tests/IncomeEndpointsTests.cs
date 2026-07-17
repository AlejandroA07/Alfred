using System.Net;
using System.Net.Http.Json;
using Alfred.Modules.Finance;

namespace Alfred.Tests;

[Collection(AlfredApp.Name)]
public class IncomeEndpointsTests(AlfredAppFactory app)
{
    private static async Task<Guid> CreateIncomeAsync(
        HttpClient client, decimal amount = 30000m, string date = "2026-07-25", string source = "Salary")
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/incomes", new { amount, date, source });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<IncomeEndpoints.IncomeResponse>();
        return created!.Id;
    }

    [Fact]
    public async Task Incomes_require_authentication()
    {
        var anonymous = app.CreateClient();

        var response = await anonymous.GetAsync("/api/finance/incomes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logged_income_is_returned_in_the_owners_list()
    {
        var client = await app.CreateLoggedInClientAsync();

        var id = await CreateIncomeAsync(client, amount: 32500.50m, source: "Salary");
        var incomes = await client.GetFromJsonAsync<List<IncomeEndpoints.IncomeResponse>>(
            "/api/finance/incomes");

        var income = Assert.Single(incomes!);
        Assert.Equal(id, income.Id);
        Assert.Equal(32500.50m, income.Amount);
        Assert.Equal(new DateOnly(2026, 7, 25), income.Date);
        Assert.Equal("Salary", income.Source);
    }

    [Fact]
    public async Task Incomes_are_not_visible_to_another_user()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        await CreateIncomeAsync(owner);

        var visible = await stranger.GetFromJsonAsync<List<IncomeEndpoints.IncomeResponse>>(
            "/api/finance/incomes");

        Assert.Empty(visible!);
    }

    [Fact]
    public async Task Another_user_cannot_update_or_delete_someone_elses_income()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var id = await CreateIncomeAsync(owner);

        var payload = new { amount = 99m, date = "2026-07-05", source = "hijack" };
        var update = await stranger.PutAsJsonAsync($"/api/finance/incomes/{id}", payload);
        var delete = await stranger.DeleteAsync($"/api/finance/incomes/{id}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var stillThere = await owner.GetFromJsonAsync<List<IncomeEndpoints.IncomeResponse>>(
            "/api/finance/incomes");
        Assert.Single(stillThere!);
    }

    [Fact]
    public async Task Update_changes_the_income()
    {
        var client = await app.CreateLoggedInClientAsync();
        var id = await CreateIncomeAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/finance/incomes/{id}",
            new { amount = 1500.25m, date = "2026-06-30", source = "  Freelance  " });
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<IncomeEndpoints.IncomeResponse>();
        Assert.Equal(1500.25m, updated!.Amount);
        Assert.Equal(new DateOnly(2026, 6, 30), updated.Date);
        Assert.Equal("Freelance", updated.Source);
    }

    [Fact]
    public async Task Delete_removes_the_income()
    {
        var client = await app.CreateLoggedInClientAsync();
        var id = await CreateIncomeAsync(client);

        var deleted = await client.DeleteAsync($"/api/finance/incomes/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<IncomeEndpoints.IncomeResponse>>(
            "/api/finance/incomes");
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Month_filter_returns_only_that_months_incomes()
    {
        var client = await app.CreateLoggedInClientAsync();
        await CreateIncomeAsync(client, date: "2026-07-25", source: "July");
        await CreateIncomeAsync(client, date: "2026-06-25", source: "June");

        var july = await client.GetFromJsonAsync<List<IncomeEndpoints.IncomeResponse>>(
            "/api/finance/incomes?month=2026-07");

        var only = Assert.Single(july!);
        Assert.Equal("July", only.Source);
    }

    [Fact]
    public async Task Invalid_month_filter_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.GetAsync("/api/finance/incomes?month=2026-13");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0, "2026-07-01", "Salary")]
    [InlineData(-5, "2026-07-01", "Salary")]
    [InlineData(10, null, "Salary")]
    [InlineData(10, "2026-07-01", "")]
    [InlineData(10, "2026-07-01", "   ")]
    public async Task Invalid_input_is_rejected(int amount, string? date, string? source)
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/finance/incomes",
            new { amount = (decimal)amount, date, source });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Source_longer_than_the_limit_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/finance/incomes",
            new { amount = 10m, date = "2026-07-01", source = new string('x', Income.SourceMaxLength + 1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

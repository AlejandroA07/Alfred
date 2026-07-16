using System.Net;
using System.Net.Http.Json;
using Alfred.Modules.Finance;

namespace Alfred.Tests;

[Collection(AlfredApp.Name)]
public class CategoryEndpointsTests(AlfredAppFactory app)
{
    private static readonly object ValidCategory = new { name = "Food", color = "#4f46e5", monthlyBudget = 6000m };

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string name = "Food")
    {
        var response = await client.PostAsJsonAsync(
            "/api/finance/categories", new { name, color = "#4f46e5", monthlyBudget = 6000m });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<CategoryEndpoints.CategoryResponse>();
        return created!.Id;
    }

    [Fact]
    public async Task Categories_require_authentication()
    {
        var anonymous = app.CreateClient();

        var response = await anonymous.GetAsync("/api/finance/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Created_category_is_returned_in_the_owners_list()
    {
        var client = await app.CreateLoggedInClientAsync();

        var id = await CreateCategoryAsync(client, "Groceries");
        var categories = await client.GetFromJsonAsync<List<CategoryEndpoints.CategoryResponse>>(
            "/api/finance/categories");

        var category = Assert.Single(categories!);
        Assert.Equal(id, category.Id);
        Assert.Equal("Groceries", category.Name);
        Assert.Equal(6000m, category.MonthlyBudget);
    }

    [Fact]
    public async Task Categories_are_not_visible_to_another_user()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        await CreateCategoryAsync(owner, "Private spending");

        var visible = await stranger.GetFromJsonAsync<List<CategoryEndpoints.CategoryResponse>>(
            "/api/finance/categories");

        Assert.Empty(visible!);
    }

    [Fact]
    public async Task Another_user_cannot_update_or_delete_someone_elses_category()
    {
        var owner = await app.CreateLoggedInClientAsync();
        var stranger = await app.CreateLoggedInClientAsync();
        var id = await CreateCategoryAsync(owner, "Car");

        var update = await stranger.PutAsJsonAsync($"/api/finance/categories/{id}", ValidCategory);
        var delete = await stranger.DeleteAsync($"/api/finance/categories/{id}");

        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        // And the owner's category survived both attempts.
        var stillThere = await owner.GetFromJsonAsync<List<CategoryEndpoints.CategoryResponse>>(
            "/api/finance/categories");
        Assert.Single(stillThere!);
    }

    [Fact]
    public async Task Duplicate_name_for_the_same_user_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();
        await CreateCategoryAsync(client, "Bills");

        var duplicate = await client.PostAsJsonAsync(
            "/api/finance/categories", new { name = "Bills", color = "#000000", monthlyBudget = (decimal?)null });

        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task Two_users_may_each_have_a_category_with_the_same_name()
    {
        var first = await app.CreateLoggedInClientAsync();
        var second = await app.CreateLoggedInClientAsync();

        await CreateCategoryAsync(first, "Food outside");
        var response = await second.PostAsJsonAsync(
            "/api/finance/categories", new { name = "Food outside", color = "#111111", monthlyBudget = 500m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Update_changes_the_category()
    {
        var client = await app.CreateLoggedInClientAsync();
        var id = await CreateCategoryAsync(client, "Presents");

        var response = await client.PutAsJsonAsync(
            $"/api/finance/categories/{id}", new { name = "Gifts", color = "#ABCDEF", monthlyBudget = 250.55m });
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<CategoryEndpoints.CategoryResponse>();
        Assert.Equal("Gifts", updated!.Name);
        Assert.Equal("#abcdef", updated.Color);
        Assert.Equal(250.55m, updated.MonthlyBudget);
    }

    [Fact]
    public async Task Delete_removes_the_category()
    {
        var client = await app.CreateLoggedInClientAsync();
        var id = await CreateCategoryAsync(client, "Temporary");

        var deleted = await client.DeleteAsync($"/api/finance/categories/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var remaining = await client.GetFromJsonAsync<List<CategoryEndpoints.CategoryResponse>>(
            "/api/finance/categories");
        Assert.Empty(remaining!);
    }

    [Theory]
    [InlineData("", "#4f46e5", null)]
    [InlineData("   ", "#4f46e5", null)]
    [InlineData("Food", "not-a-color", null)]
    [InlineData("Food", "#4f46e", null)]
    [InlineData("Food", "#4f46e5", -1)]
    public async Task Invalid_input_is_rejected(string name, string color, int? budget)
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/finance/categories", new { name, color, monthlyBudget = (decimal?)budget });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Name_longer_than_the_limit_is_rejected()
    {
        var client = await app.CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/finance/categories",
            new { name = new string('x', Category.NameMaxLength + 1), color = "#4f46e5", monthlyBudget = (decimal?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

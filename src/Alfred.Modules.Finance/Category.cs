namespace Alfred.Modules.Finance;

/// <summary>
/// A user-defined spending category. Scope (personal|shared) and split rules
/// arrive with the partner link in M2; until then every category is personal.
/// </summary>
public class Category
{
    public const int NameMaxLength = 60;

    public Guid Id { get; set; }

    /// <summary>
    /// Owning Identity user id. Deliberately not a foreign key: modules never
    /// reference each other, so the identity schema stays on its own side of the
    /// boundary and this is filtered in application code on every query.
    /// </summary>
    public required string UserId { get; set; }

    public required string Name { get; set; }

    /// <summary>Hex colour (#rrggbb) used by the money map and category chips.</summary>
    public required string Color { get; set; }

    /// <summary>Monthly budget in the household currency; null means untracked.</summary>
    public decimal? MonthlyBudget { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

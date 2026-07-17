namespace Alfred.Modules.Finance;

/// <summary>
/// A single income entry (salary, side income) — the top of the money map's monthly
/// flow. Personal until the M2 partner link; sharing fields arrive with that milestone.
/// </summary>
public class Income
{
    public const int SourceMaxLength = 60;

    public Guid Id { get; set; }

    /// <summary>
    /// Owning Identity user id. Like <see cref="Expense.UserId"/>, deliberately not a
    /// foreign key across the module boundary — filtered in application code on every query.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>Amount received in the household currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>The day the income was received (no time component).</summary>
    public DateOnly Date { get; set; }

    /// <summary>Where the money came from, e.g. "Salary" — a short required label.</summary>
    public required string Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

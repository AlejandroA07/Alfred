namespace Alfred.Modules.Finance;

/// <summary>
/// A single logged expense against one of the user's categories. Personal until
/// the M2 partner link; sharing/split fields arrive with that milestone.
/// </summary>
public class Expense
{
    public const int NoteMaxLength = 200;

    public Guid Id { get; set; }

    /// <summary>
    /// Owning Identity user id. Like <see cref="Category.UserId"/>, deliberately not
    /// a foreign key across the module boundary — filtered in application code on
    /// every query.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// The category this expense belongs to. A plain id, not a navigation FK: the
    /// endpoint verifies the category belongs to the caller before saving, so a
    /// foreign category id can never be attached.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>Amount spent in the household currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>The day the money was spent (no time component).</summary>
    public DateOnly Date { get; set; }

    /// <summary>Optional free-text note; null means none.</summary>
    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

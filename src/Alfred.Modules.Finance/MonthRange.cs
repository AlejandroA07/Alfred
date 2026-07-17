using System.Globalization;

namespace Alfred.Modules.Finance;

/// <summary>
/// Turns a <c>YYYY-MM</c> month string into the half-open day range
/// <c>[start, end)</c> that every user-scoped month query filters on.
/// </summary>
internal static class MonthRange
{
    /// <summary>
    /// Parses a <c>YYYY-MM</c> month into its first day (inclusive) and the first
    /// day of the next month (exclusive). Returns false on any malformed value.
    /// </summary>
    internal static bool TryParse(string month, out DateOnly start, out DateOnly end)
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

    /// <summary>The current UTC month's <c>[start, end)</c> range.</summary>
    internal static (DateOnly Start, DateOnly End) Current()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateOnly(today.Year, today.Month, 1);
        return (start, start.AddMonths(1));
    }
}

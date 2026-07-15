using System.Security.Cryptography;
using System.Text;

namespace Alfred.Modules.Identity;

/// <summary>
/// Registration is gated by a single server-configured invite code until the
/// Households module brings real per-person invites (M2). No configured code
/// means registration is closed.
/// </summary>
public static class RegistrationInviteCode
{
    public static bool Matches(string? configured, string? provided)
    {
        if (string.IsNullOrEmpty(configured) || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        // Constant-time comparison so response timing reveals nothing about the code.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(configured),
            Encoding.UTF8.GetBytes(provided));
    }
}

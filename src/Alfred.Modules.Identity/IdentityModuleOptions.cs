namespace Alfred.Modules.Identity;

/// <param name="RegistrationInviteCode">Null closes registration entirely.</param>
/// <param name="AuthRequestsPerMinute">Requests allowed per client IP, per minute, across /api/auth.</param>
public sealed record IdentityModuleOptions(string? RegistrationInviteCode, int AuthRequestsPerMinute);

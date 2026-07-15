using Alfred.Modules.Identity;

namespace Alfred.Tests;

public class RegistrationInviteCodeTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void No_configured_code_means_registration_closed(string? configured)
    {
        Assert.False(RegistrationInviteCode.Matches(configured, "anything"));
        Assert.False(RegistrationInviteCode.Matches(configured, ""));
        Assert.False(RegistrationInviteCode.Matches(configured, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-code")]
    [InlineData("ALFRED-DEV-INVITE")]
    [InlineData("alfred-dev-invite ")]
    public void Wrong_or_missing_code_is_rejected(string? provided)
    {
        Assert.False(RegistrationInviteCode.Matches("alfred-dev-invite", provided));
    }

    [Fact]
    public void Exact_match_is_accepted()
    {
        Assert.True(RegistrationInviteCode.Matches("alfred-dev-invite", "alfred-dev-invite"));
    }
}

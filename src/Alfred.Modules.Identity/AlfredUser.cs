using Microsoft.AspNetCore.Identity;

namespace Alfred.Modules.Identity;

public class AlfredUser : IdentityUser
{
    public string? DisplayName { get; set; }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Identity;

public class AlfredIdentityDbContext(DbContextOptions<AlfredIdentityDbContext> options)
    : IdentityDbContext<AlfredUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
    }
}

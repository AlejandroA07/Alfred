using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Finance;

public class AlfredFinanceDbContext(DbContextOptions<AlfredFinanceDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("finance");

        modelBuilder.Entity<Category>(category =>
        {
            category.HasKey(c => c.Id);
            category.Property(c => c.UserId).HasMaxLength(450).IsRequired();
            category.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).IsRequired();
            category.Property(c => c.Color).HasMaxLength(7).IsRequired();
            category.Property(c => c.MonthlyBudget).HasColumnType("numeric(12,2)");

            // Every read is scoped by user, and a user's category names are unique.
            category.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
        });
    }
}

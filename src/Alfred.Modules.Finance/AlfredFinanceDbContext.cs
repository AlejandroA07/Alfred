using Microsoft.EntityFrameworkCore;

namespace Alfred.Modules.Finance;

public class AlfredFinanceDbContext(DbContextOptions<AlfredFinanceDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Expense> Expenses => Set<Expense>();

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

        modelBuilder.Entity<Expense>(expense =>
        {
            expense.HasKey(e => e.Id);
            expense.Property(e => e.UserId).HasMaxLength(450).IsRequired();
            expense.Property(e => e.Amount).HasColumnType("numeric(12,2)");
            expense.Property(e => e.Note).HasMaxLength(Expense.NoteMaxLength);

            // Every read is scoped by user and typically to a month; index the
            // access pattern (owner, then most-recent-first by date).
            expense.HasIndex(e => new { e.UserId, e.Date });
        });
    }
}

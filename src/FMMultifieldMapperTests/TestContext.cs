using FMMultifieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

internal class TestContext : DbContext
{
    public DbSet<FmMultifield> Multifields { get; set; }
    public DbSet<FmMultifieldValue> MultifieldValues { get; set; }
    public DbSet<FmTargetTestClassMultifield> FmTargetTestClassMultifields { get; set; }
    public DbSet<FmTargetTestClass> FmTargetTestClasses { get; set; }

    public TestContext(DbContextOptions<TestContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddMultifieldMappings();
        base.OnModelCreating(modelBuilder);
    }
}

internal static class DbContextExtensions
{
    public static void AddMultifieldMappings(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FmMultifield>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(50);
            e.HasIndex(i => i.Name).IsUnique();
        });

        modelBuilder.Entity<FmMultifieldValue>(e =>
        {
            // The comparison might be case insensitive
            // e.Property(p => p.Value).UseCollation("utf8mb4_bin"); // e.g. for case sensitive for MySQL v5.7
            e.Property(p => p.Value).HasMaxLength(50);
            e.HasIndex(i => new { i.FmMultifieldId, i.Value }).IsUnique();
        });
    }

    public static void EnsureMultifieldSets<TContext>(this TContext context) where TContext : DbContext
    {
        if (!context.Model.GetEntityTypes().Any(e => e.ClrType == typeof(FmMultifield)))
        {
            throw new InvalidOperationException("The DbContext does not contain a DbSet<FmMultifield>.");
        }

        if (!context.Model.GetEntityTypes().Any(e => e.ClrType == typeof(FmMultifieldValue)))
        {
            throw new InvalidOperationException("The DbContext does not contain a DbSet<FmMultifieldValue>.");
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Server.Data.Entities;

namespace Server.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<PantryItem> Items => Set<PantryItem>();
    public DbSet<HouseholdMember> Members => Set<HouseholdMember>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PantryItem>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Unit).HasMaxLength(32);
            entity.Property(x => x.Location).HasMaxLength(64);
            entity.Property(x => x.Notes).HasMaxLength(2000);

            entity.HasOne(x => x.HouseholdMember)
                .WithMany()
                .HasForeignKey(x => x.HouseholdMemberId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HouseholdMember>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(100);
            entity.Property(x => x.Color).HasMaxLength(16);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasMaxLength(64);

            entity.HasOne(x => x.PantryItem)
                .WithMany()
                .HasForeignKey(x => x.PantryItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.PantryItemId, x.ExpirationDate, x.Type });
        });

        modelBuilder.Entity<PushSubscription>(entity =>
        {
            entity.Property(x => x.Endpoint).HasMaxLength(2000);
            entity.Property(x => x.P256dh).HasMaxLength(512);
            entity.Property(x => x.Auth).HasMaxLength(512);
            entity.HasIndex(x => x.Endpoint).IsUnique();
        });
    }
}

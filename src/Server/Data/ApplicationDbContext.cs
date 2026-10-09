using Microsoft.EntityFrameworkCore;

using Server.Data.Entities;

namespace Server.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<PantryItem> Items => Set<PantryItem>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Notification>()
            .HasOne<PantryItem>()
            .WithMany()
            .HasForeignKey(n => n.PantryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PushSubscription>()
            .HasIndex(s => s.Endpoint)
            .IsUnique();

        // Attribution only: deleting a member keeps the items.
        builder.Entity<PantryItem>()
            .HasOne<Member>()
            .WithMany()
            .HasForeignKey(i => i.CreatedByMemberId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
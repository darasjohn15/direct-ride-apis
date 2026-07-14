using DirectRide.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectRide.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AvailabilitySlot> AvailabilitySlots => Set<AvailabilitySlot>();
    public DbSet<RideRequest> RideRequests => Set<RideRequest>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RideRequest>()
            .HasOne(r => r.Rider)
            .WithMany(u => u.RiderRideRequests)
            .HasForeignKey(r => r.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RideRequest>()
            .HasOne(r => r.Driver)
            .WithMany(u => u.DriverRideRequests)
            .HasForeignKey(r => r.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RideRequest>()
            .HasOne(r => r.AvailabilitySlot)
            .WithOne(a => a.RideRequest)
            .HasForeignKey<RideRequest>(r => r.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Ride)
            .WithMany(r => r.Notifications)
            .HasForeignKey(n => n.RideId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .Property(n => n.NotificationType)
            .HasConversion<string>()
            .HasMaxLength(50);

        modelBuilder.Entity<Notification>()
            .Property(n => n.Title)
            .HasMaxLength(150);

        modelBuilder.Entity<Notification>()
            .Property(n => n.IsRead)
            .HasDefaultValue(false);

        modelBuilder.Entity<Notification>()
            .Property(n => n.CreatedAt)
            .HasDefaultValueSql("now()");

        modelBuilder.Entity<User>()
            .Property(u => u.BaseFare)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<RideRequest>()
            .Property(r => r.FareAmount)
            .HasColumnType("decimal(10,2)");

        modelBuilder.Entity<RideRequest>()
            .Property(r => r.DriverEarningsAmount)
            .HasColumnType("decimal(10,2)");
    }
}

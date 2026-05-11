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

        modelBuilder.Entity<User>()
            .Property(u => u.BaseFare)
            .HasColumnType("decimal(10,2)");
    }
}
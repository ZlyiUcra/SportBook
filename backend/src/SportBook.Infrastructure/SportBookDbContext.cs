using Microsoft.EntityFrameworkCore;
using SportBook.Domain.Entities;

namespace SportBook.Infrastructure;

/// <summary>
/// EF Core context for SportBook. Entity configuration lives here rather than in Domain so
/// engine-specific mapping (decimal precision, indexes) stays isolated to Infrastructure per
/// plan.md Project Structure.
/// </summary>
public class SportBookDbContext(DbContextOptions<SportBookDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Venue> Venues => Set<Venue>();

    public DbSet<Court> Courts => Set<Court>();

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId);
        });

        modelBuilder.Entity<City>(entity =>
        {
            // Natural key = GeoNames geonameid (data-model.md) - no IDENTITY, so the seed
            // migration can insert explicit, stable IDs instead of letting SQL Server assign them.
            entity.Property(c => c.Id).ValueGeneratedNever();
            entity.Property(c => c.Latitude).HasPrecision(9, 6);
            entity.Property(c => c.Longitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasOne(v => v.Owner).WithMany().HasForeignKey(v => v.OwnerId);
            // Restrict, not Cascade: Cities are read-only reference data (never deleted by
            // application code), but a Cascade default would silently wipe every venue of a city
            // if a City row were ever removed by hand - Restrict fails loudly instead.
            entity.HasOne(v => v.City).WithMany().HasForeignKey(v => v.CityId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(v => v.Latitude).HasPrecision(9, 6);
            entity.Property(v => v.Longitude).HasPrecision(9, 6);
        });

        modelBuilder.Entity<Court>(entity =>
        {
            entity.Property(c => c.PricePerHour).HasPrecision(18, 2);
            entity.HasOne(c => c.Venue).WithMany(v => v.Courts).HasForeignKey(c => c.VenueId);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.TotalPrice).HasPrecision(18, 2);
            entity.HasOne(b => b.Court).WithMany(c => c.Bookings).HasForeignKey(b => b.CourtId);
            // Restrict, not Cascade: Booking is also reachable from User via
            // Venue -> Court -> Booking, and SQL Server rejects multiple cascade paths to the
            // same table. Deleting a user with existing bookings must fail explicitly, not
            // silently wipe booking history (no such requirement exists in spec.md).
            entity.HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);
            // Supports the overlap check in BookingService.Create (serializable transaction over
            // rows for this court) and availability lookups by date range.
            entity.HasIndex(b => new { b.CourtId, b.StartTime, b.EndTime });
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasOne(r => r.Venue).WithMany(v => v.Reviews).HasForeignKey(r => r.VenueId);
            // Restrict for the same multiple-cascade-paths reason as Booking.User above
            // (Review is also reachable from User via Venue -> Review).
            entity.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            // Enforces "at most one review per user per venue" (data-model.md Review) -
            // BookingService.Create-or-replace upserts against this key.
            entity.HasIndex(r => new { r.VenueId, r.UserId }).IsUnique();
        });
    }
}

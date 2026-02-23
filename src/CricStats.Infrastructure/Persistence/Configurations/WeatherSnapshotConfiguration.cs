using CricStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CricStats.Infrastructure.Persistence.Configurations;

public sealed class WeatherSnapshotConfiguration : IEntityTypeConfiguration<WeatherSnapshot>
{
    public void Configure(EntityTypeBuilder<WeatherSnapshot> builder)
    {
        builder.ToTable("WeatherSnapshots");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TimestampUtc)
            .IsRequired();

        builder.Property(x => x.Temperature)
            .HasPrecision(5, 2);

        builder.Property(x => x.Humidity)
            .HasPrecision(5, 2);

        builder.Property(x => x.WindSpeed)
            .HasPrecision(5, 2);

        builder.Property(x => x.PrecipProbability)
            .HasPrecision(5, 2);

        builder.Property(x => x.PrecipAmount)
            .HasPrecision(7, 2);

        builder.Property(x => x.SourceProvider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastSyncedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Venue)
            .WithMany(x => x.WeatherSnapshots)
            .HasForeignKey(x => x.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.VenueId, x.TimestampUtc });
        builder.HasIndex(x => new { x.SourceProvider, x.ExternalId }).IsUnique();
    }
}

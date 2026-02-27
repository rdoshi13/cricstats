using CricStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CricStats.Infrastructure.Persistence.Configurations;

public sealed class SeriesMatchConfiguration : IEntityTypeConfiguration<SeriesMatch>
{
    public void Configure(EntityTypeBuilder<SeriesMatch> builder)
    {
        builder.ToTable("SeriesMatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.Format)
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.StatusText)
            .HasMaxLength(200);

        builder.Property(x => x.SourceProvider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastSyncedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Series)
            .WithMany(x => x.Matches)
            .HasForeignKey(x => x.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.StartTimeUtc);
        builder.HasIndex(x => new { x.SeriesId, x.SourceProvider, x.ExternalId }).IsUnique();
    }
}

using CricStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CricStats.Infrastructure.Persistence.Configurations;

public sealed class InningsScoreConfiguration : IEntityTypeConfiguration<InningsScore>
{
    public void Configure(EntityTypeBuilder<InningsScore> builder)
    {
        builder.ToTable("InningsScores");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InningsNo)
            .IsRequired();

        builder.Property(x => x.Runs)
            .IsRequired();

        builder.Property(x => x.Wickets)
            .IsRequired();

        builder.Property(x => x.Overs)
            .HasPrecision(5, 2);

        builder.Property(x => x.SourceProvider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastSyncedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Match)
            .WithMany(x => x.InningsScores)
            .HasForeignKey(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Team)
            .WithMany(x => x.InningsScores)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MatchId, x.TeamId, x.InningsNo }).IsUnique();
        builder.HasIndex(x => new { x.SourceProvider, x.ExternalId }).IsUnique();
    }
}

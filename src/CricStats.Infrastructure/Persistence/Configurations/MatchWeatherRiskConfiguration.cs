using CricStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CricStats.Infrastructure.Persistence.Configurations;

public sealed class MatchWeatherRiskConfiguration : IEntityTypeConfiguration<MatchWeatherRisk>
{
    public void Configure(EntityTypeBuilder<MatchWeatherRisk> builder)
    {
        builder.ToTable("MatchWeatherRisks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompositeRiskScore)
            .HasPrecision(5, 2);

        builder.Property(x => x.RiskLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.ComputedAtUtc)
            .IsRequired();

        builder.HasOne(x => x.Match)
            .WithOne(x => x.MatchWeatherRisk)
            .HasForeignKey<MatchWeatherRisk>(x => x.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MatchId).IsUnique();
    }
}

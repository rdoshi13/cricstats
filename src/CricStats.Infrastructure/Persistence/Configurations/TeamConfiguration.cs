using CricStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CricStats.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ShortName)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.SourceProvider)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastSyncedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.Country);
        builder.HasIndex(x => new { x.SourceProvider, x.ExternalId }).IsUnique();
    }
}

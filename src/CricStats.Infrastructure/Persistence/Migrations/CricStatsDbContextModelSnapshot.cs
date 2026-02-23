using CricStats.Domain.Entities;
using CricStats.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace CricStats.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CricStatsDbContext))]
public partial class CricStatsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity<Team>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("Country")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("ExternalId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<DateTimeOffset>("LastSyncedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("ShortName")
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)");

            b.Property<string>("SourceProvider")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.HasKey("Id");

            b.HasIndex("Country");

            b.HasIndex("SourceProvider", "ExternalId")
                .IsUnique();

            b.ToTable("Teams");
        });

        modelBuilder.Entity<Venue>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("City")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("Country")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<string>("ExternalId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<DateTimeOffset>("LastSyncedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<decimal>("Latitude")
                .HasPrecision(9, 6)
                .HasColumnType("numeric(9,6)");

            b.Property<decimal>("Longitude")
                .HasPrecision(9, 6)
                .HasColumnType("numeric(9,6)");

            b.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            b.Property<string>("SourceProvider")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.HasKey("Id");

            b.HasIndex("SourceProvider", "ExternalId")
                .IsUnique();

            b.ToTable("Venues");
        });

        modelBuilder.Entity<Match>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<Guid>("AwayTeamId")
                .HasColumnType("uuid");

            b.Property<string>("ExternalId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<MatchFormat>("Format")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(10)
                .HasColumnType("character varying(10)");

            b.Property<Guid>("HomeTeamId")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("LastSyncedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("SourceProvider")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<DateTimeOffset>("StartTimeUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<MatchStatus>("Status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)");

            b.Property<Guid>("VenueId")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("AwayTeamId");

            b.HasIndex("Format", "StartTimeUtc");

            b.HasIndex("HomeTeamId");

            b.HasIndex("SourceProvider", "ExternalId")
                .IsUnique();

            b.HasIndex("StartTimeUtc");

            b.HasIndex("VenueId");

            b.ToTable("Matches");
        });

        modelBuilder.Entity<InningsScore>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("ExternalId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<int>("InningsNo")
                .HasColumnType("integer");

            b.Property<DateTimeOffset>("LastSyncedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("MatchId")
                .HasColumnType("uuid");

            b.Property<decimal>("Overs")
                .HasPrecision(5, 2)
                .HasColumnType("numeric(5,2)");

            b.Property<int>("Runs")
                .HasColumnType("integer");

            b.Property<string>("SourceProvider")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<Guid>("TeamId")
                .HasColumnType("uuid");

            b.Property<int>("Wickets")
                .HasColumnType("integer");

            b.HasKey("Id");

            b.HasIndex("MatchId", "TeamId", "InningsNo")
                .IsUnique();

            b.HasIndex("SourceProvider", "ExternalId")
                .IsUnique();

            b.HasIndex("TeamId");

            b.ToTable("InningsScores");
        });

        modelBuilder.Entity<WeatherSnapshot>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<string>("ExternalId")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<decimal>("Humidity")
                .HasPrecision(5, 2)
                .HasColumnType("numeric(5,2)");

            b.Property<DateTimeOffset>("LastSyncedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<decimal>("PrecipAmount")
                .HasPrecision(7, 2)
                .HasColumnType("numeric(7,2)");

            b.Property<decimal>("PrecipProbability")
                .HasPrecision(5, 2)
                .HasColumnType("numeric(5,2)");

            b.Property<string>("SourceProvider")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)");

            b.Property<decimal>("Temperature")
                .HasPrecision(5, 2)
                .HasColumnType("numeric(5,2)");

            b.Property<DateTimeOffset>("TimestampUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("VenueId")
                .HasColumnType("uuid");

            b.Property<decimal>("WindSpeed")
                .HasPrecision(5, 2)
                .HasColumnType("numeric(5,2)");

            b.HasKey("Id");

            b.HasIndex("SourceProvider", "ExternalId")
                .IsUnique();

            b.HasIndex("VenueId", "TimestampUtc");

            b.ToTable("WeatherSnapshots");
        });

        modelBuilder.Entity<MatchWeatherRisk>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnType("uuid");

            b.Property<decimal>("CompositeRiskScore")
                .HasPrecision(5, 2)
                .HasColumnType("numeric(5,2)");

            b.Property<DateTimeOffset>("ComputedAtUtc")
                .HasColumnType("timestamp with time zone");

            b.Property<Guid>("MatchId")
                .HasColumnType("uuid");

            b.Property<RiskLevel>("RiskLevel")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnType("character varying(20)");

            b.HasKey("Id");

            b.HasIndex("MatchId")
                .IsUnique();

            b.ToTable("MatchWeatherRisks");
        });

        modelBuilder.Entity<Match>(b =>
        {
            b.HasOne(x => x.AwayTeam)
                .WithMany(x => x.AwayMatches)
                .HasForeignKey(x => x.AwayTeamId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne(x => x.HomeTeam)
                .WithMany(x => x.HomeMatches)
                .HasForeignKey(x => x.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne(x => x.Venue)
                .WithMany(x => x.Matches)
                .HasForeignKey(x => x.VenueId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity<InningsScore>(b =>
        {
            b.HasOne(x => x.Match)
                .WithMany(x => x.InningsScores)
                .HasForeignKey(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne(x => x.Team)
                .WithMany(x => x.InningsScores)
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity<WeatherSnapshot>(b =>
        {
            b.HasOne(x => x.Venue)
                .WithMany(x => x.WeatherSnapshots)
                .HasForeignKey(x => x.VenueId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity<MatchWeatherRisk>(b =>
        {
            b.HasOne(x => x.Match)
                .WithOne(x => x.MatchWeatherRisk)
                .HasForeignKey<MatchWeatherRisk>(x => x.MatchId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}

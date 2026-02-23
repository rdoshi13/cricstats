using CricStats.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CricStats.Infrastructure.Persistence;

public sealed class CricStatsDbContext : DbContext
{
    public CricStatsDbContext(DbContextOptions<CricStatsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<InningsScore> InningsScores => Set<InningsScore>();
    public DbSet<WeatherSnapshot> WeatherSnapshots => Set<WeatherSnapshot>();
    public DbSet<MatchWeatherRisk> MatchWeatherRisks => Set<MatchWeatherRisk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CricStatsDbContext).Assembly);
    }
}

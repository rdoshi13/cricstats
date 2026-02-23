using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CricStats.Infrastructure.Persistence;

public sealed class CricStatsDbContextFactory : IDesignTimeDbContextFactory<CricStatsDbContext>
{
    public CricStatsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CricStatsDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=cricstats;Username=cricstats;Password=cricstats");

        return new CricStatsDbContext(optionsBuilder.Options);
    }
}

namespace CricStats.Domain.Entities;

public sealed class WeatherSnapshot
{
    public Guid Id { get; set; }
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public DateTimeOffset TimestampUtc { get; set; }
    public decimal Temperature { get; set; }
    public decimal Humidity { get; set; }
    public decimal WindSpeed { get; set; }
    public decimal PrecipProbability { get; set; }
    public decimal PrecipAmount { get; set; }
    public string SourceProvider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public DateTimeOffset LastSyncedAtUtc { get; set; }
}

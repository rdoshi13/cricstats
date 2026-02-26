namespace CricStats.Application.Models.Providers;

public sealed record ProviderVenue(
    string ExternalId,
    string Name,
    string City,
    string Country,
    decimal Latitude,
    decimal Longitude);

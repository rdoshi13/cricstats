type WeatherRiskSummary = {
  compositeRiskScore: number;
  riskLevel: string;
};

type UpcomingMatchItem = {
  matchId: string;
  format: string;
  startTimeUtc: string;
  venueName: string;
  venueCountry: string;
  homeTeamName: string;
  awayTeamName: string;
  status: string;
  weatherRisk: WeatherRiskSummary;
};

type UpcomingMatchesResponse = {
  matches: UpcomingMatchItem[];
  totalCount: number;
};

type SeriesUpcomingMatchItem = {
  seriesMatchId: string;
  externalId: string;
  name: string;
  venueName: string;
  venueCountry: string;
  format: string | null;
  startTimeUtc: string | null;
  status: string | null;
  statusText: string;
};

type UpcomingSeriesItem = {
  seriesId: string;
  externalId: string;
  name: string;
  startDateUtc: string | null;
  endDateUtc: string | null;
  sourceProvider: string;
  matches: SeriesUpcomingMatchItem[];
};

type UpcomingSeriesResponse = {
  series: UpcomingSeriesItem[];
  totalCount: number;
};

type ApiResult<T> = {
  data: T | null;
  error: string | null;
};

const apiBaseUrl = (process.env.API_BASE_URL ?? "http://localhost:5000").replace(/\/$/, "");

async function getApiData<T>(path: string): Promise<ApiResult<T>> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, { cache: "no-store" });
    if (!response.ok) {
      return { data: null, error: `${path} returned ${response.status}` };
    }

    const data = (await response.json()) as T;
    return { data, error: null };
  } catch (error) {
    const message = error instanceof Error ? error.message : "Unexpected API error";
    return { data: null, error: `${path} failed: ${message}` };
  }
}

function formatUtcDateTime(value: string | null): string {
  if (!value) {
    return "TBD";
  }

  const date = new Date(value);
  return new Intl.DateTimeFormat("en-US", {
    dateStyle: "medium",
    timeStyle: "short",
    timeZone: "UTC"
  }).format(date);
}

function riskClassName(riskLevel: string): string {
  const normalized = riskLevel.trim().toLowerCase();
  if (normalized === "high") {
    return "pill-high";
  }
  if (normalized === "medium") {
    return "pill-medium";
  }
  return "pill-low";
}

export default async function HomePage() {
  const [matchesResult, seriesResult] = await Promise.all([
    getApiData<UpcomingMatchesResponse>("/api/v1/matches/upcoming"),
    getApiData<UpcomingSeriesResponse>("/api/v1/series/upcoming")
  ]);

  const matches = matchesResult.data?.matches ?? [];
  const series = seriesResult.data?.series ?? [];

  return (
    <main className="page">
      <section className="hero">
        <p className="eyebrow">CricStats Dashboard</p>
        <h1>Live Match + Series Board</h1>
        <p>
          Data source: <code>{apiBaseUrl}</code>
        </p>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2>Upcoming Matches</h2>
          <span>{matchesResult.data?.totalCount ?? 0}</span>
        </div>
        {matchesResult.error && <p className="error">{matchesResult.error}</p>}
        {matches.length === 0 && !matchesResult.error && <p>No matches returned.</p>}
        <div className="card-grid">
          {matches.map((match) => (
            <article key={match.matchId} className="card">
              <p className="meta">
                {match.format} • {match.status}
              </p>
              <h3>
                {match.homeTeamName} vs {match.awayTeamName}
              </h3>
              <p>{formatUtcDateTime(match.startTimeUtc)} (UTC)</p>
              <p>
                Stadium: <strong>{match.venueName}</strong> ({match.venueCountry})
              </p>
              <p className="risk-row">
                Weather:
                <span className={`pill ${riskClassName(match.weatherRisk.riskLevel)}`}>
                  {match.weatherRisk.riskLevel} ({match.weatherRisk.compositeRiskScore.toFixed(2)})
                </span>
              </p>
            </article>
          ))}
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2>Upcoming Series</h2>
          <span>{seriesResult.data?.totalCount ?? 0}</span>
        </div>
        {seriesResult.error && <p className="error">{seriesResult.error}</p>}
        {series.length === 0 && !seriesResult.error && <p>No series returned.</p>}
        <div className="series-stack">
          {series.map((entry) => (
            <article key={entry.seriesId} className="series-card">
              <div className="series-header">
                <h3>{entry.name}</h3>
                <p>{entry.sourceProvider}</p>
              </div>
              <p>
                {formatUtcDateTime(entry.startDateUtc)} to {formatUtcDateTime(entry.endDateUtc)}
              </p>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Match</th>
                      <th>Format</th>
                      <th>Start (UTC)</th>
                      <th>Stadium</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {entry.matches.map((match) => (
                      <tr key={match.seriesMatchId}>
                        <td>{match.name}</td>
                        <td>{match.format ?? "NA"}</td>
                        <td>{formatUtcDateTime(match.startTimeUtc)}</td>
                        <td>
                          {match.venueName} ({match.venueCountry})
                        </td>
                        <td>{match.statusText}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}

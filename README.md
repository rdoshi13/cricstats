# CricStats

CricStats is a production-grade cricket analytics platform built with:

- ASP.NET Core (.NET 8) backend
- PostgreSQL database
- Hangfire for background jobs
- Pluggable cricket data providers
- Weather API integration
- Next.js frontend dashboard (in progress)
- Composite Rain Risk scoring
- Future AI/ML integration support

## Quick Commands

Assume you are in the repository root (`cricstats/`) unless noted.

Start Postgres (Docker):

```bash
docker compose up -d postgres
```

Run backend API:

```bash
dotnet run --project src/CricStats.Api/CricStats.Api.csproj
```

Run frontend dashboard:

```bash
cd frontend
npm install
echo 'API_BASE_URL=http://localhost:5000' > .env.local
npm run dev
```

Stop frontend/backend:
- Press `Ctrl+C` in each running terminal.

Stop Docker Postgres:

```bash
docker compose stop postgres
```

End local Docker stack completely:

```bash
docker compose down
```

---

# Current Status (as of February 27, 2026)

Implemented now:

- Clean Architecture .NET 8 backend scaffold in `src/`
- Domain model + EF Core `DbContext` + initial migration
- PostgreSQL local setup via Docker Compose
- Pluggable provider integration with priority/fallback (`CricketDataOrg`, `CricbuzzLive`)
- Normalization + upsert pipeline for teams, venues, and matches
- `GET /api/v1/matches/upcoming` backed by database with filter support (`country`, `format`, `from`, `to`)
- `POST /api/v1/admin/sync/upcoming` to trigger fixture sync
- Live match provider integration (`CricketDataOrg`, `CricbuzzLive`)
- Live weather provider integration (Open-Meteo, no synthetic fallback data)
- Persisted weather snapshots per venue/match window
- Composite weather risk computation + storage in `MatchWeatherRisk`
- `GET /api/v1/matches/{id}/weather-risk` for score + breakdown
- `POST /api/v1/admin/weather/refresh` to recompute upcoming match weather risk
- Hangfire configured with PostgreSQL storage + recurring jobs for fixture sync and weather refresh
- Series catalog persistence (`Series`, `SeriesMatches`) with provider ingestion
- `GET /api/v1/series/upcoming` backed by database
- `POST /api/v1/admin/sync/series` to trigger series catalog sync
- Unit tests + integration smoke tests
- Next.js frontend scaffold in `frontend/` with dashboard for upcoming matches and series

Current behavior:

- The project is API-first with a minimal local website UI (`frontend/`) for dashboard views
- Upcoming endpoint reads from Postgres; if empty, it triggers provider sync and then returns data
- The runtime path no longer uses deterministic local fixture stubs
- Provider priority is configurable in `src/CricStats.Api/appsettings.json` under `CricketProviders`
- Live match provider settings are configurable in `src/CricStats.Api/appsettings.json` under `LiveCricket`
- Weather risk settings are configurable in `src/CricStats.Api/appsettings.json` under `WeatherRisk`
- Hangfire settings are configurable in `src/CricStats.Api/appsettings.json` under `Hangfire`
- Hangfire dashboard is available locally at `http://localhost:5000/hangfire` in Development
- Swagger is available locally at `http://localhost:5000/swagger` while the API runs in Development

Not implemented yet:

- Production-grade provider hardening (rate limiting, caching, richer venue/team geo normalization)
- Hangfire operational hardening (dashboard auth, retries/backoff policies, alerting)
- Historical ingestion/analytics endpoints (partial: series catalog sync added)
- Advanced frontend pages and user interactions beyond the initial dashboard

---

# Product Vision

CricStats provides:

- Global upcoming fixtures (all formats)
- Country-based filtering
- Venue weather forecasts
- Composite rain risk scoring
- Historical venue statistics
- Pluggable data providers
- Clean, scalable architecture

Primary differentiator:
Predictive composite weather risk for upcoming cricket matches.

---

# Architecture Overview

## Backend (.NET 8)

Clean Architecture structure:

src/
CricStats.Api
CricStats.Application
CricStats.Domain
CricStats.Infrastructure
CricStats.Contracts

Core responsibilities:

- Normalize data from multiple cricket APIs
- Persist fixtures, venues, teams, historical data
- Compute composite weather risk
- Expose stable REST API
- Schedule ingestion via Hangfire

---

# Domain Model (Initial)

Entities:

Team

- Id
- Name
- Country
- ShortName

Venue

- Id
- Name
- City
- Country
- Latitude
- Longitude

Match

- Id
- Format (T20 / ODI / Test)
- StartTimeUtc
- VenueId
- HomeTeamId
- AwayTeamId
- Status

InningsScore (MVP level only)

- Id
- MatchId
- TeamId
- InningsNo
- Runs
- Wickets
- Overs

WeatherSnapshot

- Id
- VenueId
- TimestampUtc
- Temperature
- Humidity
- WindSpeed
- PrecipProbability
- PrecipAmount

MatchWeatherRisk

- Id
- MatchId
- CompositeRiskScore
- RiskLevel (Low / Medium / High)
- ComputedAtUtc

---

# Composite Weather Risk (MVP Logic)

For each upcoming match:

1. Fetch weather forecast for match window (-2h to +6h).
2. Calculate weighted score:

Example formula (configurable):

CompositeScore =
(PrecipProbability _ 0.5) +
(PrecipAmountNormalized _ 0.3) +
(HumidityNormalized _ 0.1) +
(WindSpeedNormalized _ 0.1)

3. Map score to levels:

0–33 => Low
34–66 => Medium
67–100 => High

Store results in MatchWeatherRisk table.

---

# Pluggable Cricket Providers

Define interface:

ICricketProvider

- GetUpcomingMatchesAsync(range)
- GetMatchDetailsAsync(matchId)
- GetVenuesAsync()
- GetHistoricalMatchesAsync(filters)

Each provider must:

- Map external DTOs to internal domain models
- Expose external IDs
- Be registered via DI

Provider priority order configurable in appsettings:

CricketProviders:Priority = [
"CricketDataOrg",
"CricbuzzLive"
]

If provider A fails or lacks data, fallback to next.

All ingested data must include:

- SourceProvider
- ExternalId
- LastSyncedAtUtc

---

# Hangfire Jobs

Use Hangfire with PostgreSQL storage.

Recurring Jobs:

1. SyncUpcomingMatchesJob
   - Every 6 hours
   - Fetch fixtures across all formats
   - Upsert matches + venues

2. SyncUpcomingSeriesJob
   - Daily (00:00 UTC)
   - Fetch upcoming series list
   - Fetch per-series `series_info`
   - Upsert series + series matches

3. RefreshWeatherRiskJob
   - Every 3 hours
   - Recompute composite risk for upcoming matches

Jobs must be idempotent.

---

# API Endpoints (v1)

GET /api/v1/matches/upcoming

- Query params:
  - country
  - format
  - from
  - to
- Returns fixtures + risk summary

GET /api/v1/matches/{id}

- Match details + innings score

GET /api/v1/matches/{id}/weather-risk

- Composite score + breakdown

GET /api/v1/venues/{id}

- Venue details + historical averages

GET /api/v1/venues/{id}/historical

- Avg 1st innings score
- Win toss impact (future)

GET /api/v1/series/upcoming

- Upcoming series catalog from DB

GET /api/v1/series/{id}

- Series details from DB
- Query params:
  - page (default 1)
  - pageSize (default 20, max 100)

Admin endpoints (protected):
POST /api/v1/admin/sync/upcoming
POST /api/v1/admin/sync/series
POST /api/v1/admin/sync/historical
POST /api/v1/admin/weather/refresh

---

# Frontend (Next.js)

Pages:

/ (Upcoming Matches)

- Global fixtures
- Country filter
- Format filter
- Rain Risk badge

/match/[id]

- Match details
- Weather breakdown chart
- Risk explanation

/venue/[id]

- Historical averages
- Country metadata

Data fetching:

- React Query or SWR
- Poll upcoming matches every 5–10 minutes

---

# Milestones

## Milestone 1 – Core Backend Setup (Completed)

- Scaffold solution
- Domain entities
- EF Core + migrations
- Docker Compose (Postgres)
- Basic upcoming endpoint (stubbed in M1, replaced with DB-backed provider sync in M2)

## Milestone 2 – Provider Integration (Completed)

- Implement 2 providers
- Normalize + upsert fixtures
- Country filtering support

## Milestone 3 – Weather Integration (Completed)

- Implement WeatherClient
- Compute Composite Risk
- Store MatchWeatherRisk
- Expose weather endpoints

## Milestone 4 – Hangfire Jobs (In Progress)

- Configure Hangfire with PostgreSQL storage
- Implement recurring jobs:
  - `sync-upcoming-matches` (`0 */6 * * *`, UTC)
  - `refresh-weather-risk` (`0 */3 * * *`, UTC)
  - `sync-upcoming-series` (`0 0 * * *`, UTC)
- Expose Hangfire dashboard at `/hangfire` in Development

## Milestone 5 – Historical Data (In Progress)

- Ingest series catalog (`series` + `series_info`) and persist in DB
- Add series browsing endpoint (`/api/v1/series/upcoming`)
- Ingest historical matches
- Compute venue averages
- Venue historical endpoints

## Milestone 6 – Frontend (In Progress)

- Next.js scaffold
- Dashboard page for `/api/v1/matches/upcoming`
- Dashboard page for `/api/v1/series/upcoming`
- Venue/stadium display for matches and series matches
- Country filters
- Match detail page

---

# Future Phase (ML Integration)

- Separate Python FastAPI service
- Predict match outcome
- Predict player performance
- Store predictions in database
- Serve via /api/v1/matches/{id}/prediction

---

# Development Requirements

- .NET 8 SDK
- Docker
- PostgreSQL
- `dotnet-ef` CLI tool (EF migrations): `dotnet tool install --global dotnet-ef --version 8.*`
- CricketData/CricAPI key (`CricketDataOrgApi`)
- Node 20+ (required for frontend in `frontend/`)

---

# Milestone 1 Delivery Summary (Completed)

Completed deliverables:

1. Scaffolded solution and project structure.
2. Implemented Domain layer + `CricStatsDbContext`.
3. Added initial EF migration + Docker Compose for PostgreSQL.
4. Implemented initial `GET /api/v1/matches/upcoming` with deterministic stub data (superseded by live provider sync in Milestone 2).
5. Added unit and integration test coverage for Milestone 1 scope.

# Milestone 2 Delivery Summary (Completed)

Completed deliverables:

1. Implemented two live pluggable providers (`CricketDataOrg`, `CricbuzzLive`).
2. Added provider priority/fallback orchestration (`CricketProviders:Priority`).
3. Added normalization + upsert flow to persist teams, venues, and matches.
4. Switched upcoming matches endpoint to database-backed queries with country/format/date filters.
5. Added admin sync endpoint: `POST /api/v1/admin/sync/upcoming`.

# Milestone 3 Delivery Summary (Completed)

Completed deliverables:

1. Added weather provider abstraction and Open-Meteo live implementation.
2. Added weather snapshot ingestion for match windows (`-2h` to `+6h`).
3. Implemented composite weather risk calculator and risk-level mapping.
4. Persisted and exposed `MatchWeatherRisk` for each upcoming match.
5. Added weather endpoints:
   - `GET /api/v1/matches/{id}/weather-risk`
   - `POST /api/v1/admin/weather/refresh`

# Milestone 4 Delivery Summary (In Progress)

Completed so far:

1. Added Hangfire server + PostgreSQL storage wiring in API startup.
2. Registered recurring jobs for upcoming sync and weather-risk refresh.
3. Added daily recurring series sync job and config (`sync-upcoming-series`).
4. Added Hangfire configuration section in appsettings and dashboard in Development.

# Milestone 5 Delivery Summary (In Progress)

Completed so far:

1. Added `Series` and `SeriesMatches` schema + migration.
2. Added provider ingestion path for upcoming series and per-series details.
3. Added series sync service + endpoints:
   - `POST /api/v1/admin/sync/series`
   - `GET /api/v1/series/upcoming`
4. Wired Hangfire daily series catalog sync at 00:00 UTC.

Next implementation focus:

1. Add dashboard authentication and production-safe access controls.
2. Add job-level retry/backoff and operational logging/metrics.
3. Extend recurring orchestration as historical ingestion is introduced in Milestone 5.

---

# Milestone 1 Local Setup and Run

## 1. Start PostgreSQL

```bash
docker compose up -d postgres
```

## 2. Apply EF Core migration

```bash
dotnet ef database update \
  --project src/CricStats.Infrastructure/CricStats.Infrastructure.csproj \
  --startup-project src/CricStats.Api/CricStats.Api.csproj
```

If `dotnet ef` is not found:

```bash
dotnet tool install --global dotnet-ef --version 8.*
export PATH="$PATH:$HOME/.dotnet/tools"
```

To avoid exporting PATH every session, add this once to `~/.bash_profile`:

```bash
echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bash_profile
source ~/.bash_profile
```

## 3. Run API

```bash
dotnet run --project src/CricStats.Api/CricStats.Api.csproj
```

## 4. Call upcoming matches endpoint

```bash
curl "http://localhost:5000/api/v1/matches/upcoming?format=T20"
```

Optional manual sync trigger:

```bash
curl -X POST "http://localhost:5000/api/v1/admin/sync/upcoming"
curl -X POST "http://localhost:5000/api/v1/admin/sync/series"
```

Optional series query:

```bash
curl "http://localhost:5000/api/v1/series/upcoming"
```

Live provider notes:

- Upcoming fixture sync uses `CricketDataOrg` first by default (`CricketProviders:Priority`).
- Configure your CricketData/CricAPI key under `CricketDataOrgApi:ApiKey` in `appsettings.Development.json`
  or via env var `CricketDataOrgApi__ApiKey`.
- Persist API key across sessions by adding this once to `~/.bash_profile`:
  `echo 'export CricketDataOrgApi__ApiKey="YOUR_KEY"' >> ~/.bash_profile`
- Prefer storing secrets outside committed config using user-secrets:
  `dotnet user-secrets --project src/CricStats.Api/CricStats.Api.csproj init`
  then:
  `dotnet user-secrets --project src/CricStats.Api/CricStats.Api.csproj set "CricketDataOrgApi:ApiKey" "YOUR_KEY"`
- Set `LiveCricket:Enabled=false` in `src/CricStats.Api/appsettings.Development.json` to disable Cricbuzz fallback.
- Weather lookup uses Open-Meteo only; if it fails, no synthetic weather points are generated.
- Hangfire recurring jobs run in UTC with defaults:
  - `sync-upcoming-matches`: every 6 hours
  - `refresh-weather-risk`: every 3 hours
  - `sync-upcoming-series`: every day at 00:00 UTC
- You can change schedules via:
  - `Hangfire:Jobs:SyncUpcomingMatchesCron`
  - `Hangfire:Jobs:RefreshWeatherRiskCron`
  - `Hangfire:Jobs:SyncUpcomingSeriesCron`
- Open dashboard in Development: `http://localhost:5000/hangfire`

Optional weather refresh and weather-risk lookup:

```bash
curl -X POST "http://localhost:5000/api/v1/admin/weather/refresh"
curl "http://localhost:5000/api/v1/matches/upcoming"
# Use a matchId from the response below:
curl "http://localhost:5000/api/v1/matches/{matchId}/weather-risk"
```

## 5. Build and test

```bash
dotnet build CricStats.sln
dotnet test CricStats.sln
```

## 6. Run frontend dashboard

```bash
cd frontend
npm install
echo 'API_BASE_URL=http://localhost:5000' > .env.local
npm run dev
```

Open `http://localhost:3000`.

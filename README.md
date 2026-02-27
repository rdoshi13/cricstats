# CricStats

CricStats is a production-grade cricket analytics platform built with:

- ASP.NET Core (.NET 8) backend
- PostgreSQL database
- Hangfire for background jobs
- Pluggable cricket data providers
- Weather API integration
- Next.js frontend (planned)
- Composite Rain Risk scoring
- Future AI/ML integration support

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
- Unit tests + integration smoke tests
- `frontend/README.md` placeholder (no Next.js app scaffold yet)

Current behavior:

- The project is API-first at this stage (no website UI yet)
- Upcoming endpoint reads from Postgres; if empty, it triggers provider sync and then returns data
- The runtime path no longer uses deterministic local fixture stubs
- Provider priority is configurable in `src/CricStats.Api/appsettings.json` under `CricketProviders`
- Live match provider settings are configurable in `src/CricStats.Api/appsettings.json` under `LiveCricket`
- Weather risk settings are configurable in `src/CricStats.Api/appsettings.json` under `WeatherRisk`
- Swagger is available locally at `http://localhost:5000/swagger` while the API runs in Development

Not implemented yet:

- Production-grade provider hardening (rate limiting, caching, richer venue/team geo normalization)
- Hangfire recurring jobs
- Historical ingestion/analytics endpoints
- Next.js frontend pages

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

2. SyncHistoricalMatchesJob
   - Daily
   - Ingest historical matches

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

Admin endpoints (protected):
POST /api/v1/admin/sync/upcoming
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

## Milestone 4 – Hangfire Jobs (Planned)

- Configure Hangfire
- Implement recurring ingestion jobs
- Add Hangfire dashboard

## Milestone 5 – Historical Data (Planned)

- Ingest historical matches
- Compute venue averages
- Venue historical endpoints

## Milestone 6 – Frontend (Planned)

- Next.js scaffold
- Upcoming matches page
- Country filters
- Weather risk badges
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
- Node 20+ (only needed once frontend work starts)

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

Next implementation focus:

1. Milestone 4: Hangfire recurring jobs for fixture sync and weather risk refresh.
2. Schedule idempotent background ingestion without changing API contracts.
3. Add dashboard/operational visibility for recurring jobs.

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

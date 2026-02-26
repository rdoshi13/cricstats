# CricStats

CricStats is a production-grade cricket analytics platform built with:

- ASP.NET Core (.NET 8) backend
- PostgreSQL database
- Hangfire for background jobs
- Pluggable cricket data providers
- Weather API integration
- Next.js frontend
- Composite Rain Risk scoring
- Future AI/ML integration support

---

# Current Status (as of February 23, 2026)

Implemented now:

- Clean Architecture .NET 8 backend scaffold in `src/`
- Domain model + EF Core `DbContext` + initial migration
- PostgreSQL local setup via Docker Compose
- Pluggable provider integration with priority/fallback (`CricketDataOrg`, `ApiSports`)
- Normalization + upsert pipeline for teams, venues, and matches
- `GET /api/v1/matches/upcoming` backed by database with filter support (`country`, `format`, `from`, `to`)
- `POST /api/v1/admin/sync/upcoming` to trigger fixture sync
- Unit tests + integration smoke tests

Current behavior:

- The project is API-first at this stage (no website UI yet)
- Upcoming endpoint reads from Postgres; if empty, it triggers provider sync and then returns data
- Provider priority is configurable in `src/CricStats.Api/appsettings.json` under `CricketProviders`
- Swagger is available locally at `http://localhost:5000/swagger` while the API runs in Development

Not implemented yet:

- Weather API integration and composite weather computation pipeline
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
"ApiSports",
"Cricsheet"
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
- Basic upcoming endpoint (stub data)

## Milestone 2 – Provider Integration (Completed)

- Implement 2 providers
- Normalize + upsert fixtures
- Country filtering support

## Milestone 3 – Weather Integration (Planned)

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
- Node 20+
- Docker
- PostgreSQL
- Hangfire
- Weather API key
- Cricket API key(s)

---

# Milestone 1 Delivery Summary (Completed)

Completed deliverables:

1. Scaffolded solution and project structure.
2. Implemented Domain layer + `CricStatsDbContext`.
3. Added initial EF migration + Docker Compose for PostgreSQL.
4. Implemented `GET /api/v1/matches/upcoming` using deterministic stub data.
5. Added unit and integration test coverage for Milestone 1 scope.

# Milestone 2 Delivery Summary (Completed)

Completed deliverables:

1. Implemented two pluggable providers (`CricketDataOrg`, `ApiSports`).
2. Added provider priority/fallback orchestration (`CricketProviders:Priority`).
3. Added normalization + upsert flow to persist teams, venues, and matches.
4. Switched upcoming matches endpoint to database-backed queries with country/format/date filters.
5. Added admin sync endpoint: `POST /api/v1/admin/sync/upcoming`.

Next implementation focus:

1. Milestone 3: weather integration and composite risk computation.
2. Persist and expose computed weather risk details per match.
3. Keep provider-driven ingestion as the source of truth for upcoming fixtures.

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

## 5. Build and test

```bash
dotnet build CricStats.sln
dotnet test CricStats.sln
```

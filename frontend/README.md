# CricStats Frontend (Next.js)

This frontend is a first dashboard slice for:
- Upcoming matches
- Upcoming series

It reads from the backend API (default: `http://localhost:5000`) and renders venue/stadium fields for matches and series matches.

## Prerequisites

- Node.js 20+
- Backend API running on `http://localhost:5000`

## Setup

Run these commands from `frontend/`:

```bash
npm install
```

Optional custom backend URL:

```bash
echo 'API_BASE_URL=http://localhost:5000' > .env.local
```

## Run

```bash
npm run dev
```

Open:
- `http://localhost:3000`

## Notes

- This dashboard is server-rendered and uses `cache: "no-store"` so it always fetches fresh API data.
- If an endpoint fails, the page shows an inline error for that section.

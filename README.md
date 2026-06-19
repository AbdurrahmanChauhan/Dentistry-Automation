# Dentistry Automation Platform

Production-capable MVP for dental Revenue Cycle Management (RCM) automation. Orchestrates eligibility, claims, ERA/835 posting, denials, and exception queues with mock-first integrations (DentalBridge, clearinghouse, PMS write-back).

## Architecture

- **platform-api** (.NET 8) — DA API v1, auth, rate limits, workflow triggers
- **rcm-engine** (.NET 8) — Domain logic, rules engine, orchestration, EDI parsing
- **integrations** (.NET 8) — Mock DentalBridge, clearinghouse, PMS write-back adapters
- **platform-web** (React + TypeScript) — RCM ops console
- **ai-workers** (Python 3.12) — EOB extraction, denial classification

## Quick Start

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- Python 3.12+
- Docker Desktop

### Local Development

```bash
# Start infrastructure
docker compose up -d

# Apply database schema
dotnet ef database update --project src/platform-api

# Run API
dotnet run --project src/platform-api

# Run web UI (separate terminal)
cd src/platform-web && npm install && npm run dev

# Run AI worker (separate terminal)
cd src/ai-workers && pip install -r requirements.txt && python -m workers.main
```

### Default API

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- Web UI: http://localhost:5173
- Demo API key: `da-demo-key-change-in-production`

## Architecture Presentation

Open **[architecture.html](architecture.html)** in a browser for a self-contained, founder-ready architecture walkthrough (system diagrams, workflows, API catalog, integrations, AI, security, demo script).

## Documentation

See [docs/](docs/) for research, architecture, API spec, security review, and deployment guides.

## License

Proprietary — Dentistry Automation Platform MVP

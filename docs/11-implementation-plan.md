# Implementation Plan

## Repository Structure

```
Dentistry Automation Platform/
├── docs/                    # All planning documents
├── src/
│   ├── DentistryAutomation.sln
│   ├── platform-api/        # .NET 8 REST API
│   ├── rcm-engine/          # Domain + services
│   ├── integrations/        # Mock adapters
│   ├── platform-web/        # React console
│   ├── ai-workers/          # Python FastAPI
│   └── tests/               # xUnit tests
├── samples/edi/             # Sample 835 files
├── infra/bicep/             # Azure IaC
├── docker/                  # Dockerfiles
├── scripts/                 # Setup scripts
└── .github/workflows/       # CI pipeline
```

## Local Development

```bash
# Infrastructure
docker compose up -d sqlserver redis

# API (requires .NET 8 SDK or Docker)
docker compose up api
# OR: dotnet run --project src/platform-api

# AI Worker (requires Python 3.12)
cd src/ai-workers && pip install -r requirements.txt
python -m uvicorn workers.main:app --port 8000

# Web UI
cd src/platform-web && npm install && npm run dev
```

## Demo Workflow

1. Open http://localhost:5173
2. **Eligibility:** Select patient → Run Eligibility Check
3. **Claims:** Ingest from PMS → Submit claim
4. **Remittances:** Poll Clearinghouse → Auto-Post
5. **Work Queue:** Review exceptions → AI Summary → Resolve

## API Authentication

```bash
curl -H "X-Api-Key: da-demo-key-change-in-production" http://localhost:5000/v1/health
```

## Adapter Swap Strategy

Replace mock implementations in `integrations/ServiceCollectionExtensions.cs`:

```csharp
// Development
services.AddMockIntegrations();

// Production (when credentials available)
services.AddHttpIntegrations(aiWorkerUrl);
services.AddSingleton<IClearinghousePort, StediClearinghouseAdapter>();
```

## Key Design Decisions

See `docs/adr/` for Architecture Decision Records.

## Team Allocation (Suggested)

| Area | Owner | Effort |
|------|-------|--------|
| RCM Engine + API | Backend (.NET) | 60% |
| Integrations | Backend | 20% |
| Ops Console | Frontend (TS) | 10% |
| AI Workers | Python | 10% |

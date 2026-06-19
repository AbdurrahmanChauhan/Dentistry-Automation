# Dentistry Automation Platform

[![GitHub](https://img.shields.io/badge/GitHub-AbdurrahmanChauhan%2FDentistry--Automation-blue)](https://github.com/AbdurrahmanChauhan/Dentistry-Automation)

Production-capable MVP for **dental Revenue Cycle Management (RCM) automation**. Orchestrates eligibility verification, claims lifecycle, ERA/835 payment posting, denial management, and human-in-the-loop exception queues — built on mock-first integrations that swap to live DentalBridge, clearinghouse, and PMS write-back adapters without domain changes.

> **Repository:** https://github.com/AbdurrahmanChauhan/Dentistry-Automation  
> **Architecture presentation (local):** [`architecture.html`](architecture.html)  
> **Architecture presentation (live):** https://abdurrahmanchauhan.github.io/Dentistry-Automation/ *(GitHub Pages — enable once, see below)*

---

## Table of Contents

- [Overview](#overview)
- [Strategic Position](#strategic-position)
- [System Architecture](#system-architecture)
- [RCM Lifecycle](#rcm-lifecycle)
- [Repository Structure](#repository-structure)
- [Technology Stack](#technology-stack)
- [Workflows](#workflows)
- [Data Model](#data-model)
- [DA API v1](#da-api-v1)
- [Integration Architecture](#integration-architecture)
- [AI & Human-in-the-Loop](#ai--human-in-the-loop)
- [Security & Compliance](#security--compliance)
- [Observability](#observability)
- [Getting Started](#getting-started)
- [Demo Walkthrough](#demo-walkthrough)
- [Deployment](#deployment)
- [Testing](#testing)
- [Documentation](#documentation)
- [Roadmap](#roadmap)
- [License](#license)

---

## Overview

Dental practices and DSOs lose revenue and staff hours to manual RCM workflows trapped across **PMS**, **clearinghouses**, **payer portals**, and **AR reports**. Dentistry Automation already ships **DentalBridge API** (PMS-agnostic read) and **RCM write-back endpoints** (`/rcm/eligibility-verification`, `/rcm/patientpaymentswriteback`). This platform adds the missing **workflow orchestration layer**:

| Capability | Description |
|------------|-------------|
| **Eligibility orchestration** | 270/271 jobs, benefit snapshots, PMS write-back |
| **Claim lifecycle** | Ingest, rules scrub, 837D submit, 999/277CA monitoring |
| **ERA/835 posting** | Parse remittances, match to claims, auto-post with confidence gates |
| **Exception queues** | Denials, ack rejections, posting failures — routed to RCM staff |
| **DA API v1** | Partner-facing REST with auth, rate limits, OpenAPI spec |
| **RCM ops console** | React dashboard for eligibility, claims, remittances, work items |

**Architecture style:** Modular monolith (.NET 8) + background workers + Python AI sidecar — optimized for a small team, correctness-critical money paths, and fast iteration.

---

## Strategic Position

This MVP does **not** rebuild PMS connectivity. It builds the **workflow brain** on top of Dentistry Automation's existing DentalBridge moat.

| Competitor | Strength | Gap | Our opportunity |
|------------|----------|-----|-----------------|
| Zuub / Dental Intelligence | Deep verification | Weak ERA→PMS loop | End-to-end workflow closure |
| Zentist | ERA AI, denial triage | Limited PMS posting breadth | DentalBridge write-back breadth |
| dentalrobot | 12+ PMS write-back | Less clearinghouse-native | Ack monitoring + CH routing |
| eAssist / DCS | Human scale | Low software margin | Software automation + HITL queues |

**Differentiation:** Loop closure (eligibility → submit → ack → ERA → post → denial → resubmit) with boring reliability (retries, audit, idempotency) and DSO-grade multi-tenant analytics.

---

## System Architecture

### High-Level Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                                   │
│  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐         │
│  │  RCM Ops Console │  │  Partner APIs    │  │  Clinician View  │         │
│  │  React + TS      │  │  (DA API v1)     │  │  (future)        │         │
│  │  :5173           │  │  X-Api-Key       │  │                  │         │
│  └────────┬─────────┘  └────────┬─────────┘  └──────────────────┘         │
└───────────┼─────────────────────┼───────────────────────────────────────────┘
            │                     │
            ▼                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         API GATEWAY — platform-api (.NET 8)                  │
│  Auth (API keys) │ Rate limits (100/min) │ Swagger │ Usage logging │ :5000  │
│  Background: WorkflowBackgroundService (eligibility + ack polling, 30s)      │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DOMAIN — rcm-engine (.NET 8)                         │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │ Eligibility │ │   Claims    │ │ ERA/Posting │ │ Work Items  │           │
│  │ Orchestrator│ │  Lifecycle  │ │ Match Engine│ │ + Denials   │           │
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘           │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                            │
│  │ Rules Engine│ │ EDI 835     │ │ Audit + Jobs│                            │
│  │ (CDT scrub) │ │ Parser      │ │             │                            │
│  └─────────────┘ └─────────────┘ └─────────────┘                            │
└─────────────────────────────────┬───────────────────────────────────────────┘
                                  │ Adapter Ports (interfaces)
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                    INTEGRATIONS — integrations (.NET 8)                      │
│  IDentalBridgeClient │ IClearinghousePort │ IPmsWriteBackPort │ IAiWorkerClient│
│  (MVP: mock impl)    │ (MVP: mock impl)   │ (MVP: mock impl)  │ (HTTP + fallback)│
└───────────┬─────────────────┬─────────────────┬─────────────────┬───────────┘
            │                 │                 │                 │
            ▼                 ▼                 ▼                 ▼
     ┌────────────┐   ┌────────────┐   ┌────────────┐   ┌────────────┐
     │ PMS via    │   │ Clearing-  │   │ DA Write-  │   │ Python AI  │
     │ DentalBridge│  │ house      │   │ Back API   │   │ Worker     │
     │ Dentrix,   │   │ Stedi,     │   │ /rcm/*     │   │ FastAPI    │
     │ Open Dental│   │ DentalXChg │   │            │   │ :8000      │
     └────────────┘   └────────────┘   └────────────┘   └────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         DATA & INFRASTRUCTURE                                │
│  Azure SQL (EF Core) │ Redis │ Blob Storage │ audit_events │ App Insights   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Project | Path | Stack | Responsibility |
|---------|------|-------|----------------|
| **platform-api** | `src/platform-api/` | .NET 8 Web API | HTTP surface, API key auth, rate limits, Swagger, middleware, background service |
| **rcm-engine** | `src/rcm-engine/` | .NET 8 | Domain entities, EF Core DbContext, orchestration services, rules engine, EDI parser |
| **integrations** | `src/integrations/` | .NET 8 | Mock adapters today; Stedi/DentalBridge live adapters tomorrow |
| **platform-web** | `src/platform-web/` | React 18 + TypeScript | RCM ops console — dashboard, eligibility, claims, remittances, work queue |
| **ai-workers** | `src/ai-workers/` | Python 3.12 + FastAPI | EOB extraction, denial summarization |
| **tests** | `src/tests/` | xUnit + pytest | Scrub rules, 835 parser, AI worker endpoints |

### Multi-Tenant Model

```
Organization (DSO / Practice Group)
    └── Location (Clinic / Office)
            └── Provider (NPI, name)
            └── Patient → Coverage → Claims → Remittances
```

Every entity carries `OrganizationId`. All API queries are tenant-scoped. Demo seed data creates **Demo Dental Group** with one Phoenix clinic.

### Key Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Architecture style | Modular monolith + workers | Small team; money-path transactions stay in one DB |
| PMS integration | DentalBridge read + DA write-back | DA already owns PMS connectivity |
| Clearinghouse | Stedi-shaped port (mock first) | JSON-native, webhooks for 277CA/835 |
| Database | Azure SQL + EF Core | Strong transactional guarantees for payments |
| AI runtime | Python sidecar | PDF/ML ecosystem; .NET validates all money movement |
| Auto-posting | Confidence ≥ 0.95 | Never auto-move money below threshold |

See [`docs/adr/README.md`](docs/adr/README.md) for full ADRs.

---

## RCM Lifecycle

End-to-end revenue loop the platform automates:

```
Appointment → Eligibility (270/271) → Benefit Snapshot
    → Claim Built → Rules Scrub → Submit (837D)
    → Ack (999/277CA) → Adjudication → ERA (835)
    → Match Engine → Post to PMS → Denial? → Work Queue → Resubmit
```

| Stage | EDI / Artifact | Service | Entity | Automation |
|-------|----------------|---------|--------|------------|
| Eligibility | 270/271 | `EligibilityOrchestrator` | `EligibilityCheck`, `BenefitSnapshot` | Auto if confidence ≥ 0.95 |
| Claim scrub | Internal | `ClaimScrubService` | `Claim`, `ClaimLine` | Deterministic rules |
| Submit | 837D | `ClaimSubmissionService` | `ClaimSubmission` | Auto via clearinghouse |
| Acknowledge | 999, 277CA | `AckMonitoringService` | `AckEvent` | Auto poll; reject → work item |
| Remittance | 835 ERA | `RemittanceService` | `Remittance`, `RemittanceLine` | Poll or file upload |
| Match & post | 835 + PMS | `PaymentMatchEngine`, `PostingService` | `PostingAttempt` | Auto if ≥ 0.95; else HITL |
| Denials | CARC/RARC | `WorkItemService` + AI | `WorkItem` | AI triage; human resolves |

---

## Repository Structure

```
Dentistry-Automation/
├── architecture.html          # Visual architecture presentation (open in browser)
├── README.md
├── docker-compose.yml         # SQL Server, Redis, Azurite, API, AI, Web
├── docker/                    # Dockerfiles + nginx config
├── docs/                      # 14 planning documents + OpenAPI spec + ADRs
├── infra/bicep/               # Azure deployment templates
├── samples/edi/               # Sample 835 JSON and X12 files
├── scripts/setup-local.ps1
├── .github/workflows/ci.yml   # dotnet test, pytest, npm build, Docker
└── src/
    ├── DentistryAutomation.sln
    ├── platform-api/          # Controllers, Auth, Middleware, Background jobs
    ├── rcm-engine/            # Domain/, Data/, Services/, Ports/
    ├── integrations/          # DentalBridge/, Clearinghouse/, Ai/ mocks
    ├── platform-web/          # React pages + API client
    ├── ai-workers/            # FastAPI worker
    └── tests/                 # RcmEngine.Tests/
```

---

## Technology Stack

| Layer | Technologies |
|-------|-------------|
| **Backend** | .NET 8, ASP.NET Core, EF Core, Serilog, OpenTelemetry, AspNetCoreRateLimit |
| **Frontend** | React 18, TypeScript, Vite, React Router |
| **AI** | Python 3.12, FastAPI, Uvicorn, Pydantic |
| **Database** | Azure SQL / SQL Server (local Docker) |
| **Cache** | Redis (configured in docker-compose) |
| **CI/CD** | GitHub Actions |
| **Cloud** | Azure App Service, Azure SQL, Blob Storage, App Insights (Bicep) |

---

## Workflows

### 1. Eligibility Orchestration

```
POST /v1/eligibility/check
    → Build 270 request (patient, coverage, provider NPI)
    → IClearinghousePort.CheckEligibilityAsync (mock 271)
    → Confidence ≥ 0.95? → Verified + BenefitSnapshot (7-day TTL)
    → IPmsWriteBackPort.WriteEligibilityAsync
    → Below threshold? → NeedsReview (never auto-verify from AI/portal)
```

### 2. Claim Lifecycle

```
POST /v1/claims/ingest → IDentalBridgeClient.GetClaimProceduresAsync
    → ClaimScrubService (CDT format, tooth for D2/D3/D4, amounts, payer ID)
    → POST /v1/claims/{id}/submit → IClearinghousePort.Submit837DAsync
    → AckMonitoringService polls 999/277CA
    → Reject? → WorkItem (AckRejection)
```

**Scrub rules:** Valid CDT (`D####`), tooth required for restorative codes, positive amounts, at least one line, payer ID required.

### 3. ERA / 835 Payment Posting (Money Path)

```
POST /v1/remittances/poll → IClearinghousePort.PollRemittancesAsync
    → Edi835Parser (JSON or X12-lite)
    → PaymentMatchEngine (claim ID + CDT + amount tolerance)
    → Confidence ≥ 0.95 → IPmsWriteBackPort.WritePaymentAsync (idempotent key: post-{lineId})
    → Below threshold → WorkItem (PostingException)
    → CARC + $0 paid → WorkItem (Denial)
```

### 4. Denial & Exception Workbench

```
Work item created (Open) → POST /v1/work-items/{id}/ai-summary
    → IAiWorkerClient.SummarizeDenialAsync (CARC-based action suggestion)
    → RCM reviewer assigns / resolves via PATCH /v1/work-items/{id}
```

**Work item types:** `EligibilityReview`, `ScrubFailure`, `AckRejection`, `PostingException`, `Denial`, `Underpayment`, `General`

---

## Data Model

20 core tables in `RcmDbContext`:

| Domain | Tables |
|--------|--------|
| **Tenant** | `organizations`, `locations`, `providers`, `patients`, `coverages`, `appointments` |
| **Eligibility** | `eligibility_checks`, `benefit_snapshots` |
| **Claims** | `claims`, `claim_lines`, `claim_submissions`, `claim_status_events`, `ack_events` |
| **Remittances** | `remittances`, `remittance_lines`, `posting_attempts` |
| **Workflow** | `work_items`, `integration_jobs` |
| **Security / API** | `audit_events`, `api_keys`, `api_usage_logs` |

**Data ownership:** PMS remains ledger of record. Platform owns workflow state, provenance, and audit trail.

Full ER diagram: [`docs/09-database-design.md`](docs/09-database-design.md)

---

## DA API v1

**Base URL:** `http://localhost:5000` (local)  
**Auth:** `X-Api-Key: da-demo-key-change-in-production`  
**OpenAPI:** [`docs/06-api-spec.openapi.yaml`](docs/06-api-spec.openapi.yaml) · Swagger UI at `/swagger`

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `GET` | `/v1/health` | Platform health (no auth) |
| `POST` | `/v1/eligibility/check` | Trigger 270/271 eligibility job |
| `GET` | `/v1/eligibility` | List eligibility checks |
| `GET` | `/v1/eligibility/{id}` | Check detail + benefit snapshot |
| `GET` | `/v1/claims` | List claims (filter by `?status=`) |
| `POST` | `/v1/claims/ingest` | Ingest from DentalBridge |
| `GET` | `/v1/claims/{id}` | Claim detail + status history |
| `POST` | `/v1/claims/{id}/submit` | Scrub + submit 837D |
| `GET` | `/v1/remittances` | List ERA remittances |
| `POST` | `/v1/remittances/poll` | Poll clearinghouse for 835 |
| `POST` | `/v1/remittances/upload-835` | Upload raw 835 (JSON or X12) |
| `POST` | `/v1/remittances/{id}/post` | Auto-post matched lines |
| `POST` | `/v1/remittances/lines/{lineId}/post` | Post single line (`?force=true`) |
| `GET` | `/v1/work-items` | Exception/denial queue |
| `GET` | `/v1/work-items/{id}` | Work item detail |
| `PATCH` | `/v1/work-items/{id}` | Assign, resolve, escalate |
| `POST` | `/v1/work-items/{id}/ai-summary` | AI denial analysis |
| `GET` | `/v1/locations` | List organization locations |
| `GET` | `/v1/locations/{id}/kpis` | Denial rate, eligibility %, AR metrics |
| `GET` | `/v1/patients` | List patients (`?locationId=`) |

**Example:**

```bash
curl -H "X-Api-Key: da-demo-key-change-in-production" http://localhost:5000/v1/health
```

---

## Integration Architecture

Four swappable ports in `src/rcm-engine/Ports/IntegrationPorts.cs`:

| Port | MVP Implementation | Production Target | Key Methods |
|------|-------------------|-------------------|-------------|
| `IDentalBridgeClient` | `MockDentalBridgeClient` | DA DentalBridge REST | `GetAppointments`, `GetCoverage`, `GetClaimProcedures` |
| `IClearinghousePort` | `MockClearinghousePort` | Stedi JSON API | `CheckEligibility`, `Submit837D`, `PollRemits`, `PollAcks` |
| `IPmsWriteBackPort` | `MockPmsWriteBackPort` | `/rcm/patientpaymentswriteback` | `WritePayment`, `WriteEligibility` |
| `IAiWorkerClient` | `MockAiWorkerClient` / HTTP | Python FastAPI `:8000` | `ExtractEob`, `SummarizeDenial` |

**Swap to production** — change one registration in `integrations/ServiceCollectionExtensions.cs`:

```csharp
// Development
services.AddMockIntegrations();

// Production (when credentials available)
services.AddHttpIntegrations(aiWorkerBaseUrl);
// + register StediClearinghouseAdapter, live DentalBridge client, etc.
```

### EDI Standards

| Transaction | Purpose | MVP Status |
|-------------|---------|------------|
| 270/271 | Eligibility | Mock + structured JSON storage |
| 837D | Dental claim submit | Mock + payload logging |
| 835 | ERA remittance | JSON + X12-lite parser |
| 999 / 277CA | Acknowledgments | Poll + reject → work item |
| 276/277 | Claim status | Interface defined; Phase 2 |

Sample files: [`samples/edi/`](samples/edi/)

---

## AI & Human-in-the-Loop

**Principle:** Rules for money movement; AI for variation and triage.

| Use deterministic rules | Use AI |
|------------------------|--------|
| X12 formatting, CDT validation | EOB PDF/text extraction |
| ERA line matching + amount tolerance | Denial summarization |
| Auto-post gate (≥ 0.95) | Suggested next action |
| CARC → queue routing | Work queue priority scoring |
| Idempotent write-back | Benefit summary for clinicians (future) |

### Confidence Thresholds

| Operation | Auto threshold | Below threshold |
|-----------|---------------|-----------------|
| ERA line match + post | ≥ 0.95 | Exception work queue |
| EOB field extraction | ≥ 0.90 | Manual review |
| Eligibility "Verified" | 271 only, ≥ 0.95 | `NeedsReview` — never auto-verify |
| Denial recommendations | Never auto | Human must approve before resubmit |

**AI worker endpoints** (`src/ai-workers/workers/main.py`):

- `GET /health`
- `POST /extract/eob` — structured remittance lines from PDF/text
- `POST /summarize/denial` — summary + suggested action by CARC code

Falls back to `MockAiWorkerClient` if the worker is unavailable.

---

## Security & Compliance

| Control | Implementation |
|---------|----------------|
| **Authentication** | API keys (SHA-256 hashed in `api_keys` table) |
| **Tenant isolation** | All queries scoped by `OrganizationId` |
| **Audit trail** | Append-only `audit_events` — PHI access logging |
| **Rate limiting** | 100 requests/minute on `/v1/*` (HTTP 429) |
| **Encryption** | TLS in transit; Azure SQL TDE at rest (production) |
| **Usage metering** | `api_usage_logs` per request (method, path, duration) |
| **AI PHI** | No training on tenant data; inference logged with correlation ID |

Platform designed as HIPAA Business Associate. BAA required before production PHI. See [`docs/08-security-review.md`](docs/08-security-review.md).

---

## Observability

| Capability | Implementation |
|------------|----------------|
| Structured logging | Serilog → console / App Insights |
| Distributed tracing | OpenTelemetry ASP.NET instrumentation |
| Health check | `GET /v1/health` (DB connectivity) |
| Correlation IDs | `X-Correlation-Id` middleware |
| API usage logs | Per-request logging to `api_usage_logs` |
| Background jobs | `integration_jobs` with DeadLetter status |
| KPIs | `GET /v1/locations/{id}/kpis` |

---

## Getting Started

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8.0+ |
| Node.js | 18+ (20 recommended) |
| Python | 3.12 (not 3.14 — pydantic compatibility) |
| Docker Desktop | Latest (optional but recommended) |

### Option A — Docker (recommended)

```bash
git clone https://github.com/AbdurrahmanChauhan/Dentistry-Automation.git
cd Dentistry-Automation
docker compose up -d
```

| Service | URL |
|---------|-----|
| API + Swagger | http://localhost:5000/swagger |
| Web UI | http://localhost:5173 |
| AI Worker | http://localhost:8000/health |
| SQL Server | `localhost:1433` (sa / `DaPlatform123!`) |

### Option B — Local (without Docker for app services)

```bash
# 1. Infrastructure only
docker compose up -d sqlserver redis

# 2. API (.NET 8)
dotnet run --project src/platform-api
# Schema auto-created via EF Core EnsureCreated + demo seed on startup

# 3. AI worker (Python 3.12)
cd src/ai-workers
pip install -r requirements.txt
python -m uvicorn workers.main:app --host 0.0.0.0 --port 8000

# 4. Web UI
cd src/platform-web
npm install
npm run dev
```

### Default Credentials

| Item | Value |
|------|-------|
| Demo API key | `da-demo-key-change-in-production` |
| SQL (Docker) | `sa` / `DaPlatform123!` |
| Demo org | Demo Dental Group — Phoenix clinic |

---

## Demo Walkthrough

1. Open **http://localhost:5173** — Dashboard shows health, KPIs, locations
2. **Eligibility** — Select patient → **Run Eligibility Check** → Verified + benefit snapshot
3. **Claims** — **Ingest from PMS** → **Submit** → status moves to Submitted → AckAccepted
4. **Remittances** — **Poll Clearinghouse** → ERA with 3 lines → **Auto-Post** → mock PMS write-back
5. **Work Queue** — Review exceptions → **AI Summary** → **Resolve**
6. **API** — Swagger at http://localhost:5000/swagger with `X-Api-Key` header

---

## Deployment

| Environment | Method | Config |
|-------------|--------|--------|
| **Local** | `docker-compose.yml` | SQL Server, Redis, Azurite, API, AI, Web |
| **Azure** | `infra/bicep/main.bicep` | App Service, Azure SQL S0, Storage, App Insights |
| **CI** | `.github/workflows/ci.yml` | dotnet test, pytest, npm build, Docker images |
| **Architecture site** | `.github/workflows/github-pages.yml` | Publishes `architecture.html` to GitHub Pages |

Release process and runbooks: [`docs/13-deployment-plan.md`](docs/13-deployment-plan.md)

### GitHub Pages (architecture presentation)

The visual architecture deck is published automatically when you push to `master`.

**One-time setup** (repo owner only):

1. Open https://github.com/AbdurrahmanChauhan/Dentistry-Automation/settings/pages
2. Under **Build and deployment → Source**, select **GitHub Actions**
3. Push to `master` (or run the **Deploy GitHub Pages** workflow manually)

**Live URLs** (after first successful deploy):

| URL | Purpose |
|-----|---------|
| https://abdurrahmanchauhan.github.io/Dentistry-Automation/ | Homepage (`index.html` = architecture deck) |
| https://abdurrahmanchauhan.github.io/Dentistry-Automation/architecture.html | Same deck (alternate path) |

The workflow copies `architecture.html` to both paths on each deploy. No build step required — the file is self-contained HTML/CSS.

---

## Testing

```bash
# .NET unit tests
dotnet test src/DentistryAutomation.sln

# Python AI worker tests (requires Python 3.12)
pip install -r src/ai-workers/requirements.txt
pytest src/ai-workers/tests/ -v

# Frontend build verification
cd src/platform-web && npm run build
```

Coverage: claim scrub rules, 835 parser (JSON + X12-lite), payment match thresholds, AI worker health/summary/extraction.

Strategy details: [`docs/12-testing-strategy.md`](docs/12-testing-strategy.md)

---

## Documentation

| # | Document | Description |
|---|----------|-------------|
| — | [`architecture.html`](architecture.html) | **Visual architecture presentation** |
| 01 | [`docs/01-research-report.md`](docs/01-research-report.md) | Domain & industry research |
| 02 | [`docs/02-domain-analysis.md`](docs/02-domain-analysis.md) | Pain points & automation opportunities |
| 03 | [`docs/03-competitor-analysis.md`](docs/03-competitor-analysis.md) | Market landscape |
| 04 | [`docs/04-prd.md`](docs/04-prd.md) | Product requirements |
| 05 | [`docs/05-architecture.md`](docs/05-architecture.md) | System architecture |
| 06 | [`docs/06-api-spec.openapi.yaml`](docs/06-api-spec.openapi.yaml) | OpenAPI 3.0 spec |
| 07 | [`docs/07-ai-architecture.md`](docs/07-ai-architecture.md) | AI use cases & HITL |
| 08 | [`docs/08-security-review.md`](docs/08-security-review.md) | HIPAA & threat model |
| 09 | [`docs/09-database-design.md`](docs/09-database-design.md) | ER model & indexes |
| 10 | [`docs/10-mvp-roadmap.md`](docs/10-mvp-roadmap.md) | Sprint plan |
| 11 | [`docs/11-implementation-plan.md`](docs/11-implementation-plan.md) | Dev workflow |
| 12 | [`docs/12-testing-strategy.md`](docs/12-testing-strategy.md) | Test pyramid |
| 13 | [`docs/13-deployment-plan.md`](docs/13-deployment-plan.md) | Azure & runbooks |
| 14 | [`docs/14-risk-assessment.md`](docs/14-risk-assessment.md) | Risk register |
| ADR | [`docs/adr/README.md`](docs/adr/README.md) | Architecture decisions |

---

## Roadmap

| Phase | Timeline | Scope | Status |
|-------|----------|-------|--------|
| **MVP** | Weeks 1–12 | Full monorepo, mock integrations, ops console, DA API v1, docs, CI | **Complete** |
| **Phase 2** | Weeks 13–20 | Stedi sandbox, DentalBridge production, NEA attachments, 276/277 polling | Planned |
| **Phase 3** | Weeks 21–30 | Dual CH failover, portfolio denial analytics, underpayment detection, SOC 2 | Planned |

---

## License

Proprietary — Dentistry Automation Platform MVP

---

<p align="center">
  Built for dental RCM automation · <a href="https://github.com/AbdurrahmanChauhan/Dentistry-Automation">View on GitHub</a> · <a href="architecture.html">Architecture Presentation</a>
</p>

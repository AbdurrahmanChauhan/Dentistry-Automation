# System Architecture Document

## Architecture Style

**Modular monolith (.NET 8) + event-driven background workers + Python AI sidecar**

Rationale: Small team, correctness-critical money path, fast iteration without microservice network partition risk.

## Component Diagram

```
┌─────────────┐     ┌──────────────────┐     ┌─────────────────┐
│ React Web   │────▶│ Platform API     │────▶│ Azure SQL       │
│ Console     │     │ (.NET 8)         │     │                 │
└─────────────┘     └────────┬─────────┘     └─────────────────┘
                             │
                    ┌────────┴────────┐
                    │  RCM Engine     │
                    │  (Domain)       │
                    └────────┬────────┘
                             │
         ┌───────────────────┼───────────────────┐
         ▼                   ▼                   ▼
┌────────────────┐  ┌────────────────┐  ┌────────────────┐
│ Mock DentalBridge│ │ Mock Clearinghouse│ │ Python AI Worker│
│ (PMS read)      │  │ (270/837/835)   │  │ (EOB, denials)  │
└────────────────┘  └────────────────┘  └────────────────┘
```

## Projects

| Project | Stack | Responsibility |
|---------|-------|----------------|
| `platform-api` | .NET 8 | HTTP API, auth, rate limits, background service |
| `rcm-engine` | .NET 8 | Domain, rules, orchestration, EDI parser |
| `integrations` | .NET 8 | Adapter ports (mock → live swap) |
| `platform-web` | React/TS | RCM ops console |
| `ai-workers` | Python 3.12 | FastAPI extraction/summarization |

## Data Architecture

- **Transactional:** Azure SQL — workflow state, audit, API usage
- **Documents:** Blob storage — EOB PDFs, raw 835 (future)
- **Cache:** Redis — eligibility TTL, idempotency keys (configured, MVP uses in-memory rate limit)
- **Audit:** Append-only `audit_events` table

## Integration Ports

```csharp
IDentalBridgeClient    // PMS read
IClearinghousePort     // 270/271, 837D, 835, acks
IPmsWriteBackPort      // Payment + eligibility write-back
IAiWorkerClient        // EOB extraction, denial summary
```

Swap mock implementations without domain changes.

## Security

- API key auth (SHA-256 hashed keys in DB)
- Tenant isolation on all queries via `OrganizationId`
- PHI access audit logging
- TLS in transit; SQL TDE at rest (Azure)

## Observability

- Serilog structured logs
- OpenTelemetry ASP.NET instrumentation
- Health endpoint `/v1/health`
- API usage logs per request
- Background workflow service (30s cycle)

## Deployment

- **Local:** docker-compose (SQL Server, Redis, Azurite, API, AI, Web)
- **Azure:** Bicep templates — App Service, Azure SQL, Storage, App Insights

See [13-deployment-plan.md](13-deployment-plan.md) for details.

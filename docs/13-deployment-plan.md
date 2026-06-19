# Production Deployment Plan

## Environments

| Environment | Purpose | Infrastructure |
|-------------|---------|----------------|
| `dev` | Local development | docker-compose |
| `staging` | Pre-production validation | Azure (scaled-down) |
| `prod` | Customer-facing | Azure HA |

## Docker Compose (Local/Dev)

```bash
docker compose up -d
```

Services:
- `sqlserver` — port 1433
- `redis` — port 6379
- `azurite` — blob/queue emulator
- `api` — port 5000
- `ai-worker` — port 8000
- `web` — port 5173

## Azure Deployment (Bicep)

```bash
az group create -n da-platform-rg -l eastus
az deployment group create -g da-platform-rg -f infra/bicep/main.bicep
```

Resources provisioned:
- Azure SQL Server + Database (S0)
- App Service Plan (B1) + API App
- Storage Account (EOB documents)
- Application Insights

## Release Process

1. PR → CI passes (build, test, Docker)
2. Merge to `main`
3. Deploy to staging via GitHub Actions (future)
4. Smoke test: health, eligibility, claim, remittance E2E
5. Staged rollout to production (10% → 50% → 100%)
6. Monitor: error rate, queue depth, API latency

## Rollback Plan

1. Revert to previous App Service deployment slot
2. Database: forward-only migrations; no destructive rollback
3. Feature flags for new workflow steps (future)

## Monitoring & Alerting

| Alert | Condition | Action |
|-------|-----------|--------|
| API down | Health check fails 3x | Page on-call |
| DLQ depth | Integration jobs in DeadLetter > 10 | Investigate adapter |
| Ack failures | AckRejected rate spike | Check clearinghouse |
| Posting exceptions | Work item queue > 50 | Scale RCM review |

## Runbook: Common Operations

### Restart API
```bash
az webapp restart -n da-platform-prod-api -g da-platform-rg
```

### Check integration job failures
```sql
SELECT * FROM integration_jobs WHERE status = 'DeadLetter' ORDER BY created_at DESC;
```

### Rotate API key
1. Generate new key
2. Insert hash into `api_keys`
3. Notify partner
4. Deactivate old key after 30-day grace period

## Secrets

| Secret | Storage |
|--------|---------|
| SQL connection string | Azure Key Vault |
| API demo keys | Key Vault (not in code) |
| Clearinghouse credentials | Key Vault (Phase 2) |
| AI provider keys | Key Vault (Phase 2) |

# MVP Roadmap

## Sprint Plan (8–12 Weeks)

| Sprint | Weeks | Deliverables | Status |
|--------|-------|--------------|--------|
| 0 — Foundation | 1 | Monorepo, SQL schema, auth, audit, CI, docker-compose | Complete |
| 1 — Eligibility | 2–3 | Orchestrator, 271 mock, benefit snapshots, API + UI | Complete |
| 2 — Claims | 4–5 | Ingest, scrub engine, 837D submit, ack monitoring, UI | Complete |
| 3 — ERA Posting | 6–7 | 835 parser, match engine, auto-post, exception queue | Complete |
| 4 — Denials + API | 8–9 | Work bench, AI summary, rate limits, OpenAPI | Complete |
| 5 — Production | 10–12 | Security review, observability, deployment runbooks | Complete |

## Phase 2 (Post-MVP)

- Stedi sandbox integration (live 270/271, 837D, 835)
- DentalBridge production API connection
- NEA FastAttach attachment workflow
- Bank EFT ↔ TRN reconciliation
- Entra ID staff authentication
- Payer portal RPA (Python Playwright workers)

## Phase 3 (DSO Scale)

- Dual clearinghouse failover
- Portfolio denial clustering analytics
- Underpayment detection vs contracted fees
- SOP/workflow engine for CBO routing
- SOC 2 Type II certification

## Milestone Gates

| Gate | Criteria |
|------|----------|
| M1 — Demo Ready | E2E: eligibility → claim → ERA → post |
| M2 — Partner Ready | OpenAPI published, rate limits, usage logs |
| M3 — Pilot Ready | Live sandbox integrations, HIPAA checklist complete |
| M4 — Production | Staged rollout, monitoring, on-call runbooks |

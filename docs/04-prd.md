# Product Requirements Document (PRD)

## Product Vision

Unified RCM automation platform that reduces manual dental billing work, accelerates cash flow, and scales DSOs without proportional hiring.

## Users & Roles

| Role | Goals | MVP Features |
|------|-------|--------------|
| OrgAdmin | Configure tenants, integrations | Org/location setup, API keys |
| PracticeManager | Monitor location KPIs | Dashboard, eligibility rate |
| RCMSpecialist | Resolve exceptions | Work queue, claim/remit detail |
| APIPartner | Integrate programmatically | DA API v1, OpenAPI |
| Clinician | Pre-visit eligibility status | Eligibility dashboard (read) |

## MVP Functional Requirements

### FR-1: Multi-Tenant Foundation
- Organization → Location → Provider hierarchy
- Tenant-scoped data isolation
- RBAC via API key + role claims

### FR-2: Eligibility Orchestration
- Trigger 270/271 on demand or via appointment poll
- Store benefit snapshots with 7-day TTL
- Write eligibility status to PMS (mock)
- Confidence gate: only 271 ≥0.95 = Verified

### FR-3: Claim Lifecycle
- Ingest claims from DentalBridge mock
- Rules-based scrub (CDT, tooth, amounts)
- Submit 837D via mock clearinghouse
- Track 999/277CA acknowledgments

### FR-4: ERA/835 Posting
- Poll clearinghouse for 835
- Parse JSON and X12-lite 835 files
- Deterministic match engine (≥0.95 auto-post)
- Exception queue for low-confidence lines
- PMS write-back via mock adapter

### FR-5: Denial Workbench
- CARC/RARC routed work items
- AI-generated summary and suggested action
- Assign, resolve, escalate workflow

### FR-6: DA API v1
- REST endpoints per OpenAPI spec
- API key authentication
- Rate limiting (100 req/min)
- Usage logging

### FR-7: RCM Ops Console
- React dashboard: eligibility, claims, remittances, work queue
- Location KPIs

### FR-8: Observability
- Structured logging (Serilog)
- OpenTelemetry tracing
- Health endpoint
- Background job processing

## Non-Functional Requirements

- API p95 < 300ms (reads)
- 99.5% job success rate (excl. external downtime)
- HIPAA technical safeguards (encryption, audit, tenant isolation)
- Idempotent write-back operations

## Out of Scope (MVP)

- Live Stedi/DentalXChange production
- Payer portal RPA
- NEA FastAttach
- Bank EFT reconciliation
- SOC 2 certification

## Success Metrics

- Eligibility verified rate > 80% (demo data)
- ERA auto-post rate > 90% for matched lines
- Zero double-posts (idempotency invariant)
- API uptime > 99.5%

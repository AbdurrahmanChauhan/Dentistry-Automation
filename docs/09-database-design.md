# Database Design

## ER Diagram

```
Organization 1──* Location 1──* Provider
     │                │
     │                ├──* Patient 1──* Coverage
     │                │         │
     │                │         ├──* Claim 1──* ClaimLine
     │                │         │         ├──* ClaimStatusEvent
     │                │         │         └──* ClaimSubmission
     │                │         └──* EligibilityCheck 1──1 BenefitSnapshot
     │                └──* Appointment
     │
     ├──* Remittance 1──* RemittanceLine 1──* PostingAttempt
     ├──* WorkItem
     ├──* AckEvent
     ├──* IntegrationJob
     ├──* AuditEvent
     └──* ApiKey
```

## Core Tables

| Table | Purpose | Key Indexes |
|-------|---------|-------------|
| `organizations` | Tenant root | `slug` UNIQUE |
| `locations` | Practice sites | `(organization_id, external_clinic_id)` |
| `patients` | Canonical patients | `(organization_id, location_id, external_patient_id)` |
| `coverages` | Insurance policies | `patient_id` |
| `eligibility_checks` | Verification jobs | `(organization_id, status)` |
| `benefit_snapshots` | Point-in-time benefits | `eligibility_check_id` UNIQUE |
| `claims` | Claim workflow state | `(organization_id, payer_claim_id)`, `(organization_id, status)` |
| `claim_lines` | Procedure lines | `claim_id` |
| `claim_submissions` | 837D submissions | `claim_id` |
| `claim_status_events` | Status timeline | `claim_id, occurred_at` |
| `ack_events` | 999/277CA events | `(organization_id, received_at)` |
| `remittances` | ERA headers | `(organization_id, era_reference)` |
| `remittance_lines` | ERA detail lines | `(remittance_id, line_number)` |
| `posting_attempts` | PMS write-back log | `idempotency_key` UNIQUE |
| `work_items` | Exception queue | `(organization_id, status, priority, created_at)` |
| `integration_jobs` | Async job queue | `(status, job_type)`, `idempotency_key` |
| `audit_events` | PHI access audit | `(organization_id, timestamp)` |
| `api_keys` | Partner API keys | `key_hash` UNIQUE |
| `api_usage_logs` | API metering | `(organization_id, timestamp)` |

## Schema Management

- EF Core `EnsureCreated` for MVP bootstrap
- Production: EF Core migrations (`dotnet ef migrations add`)
- Seed data via `DataSeeder` (demo org, patients, claims, API key)

## Data Ownership

| Data | System of Record | Platform Role |
|------|-----------------|---------------|
| Patient ledger | PMS | Workflow copy + provenance |
| Claim status | Platform + Clearinghouse | Canonical workflow state |
| Benefit snapshot | Platform | Cached from 271 with TTL |
| ERA/remittance | Clearinghouse | Stored copy for matching |
| Work items | Platform | Exception management |

## Retention Policy (Designed)

- Audit events: 7 years
- Benefit snapshots: 7 days active, then archive
- Raw 835/837 payloads: 3 years
- API usage logs: 1 year

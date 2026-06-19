# Research Report — Dental RCM Automation

## Executive Summary

Dental Revenue Cycle Management spans eligibility verification through final payment collection across fragmented systems (PMS, clearinghouse, payer portals, AR reports). Dentistry Automation's opportunity is closing the loop with workflow orchestration atop existing DentalBridge PMS integration.

## Dental Industry Structure

| Segment | Characteristics | RCM Implications |
|---------|----------------|------------------|
| Single practice | 1 PMS, local billing staff | Manual portals, spreadsheet tracking |
| Multi-location group | 2–15 sites, emerging CBO | Workflow variance, reporting gaps |
| DSO | 20–500+ sites, M&A growth | PMS heterogeneity, centralized billing, portfolio analytics |

## RCM Lifecycle Stages

1. **Eligibility (270/271)** — Pre-visit coverage verification; 45–85% of denials tied to eligibility gaps
2. **Claim build & scrub** — CDT validation, payer companion guides
3. **Submit & acknowledge (837D, 999, 277CA)** — Silent ack failures are a major revenue leak
4. **Status (276/277)** — Automated polling vs manual portal checks
5. **Adjudication & remittance (835)** — ERA is posting source of truth
6. **Payment posting** — Match ERA lines to PMS procedures; write-back
7. **Denials (CARC/RARC)** — Work queues, appeals, resubmission
8. **AR & collections** — Insurance vs patient AR separation

## Integration Ecosystem

- **PMS:** Dentrix, Open Dental, Eaglesoft — DentalBridge provides unified read; write-back via `/rcm/patientpaymentswriteback`
- **Clearinghouses:** DentalXChange, Vyne, Stedi (JSON-native, webhooks)
- **EDI:** HIPAA 5010 — 270/271, 837D (005010X224A2), 835, 276/277, 999, 277CA
- **Attachments:** NEA FastAttach (deferred in MVP)

## Key Findings

- Verification-only products leave cash on the table; ERA→PMS loop closure differentiates
- Rules engines outperform AI for money movement; AI excels at unstructured extraction
- DSO buyers require multi-tenant hierarchy, location KPIs, and audit trails
- Post-Change Healthcare breach drove demand for clearinghouse failover

## Sources

- Dentistry Automation API Documentation
- Stedi Healthcare API docs
- Open Dental REST API documentation
- Industry RCM guides (Today's Dental Consulting, Operant Billing)

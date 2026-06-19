# Risk Assessment

## Risk Register

| ID | Risk | Likelihood | Impact | Mitigation | Residual |
|----|------|------------|--------|------------|----------|
| R1 | No sandbox credentials delay go-live | High | Medium | Mock-first ports; swap adapters | Low |
| R2 | PMS write-back failures | Medium | High | Idempotency + reconciliation queue + manual UI | Medium |
| R3 | AI extraction errors | Medium | High | Confidence gates; HITL; deterministic validation | Low |
| R4 | HIPAA breach | Low | Critical | Tenant isolation, audit, encryption, no PHI in logs | Low |
| R5 | Scope creep | High | High | Strict MVP wedge; defer portal RPA | Medium |
| R6 | Clearinghouse outage | Medium | High | Dual-router pattern (Phase 3); job retry | Medium |
| R7 | Small team bus factor | Medium | Medium | Modular monolith, docs, ADRs | Medium |
| R8 | .NET/Python version mismatch | Medium | Low | Docker pins versions; CI validates | Low |
| R9 | Competitor feature parity | Medium | Medium | Focus on loop closure, not verification-only | Low |
| R10 | Customer trust in AI posting | Medium | High | Explainability, audit trail, human approval | Low |

## Highest Priority Risks

### R2: PMS Write-Back Failures
Money-path operations must never double-post or lose payments. Mitigated by:
- Idempotency keys on all write-back calls
- `posting_attempts` audit table
- Exception queue for manual review
- Mock adapter logs all write-backs for verification

### R4: HIPAA Breach
Platform handles PHI (patient names, member IDs, claim data). Mitigated by:
- Tenant-scoped queries
- Audit logging on PHI access
- Encryption in transit and at rest (Azure)
- BAA requirement before production PHI

## Risk Acceptance

| Risk | Accepted? | Notes |
|------|-----------|-------|
| Mock-only integrations for MVP | Yes | By design; adapters swappable |
| Simplified 835 parser | Yes | Full X12 parser in Phase 2 |
| No SOC 2 for MVP | Yes | Design for compliance; certify later |

## Review Schedule

- Monthly risk register review
- Pre-pilot security assessment
- Post-incident review within 48 hours

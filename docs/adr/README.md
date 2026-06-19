# ADR-001: Modular Monolith over Microservices

## Status
Accepted

## Context
Small engineering team building correctness-critical RCM platform.

## Decision
Modular monolith (.NET) with background workers and Python AI sidecar.

## Consequences
- Faster iteration, simpler deployment
- Clear module boundaries enable future service extraction
- Single database transaction for money-path operations

---

# ADR-002: DentalBridge for PMS Read

## Status
Accepted

## Context
DA already ships DentalBridge API for PMS-agnostic data extraction.

## Decision
Use DentalBridge-shaped adapter for PMS read; existing write-back APIs for posting.

## Consequences
- No per-PMS adapter build for MVP
- Focus engineering on workflow orchestration gap
- Direct PMS adapters only when DentalBridge gaps proven

---

# ADR-003: Mock-First Integration Ports

## Status
Accepted

## Context
No sandbox credentials available at project start.

## Decision
Define `IClearinghousePort`, `IDentalBridgeClient`, `IPmsWriteBackPort` with mock implementations.

## Consequences
- Full E2E demo without external dependencies
- Swap to Stedi/DentalBridge live adapters without domain changes

---

# ADR-004: Azure SQL as Primary Database

## Status
Accepted

## Context
JD specifies SQL; money-path requires strong transactional guarantees.

## Decision
Azure SQL with EF Core; TDE in production.

## Consequences
- Familiar .NET stack integration
- Vertical scaling sufficient for dental RCM volumes

---

# ADR-005: Confidence-Gated Auto-Posting

## Status
Accepted

## Context
AI-assisted ERA matching can err; incorrect posting damages trust.

## Decision
Auto-post only when match confidence ≥ 0.95; otherwise exception queue.

## Consequences
- Higher manual review volume initially (~5%)
- Zero tolerance for incorrect auto-posts
- Clear audit trail for all posting decisions

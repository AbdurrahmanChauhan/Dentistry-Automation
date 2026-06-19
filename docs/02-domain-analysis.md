# Domain Analysis

## Problem Statement

Dental practices lose revenue and staff hours to manual RCM workflows trapped in data silos. Staff manually check payer portals, post ERAs line-by-line, and chase denials without portfolio-level visibility.

## Root Causes

| Root Cause | Manifestation | Platform Response |
|------------|---------------|-------------------|
| No canonical model | Same patient/claim in 4 systems | DentalBridge → canonical store |
| Exception-default workflows | 80% volume treated as exceptions | Rules-first auto-path |
| Ack monitoring gaps | Claims never adjudicate | 999/277CA ingestion + alerts |
| ERA posting manual | Delayed cash, errors | 835 parser + match engine + HITL |
| Per-location blindness | Repeated denial patterns | Org-level CARC analytics |

## Automation Classification

### Fully Automated
- 270/271 request formatting
- Required-field claim scrubbing
- 999/277CA monitoring
- Idempotent job retries
- CARC→queue routing

### Partially Automated (AI + Rules)
- Benefit interpretation summaries
- EOB PDF extraction
- ERA line matching (confidence threshold)
- Denial next-action suggestions

### Human Required
- Appeal approval
- Complex COB resolution
- Write-offs above threshold
- Payer phone follow-up

### Remain Manual
- Treatment planning
- Clinical diagnosis
- Contract negotiation

## AI Suitability Matrix

| Workflow | AI Fit | Alternative |
|----------|--------|-------------|
| EOB PDF extraction | High | Template OCR (fails at scale) |
| Denial summarization | High | Static CARC lookup tables |
| X12 generation | None | Deterministic EDI library |
| Payment posting | Low | Rule engine + tolerance |
| Eligibility verified gate | None | 271 success only |

## Measurable Business Impact

- 20+ hours/week saved on payment posting (per DA marketing)
- Reduced days in AR via faster ERA ingestion
- Higher clean claim rate via pre-submit scrubbing
- Lower denial repeat rate via portfolio analytics

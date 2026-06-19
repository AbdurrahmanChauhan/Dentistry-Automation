# AI Architecture Document

## Principles

1. **Rules for money movement** — Never auto-post below confidence threshold
2. **AI for variation** — Unstructured documents, summarization, triage
3. **Human-in-the-loop** — All AI outputs reviewable; appeals require human approval
4. **Auditable** — Store model version, input hash, output, confidence, reviewer decision

## AI Use Cases (MVP)

| Use Case | Input | Output | Implementation |
|----------|-------|--------|----------------|
| EOB extraction | PDF/text file | Structured remittance lines | Python FastAPI heuristic + regex |
| Denial summary | CARC/RARC + context | Summary + suggested action | Rule table + template generation |
| Work priority | CARC code | Priority score 0–100 | Lookup table by denial type |

## Human-in-the-Loop Flow

```
Inbound → AI Extract → Schema Validate → Confidence Score
                                              │
                    ┌─────────────────────────┴─────────────────────────┐
                    ▼ ≥ 0.95                                              ▼ < 0.95
              Auto-Process                                          Exception Queue
                    │                                                     │
                    ▼                                                     ▼
              Audit Log                                            RCM Reviewer
```

## Thresholds

| Operation | Auto Threshold | Fallback |
|-----------|---------------|----------|
| ERA line match + post | ≥ 0.95 | Work item queue |
| EOB field extraction | ≥ 0.90 | Manual review |
| Eligibility "Verified" | 271 only (≥ 0.95) | NeedsReview status |
| Denial recommendations | Never auto | Human approve before resubmit |

## AI Worker Service

- **Runtime:** Python 3.12 + FastAPI + Uvicorn
- **Endpoints:** `POST /extract/eob`, `POST /summarize/denial`, `GET /health`
- **Fallback:** .NET `MockAiWorkerClient` when worker unavailable

## Future Enhancements

- Azure Document Intelligence for EOB OCR
- Azure OpenAI with BAA for complex denial letters
- ML classifier for work queue prioritization trained on historical resolutions
- Golden set regression (50+ EOB samples) in CI

## Governance

- No PHI in AI training data
- Log all inference requests with correlation IDs
- Monthly accuracy regression on golden set
- Model/prompt version tracking in `posting_attempts` and `work_items`

## Evaluation Strategy

| Metric | MVP Target |
|--------|------------|
| EOB field extraction accuracy | ≥ 92% |
| Denial classification agreement | ≥ 85% |
| Auto-post below threshold | 0 (invariant) |
| Hallucinated claim ID rate | 0 (caught by deterministic validation) |

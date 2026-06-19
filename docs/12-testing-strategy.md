# Testing Strategy

## Test Pyramid

```
        ┌─────────┐
        │  E2E    │  Playwright (future)
        ├─────────┤
        │ Integr. │  API contract tests (future)
        ├─────────┤
        │  Unit   │  xUnit + pytest (implemented)
        └─────────┘
```

## Unit Tests (.NET)

Location: `src/tests/RcmEngine.Tests/`

| Test Class | Coverage |
|------------|----------|
| `ClaimScrubServiceTests` | CDT validation, tooth requirements, invalid codes |
| `Edi835ParserTests` | JSON and X12-lite 835 parsing |
| `PaymentMatchEngineTests` | Auto-post confidence threshold |

Run: `dotnet test src/DentistryAutomation.sln`

## Unit Tests (Python)

Location: `src/ai-workers/tests/`

| Test | Coverage |
|------|----------|
| `test_health` | AI worker health endpoint |
| `test_denial_summarize` | CARC-based summarization |
| `test_eob_extract` | EOB text extraction |

Run: `pytest src/ai-workers/tests/ -v` (requires Python 3.12)

## Frontend Build Verification

```bash
cd src/platform-web && npm run build
```

TypeScript strict mode enabled; build serves as compile-time test.

## CI Pipeline

`.github/workflows/ci.yml` runs on push/PR:
1. `dotnet restore/build/test`
2. `pytest` (Python 3.12)
3. `npm ci && npm run build`
4. Docker image builds (API, AI, Web)

## Golden File Tests (Future)

- `samples/edi/sample-835.json` — JSON parser output validation
- `samples/edi/sample-835.x12` — X12 parser output validation
- 50+ EOB samples for AI extraction accuracy regression

## Invariant Tests (Critical)

| Invariant | Test Approach |
|-----------|---------------|
| No double-post | Idempotency key uniqueness constraint |
| No auto-post below 0.95 | `CanAutoPost` threshold unit test |
| Tenant isolation | Integration test with two orgs (future) |

## Performance Targets

| Metric | Target | Test Method |
|--------|--------|-------------|
| API read p95 | < 300ms | Load test (k6, future) |
| ERA processing | 500 lines/min | Batch benchmark (future) |
| AI extraction | < 5s per doc | Worker latency test (future) |

## Manual Test Checklist

- [ ] Health endpoint returns healthy
- [ ] Eligibility check returns Verified for valid member
- [ ] Claim scrub rejects missing tooth on D2391
- [ ] Claim submit generates ack event
- [ ] Remittance poll ingests 835 lines
- [ ] Auto-post writes to mock PMS
- [ ] Low-confidence line creates work item
- [ ] AI summary populates work item
- [ ] API rate limit returns 429 after 100 req/min

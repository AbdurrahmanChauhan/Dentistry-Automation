# Security Review

## Compliance Context

Dental practices are HIPAA covered entities. Dentistry Automation platform is a **Business Associate** requiring BAA with each customer and subcontractor chain.

## Threat Model

| Threat | Likelihood | Impact | Mitigation |
|--------|------------|--------|------------|
| Cross-tenant data leak | Medium | Critical | OrganizationId on all queries; integration tests |
| API key theft | Medium | High | SHA-256 hashed keys; rate limiting; usage logging |
| PHI in logs | Medium | Critical | No PHI in log messages; structured logging with entity IDs only |
| SQL injection | Low | Critical | EF Core parameterized queries |
| Unauthorized write-back | Medium | High | Idempotency keys; audit trail; confidence gates |
| AI PHI exposure | Medium | High | No training on tenant data; BAA with AI provider |
| Insider access abuse | Low | High | Audit events; role-based access |

## Technical Safeguards (Implemented)

| Control | Status | Implementation |
|---------|--------|----------------|
| Encryption in transit | Implemented | TLS (HTTPS in production) |
| Encryption at rest | Designed | Azure SQL TDE, Blob SSE |
| Access control | Implemented | API key auth; org-scoped queries |
| Audit logging | Implemented | `audit_events` table; API usage logs |
| Authentication | Implemented | X-Api-Key header; JWT-ready |
| Rate limiting | Implemented | AspNetCoreRateLimit 100 req/min |
| Secrets management | Designed | Azure Key Vault (production) |
| Session timeout | N/A (API) | Stateless API keys |

## HIPAA Checklist

- [x] Unique user/API key identification
- [x] Emergency access procedure (documented in runbook)
- [x] Automatic logoff (N/A for API; UI session timeout future)
- [x] Encryption and decryption (TLS + TDE design)
- [x] Audit controls (audit_events, api_usage_logs)
- [x] Integrity controls (idempotency, optimistic concurrency via EF)
- [x] Transmission security (TLS 1.2+)
- [ ] BAA templates (legal — not code)
- [ ] Risk assessment documentation (annual)
- [ ] SOC 2 Type II (future)

## Recommendations for Production

1. Enable Azure SQL TDE and Always Encrypted for SSN/member ID columns
2. Move API keys to Azure Key Vault; rotate quarterly
3. Add Entra ID for staff UI authentication
4. Implement field-level PHI masking in UI
5. Penetration test before first enterprise customer
6. Document incident response plan (72-hour breach notification)

## Risk Rating

**Overall: Medium-Low** for MVP with mock integrations. **Medium** when live PHI and production clearinghouse credentials are enabled.

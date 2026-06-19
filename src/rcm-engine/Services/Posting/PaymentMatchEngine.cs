using Microsoft.EntityFrameworkCore;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;

namespace RcmEngine.Services.Posting;

public record MatchResult(Guid? ClaimLineId, decimal ConfidenceScore, string? MatchReason);

public interface IPaymentMatchEngine
{
    Task<MatchResult> MatchLineAsync(Guid organizationId, RemittanceLine line, CancellationToken ct = default);
}

public class PaymentMatchEngine(RcmDbContext db) : IPaymentMatchEngine
{
    private const decimal AutoPostThreshold = 0.95m;
    private const decimal AmountTolerance = 0.01m;

    public async Task<MatchResult> MatchLineAsync(Guid organizationId, RemittanceLine line, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(line.PayerClaimId))
            return new MatchResult(null, 0, "No payer claim ID");

        var claim = await db.Claims
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId &&
                (c.PayerClaimId == line.PayerClaimId || c.ExternalClaimId == line.PayerClaimId), ct);

        if (claim == null)
            return new MatchResult(null, 0.1m, "No matching claim found");

        var claimLine = claim.Lines.FirstOrDefault(l =>
            l.ProcedureCode == line.ProcedureCode &&
            (!line.DateOfService.HasValue || claim.DateOfService == line.DateOfService));

        if (claimLine == null)
            return new MatchResult(null, 0.5m, "Claim found but procedure line mismatch");

        var amountMatch = Math.Abs(claimLine.ChargeAmount - line.BilledAmount) <= AmountTolerance ||
                          Math.Abs(claimLine.ChargeAmount - line.PaidAmount) <= AmountTolerance;

        var confidence = amountMatch ? 0.98m : 0.75m;
        return new MatchResult(claimLine.Id, confidence,
            amountMatch ? "Exact match on claim, procedure, and amount" : "Partial amount match");
    }

    public static bool CanAutoPost(decimal confidence) => confidence >= AutoPostThreshold;
}

public interface IRemittanceService
{
    Task<IReadOnlyList<Remittance>> IngestFromClearinghouseAsync(Guid organizationId, CancellationToken ct = default);
    Task<Remittance?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Remittance>> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task Process835FileAsync(Guid organizationId, string raw835, CancellationToken ct = default);
}

public interface IPostingService
{
    Task<PostingAttempt> PostRemittanceLineAsync(Guid remittanceLineId, bool forceManual = false, CancellationToken ct = default);
    Task<int> AutoPostRemittanceAsync(Guid remittanceId, CancellationToken ct = default);
}

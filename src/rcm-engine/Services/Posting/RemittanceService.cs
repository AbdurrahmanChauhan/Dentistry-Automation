using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;
using RcmEngine.Ports;
using RcmEngine.Services.Edi;
using RcmEngine.Services.WorkItems;

namespace RcmEngine.Services.Posting;

public class RemittanceService(
    RcmDbContext db,
    IClearinghousePort clearinghouse,
    IPaymentMatchEngine matchEngine,
    ILogger<RemittanceService> logger) : IRemittanceService
{
    public async Task<IReadOnlyList<Remittance>> IngestFromClearinghouseAsync(Guid organizationId, CancellationToken ct = default)
    {
        var remits = await clearinghouse.PollRemittancesAsync(ct);
        var results = new List<Remittance>();

        foreach (var r in remits)
        {
            var existing = await db.Remittances
                .AnyAsync(x => x.OrganizationId == organizationId && x.EraReference == r.EraReference, ct);
            if (existing) continue;

            var remittance = new Remittance
            {
                OrganizationId = organizationId,
                EraReference = r.EraReference,
                PayerName = r.PayerName,
                PayerId = r.PayerId,
                PaymentDate = r.PaymentDate,
                TotalPaymentAmount = r.TotalPaymentAmount,
                TraceNumber = r.TraceNumber,
                Raw835Payload = r.RawPayload,
                Status = RemittanceStatus.Received
            };

            foreach (var line in r.Lines)
            {
                var remLine = new RemittanceLine
                {
                    LineNumber = line.LineNumber,
                    PayerClaimId = line.PayerClaimId,
                    PatientControlNumber = line.PatientControlNumber,
                    ProcedureCode = line.ProcedureCode,
                    DateOfService = line.DateOfService,
                    BilledAmount = line.BilledAmount,
                    PaidAmount = line.PaidAmount,
                    AdjustmentAmount = line.AdjustmentAmount,
                    CarcCode = line.CarcCode,
                    RarcCode = line.RarcCode
                };

                var match = await matchEngine.MatchLineAsync(organizationId, remLine, ct);
                remLine.MatchedClaimLineId = match.ClaimLineId;
                remLine.MatchConfidence = match.ConfidenceScore;
                remittance.Lines.Add(remLine);
            }

            remittance.Status = remittance.Lines.All(l => PaymentMatchEngine.CanAutoPost(l.MatchConfidence))
                ? RemittanceStatus.Matched
                : RemittanceStatus.PartialMatch;

            db.Remittances.Add(remittance);
            results.Add(remittance);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Ingested {Count} remittances for org {OrgId}", results.Count, organizationId);
        return results;
    }

    public async Task Process835FileAsync(Guid organizationId, string raw835, CancellationToken ct = default)
    {
        var parsed = Edi835Parser.Parse(raw835);
        var remittance = new Remittance
        {
            OrganizationId = organizationId,
            EraReference = parsed.EraReference,
            PayerName = parsed.PayerName,
            PayerId = parsed.PayerId,
            PaymentDate = parsed.PaymentDate,
            TotalPaymentAmount = parsed.TotalPaymentAmount,
            TraceNumber = parsed.TraceNumber,
            Raw835Payload = raw835,
            Status = RemittanceStatus.Received,
            Source = "FileUpload"
        };

        foreach (var line in parsed.Lines)
        {
            var remLine = new RemittanceLine
            {
                LineNumber = line.LineNumber,
                PayerClaimId = line.PayerClaimId,
                ProcedureCode = line.ProcedureCode,
                DateOfService = line.DateOfService,
                BilledAmount = line.BilledAmount,
                PaidAmount = line.PaidAmount,
                AdjustmentAmount = line.AdjustmentAmount,
                CarcCode = line.CarcCode,
                RarcCode = line.RarcCode
            };
            var match = await matchEngine.MatchLineAsync(organizationId, remLine, ct);
            remLine.MatchedClaimLineId = match.ClaimLineId;
            remLine.MatchConfidence = match.ConfidenceScore;
            remittance.Lines.Add(remLine);
        }

        db.Remittances.Add(remittance);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Remittance?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Remittances
            .Include(r => r.Lines)
            .ThenInclude(l => l.PostingAttempts)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Remittance>> ListAsync(Guid organizationId, CancellationToken ct = default) =>
        await db.Remittances
            .Include(r => r.Lines)
            .Where(r => r.OrganizationId == organizationId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .ToListAsync(ct);
}

public class PostingService(
    RcmDbContext db,
    IPmsWriteBackPort pmsWriteBack,
    IWorkItemService workItems,
    IAuditService audit,
    ILogger<PostingService> logger) : IPostingService
{
    public async Task<PostingAttempt> PostRemittanceLineAsync(Guid remittanceLineId, bool forceManual = false, CancellationToken ct = default)
    {
        var line = await db.RemittanceLines
            .Include(l => l.Remittance)
            .FirstOrDefaultAsync(l => l.Id == remittanceLineId, ct)
            ?? throw new KeyNotFoundException($"Remittance line {remittanceLineId} not found");

        var remittance = line.Remittance!;
        var idempotencyKey = $"post-{line.Id}";

        var existing = await db.PostingAttempts
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey && p.Status == PostingStatus.AutoPosted, ct);
        if (existing != null) return existing;

        var canAutoPost = PaymentMatchEngine.CanAutoPost(line.MatchConfidence) || forceManual;

        if (!canAutoPost && !forceManual)
        {
            await workItems.CreateAsync(new WorkItemCreateRequest
            {
                OrganizationId = remittance.OrganizationId,
                LocationId = Guid.Empty,
                Type = WorkItemType.PostingException,
                Priority = WorkItemPriority.High,
                Title = $"Posting exception: {line.ProcedureCode}",
                Description = $"Match confidence {line.MatchConfidence:P0} below threshold",
                RemittanceId = remittance.Id,
                CarcCode = line.CarcCode,
                RarcCode = line.RarcCode
            }, ct);

            line.PostingStatus = PostingStatus.Skipped;
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Match confidence below auto-post threshold");
        }

        var location = await db.Locations
            .Where(l => l.OrganizationId == remittance.OrganizationId)
            .FirstOrDefaultAsync(ct);

        var attempt = new PostingAttempt
        {
            RemittanceLineId = line.Id,
            OrganizationId = remittance.OrganizationId,
            ConfidenceScore = line.MatchConfidence,
            IdempotencyKey = idempotencyKey,
            Status = PostingStatus.Pending
        };

        try
        {
            var success = await pmsWriteBack.WritePaymentAsync(new PaymentWriteBackRequest(
                location?.Name ?? "Demo Office",
                location?.ExternalClinicId ?? "CLINIC-001",
                idempotencyKey,
                line.PatientControlNumber ?? "UNKNOWN",
                line.PaidAmount,
                remittance.PaymentDate,
                line.DateOfService,
                null, null, null), ct);

            attempt.Status = forceManual ? PostingStatus.ManualApproved : PostingStatus.AutoPosted;
            attempt.PmsWriteBackReference = success ? $"WB-{Guid.NewGuid():N}" : null;
            line.PostingStatus = attempt.Status;

            if (!string.IsNullOrEmpty(line.CarcCode) && line.PaidAmount == 0)
            {
                await workItems.CreateDenialFromRemittanceLineAsync(remittance, line, ct);
            }
        }
        catch (Exception ex)
        {
            attempt.Status = PostingStatus.Failed;
            attempt.ErrorMessage = ex.Message;
            line.PostingStatus = PostingStatus.Failed;
            logger.LogError(ex, "Posting failed for line {LineId}", line.Id);
        }

        db.PostingAttempts.Add(attempt);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(remittance.OrganizationId, location?.Id, "system", "System",
            "Payment.Posted", "RemittanceLine", line.Id.ToString(),
            $"Amount {line.PaidAmount}", null, ct);

        return attempt;
    }

    public async Task<int> AutoPostRemittanceAsync(Guid remittanceId, CancellationToken ct = default)
    {
        var remittance = await db.Remittances
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == remittanceId, ct)
            ?? throw new KeyNotFoundException($"Remittance {remittanceId} not found");

        var posted = 0;
        foreach (var line in remittance.Lines.Where(l => l.PostingStatus == PostingStatus.Pending))
        {
            if (!PaymentMatchEngine.CanAutoPost(line.MatchConfidence)) continue;
            try
            {
                await PostRemittanceLineAsync(line.Id, forceManual: false, ct);
                posted++;
            }
            catch
            {
                // Exception queue created in PostRemittanceLineAsync
            }
        }

        remittance.Status = remittance.Lines.All(l => l.PostingStatus is PostingStatus.AutoPosted or PostingStatus.ManualApproved)
            ? RemittanceStatus.Posted
            : RemittanceStatus.Exception;
        await db.SaveChangesAsync(ct);
        return posted;
    }
}

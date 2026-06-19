using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;
using RcmEngine.Ports;
using RcmEngine.Services.Rules;

namespace RcmEngine.Services.Claims;

public class ClaimSubmissionService(
    RcmDbContext db,
    IDentalBridgeClient dentalBridge,
    IClearinghousePort clearinghouse,
    IClaimScrubService scrubService,
    IAuditService audit,
    ILogger<ClaimSubmissionService> logger) : IClaimSubmissionService
{
    public async Task<Claim> IngestFromDentalBridgeAsync(Guid organizationId, Guid locationId, CancellationToken ct = default)
    {
        var location = await db.Locations.FindAsync([locationId], ct)
            ?? throw new KeyNotFoundException($"Location {locationId} not found");

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var to = DateOnly.FromDateTime(DateTime.UtcNow);
        var procedures = await dentalBridge.GetClaimProceduresAsync(location.ExternalClinicId, from, to, ct);

        Claim? lastClaim = null;
        var grouped = procedures.GroupBy(p => p.ClaimId);

        foreach (var group in grouped)
        {
            var first = group.First();
            var patient = await db.Patients
                .FirstOrDefaultAsync(p => p.OrganizationId == organizationId &&
                    p.ExternalPatientId == first.PatientId, ct);

            if (patient == null)
            {
                patient = new Patient
                {
                    OrganizationId = organizationId,
                    LocationId = locationId,
                    ExternalPatientId = first.PatientId,
                    FirstName = "Unknown",
                    LastName = first.PatientId
                };
                db.Patients.Add(patient);
                await db.SaveChangesAsync(ct);
            }

            var existing = await db.Claims
                .Include(c => c.Lines)
                .FirstOrDefaultAsync(c => c.OrganizationId == organizationId &&
                    c.ExternalClaimId == first.ClaimId, ct);

            if (existing != null)
            {
                lastClaim = existing;
                continue;
            }

            var claim = new Claim
            {
                OrganizationId = organizationId,
                LocationId = locationId,
                PatientId = patient.Id,
                ExternalClaimId = first.ClaimId,
                PayerId = first.PayerId,
                PayerName = first.PayerName,
                DateOfService = first.DateOfService,
                Status = ClaimStatus.Draft,
                TotalChargeAmount = group.Sum(g => g.ChargeAmount)
            };

            var lineNum = 1;
            foreach (var proc in group)
            {
                claim.Lines.Add(new ClaimLine
                {
                    LineNumber = lineNum++,
                    ProcedureCode = proc.ProcedureCode,
                    ToothNumber = proc.ToothNumber,
                    Surface = proc.Surface,
                    ChargeAmount = proc.ChargeAmount
                });
            }

            db.Claims.Add(claim);
            lastClaim = claim;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Ingested claims from DentalBridge for location {LocationId}", locationId);
        return lastClaim ?? throw new InvalidOperationException("No claims found to ingest");
    }

    public async Task<Claim> ScrubAndSubmitAsync(Guid claimId, CancellationToken ct = default)
    {
        var claim = await db.Claims
            .Include(c => c.Lines)
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct)
            ?? throw new KeyNotFoundException($"Claim {claimId} not found");

        var scrub = scrubService.Scrub(claim);
        if (!scrub.Passed)
        {
            claim.Status = ClaimStatus.ScrubFailed;
            claim.ScrubErrorsJson = JsonSerializer.Serialize(scrub.Errors);
            claim.UpdatedAt = DateTime.UtcNow;
            AddStatusEvent(claim, ClaimStatus.ScrubFailed, "RulesEngine", string.Join("; ", scrub.Errors));
            await db.SaveChangesAsync(ct);
            return claim;
        }

        claim.Status = ClaimStatus.ReadyToSubmit;
        claim.ScrubErrorsJson = null;

        var payload = JsonSerializer.Serialize(new
        {
            claim.ExternalClaimId,
            claim.PayerId,
            claim.PayerName,
            claim.DateOfService,
            claim.TotalChargeAmount,
            Lines = claim.Lines.Select(l => new { l.ProcedureCode, l.ToothNumber, l.ChargeAmount })
        });

        var result = await clearinghouse.Submit837DAsync(payload, ct);

        var submission = new ClaimSubmission
        {
            ClaimId = claim.Id,
            OrganizationId = claim.OrganizationId,
            ClearinghouseReference = result.ReferenceId,
            Status = result.Status,
            Raw837Payload = payload
        };
        db.ClaimSubmissions.Add(submission);

        claim.Status = ClaimStatus.Submitted;
        claim.PayerClaimId = result.ReferenceId;
        AddStatusEvent(claim, ClaimStatus.Submitted, "Clearinghouse", result.ReferenceId);
        claim.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(claim.OrganizationId, claim.LocationId, "system", "System",
            "Claim.Submitted", "Claim", claim.Id.ToString(), result.ReferenceId, null, ct);

        return claim;
    }

    public async Task<IReadOnlyList<Claim>> ListAsync(Guid organizationId, ClaimStatus? status, CancellationToken ct = default)
    {
        var query = db.Claims
            .Include(c => c.Lines)
            .Include(c => c.Patient)
            .Where(c => c.OrganizationId == organizationId);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        return await query.OrderByDescending(c => c.CreatedAt).Take(100).ToListAsync(ct);
    }

    public async Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Claims
            .Include(c => c.Lines)
            .Include(c => c.Patient)
            .Include(c => c.StatusHistory)
            .Include(c => c.Submissions)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    private static void AddStatusEvent(Claim claim, ClaimStatus status, string source, string? details)
    {
        claim.StatusHistory.Add(new ClaimStatusEvent
        {
            ClaimId = claim.Id,
            Status = status,
            Source = source,
            Details = details
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;
using RcmEngine.Ports;

namespace RcmEngine.Services.Eligibility;

public interface IEligibilityOrchestrator
{
    Task<EligibilityCheck> TriggerCheckAsync(Guid organizationId, Guid locationId, Guid patientId,
        Guid? coverageId, Guid? appointmentId, CancellationToken ct = default);
    Task ProcessPendingChecksAsync(CancellationToken ct = default);
    Task<EligibilityCheck?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EligibilityCheck>> ListAsync(Guid organizationId, Guid? locationId, CancellationToken ct = default);
}

public class EligibilityOrchestrator(
    RcmDbContext db,
    IClearinghousePort clearinghouse,
    IPmsWriteBackPort pmsWriteBack,
    IAuditService audit,
    ILogger<EligibilityOrchestrator> logger) : IEligibilityOrchestrator
{
    private const decimal VerifiedThreshold = 0.95m;

    public async Task<EligibilityCheck> TriggerCheckAsync(Guid organizationId, Guid locationId, Guid patientId,
        Guid? coverageId, Guid? appointmentId, CancellationToken ct = default)
    {
        var patient = await db.Patients.FindAsync([patientId], ct)
            ?? throw new KeyNotFoundException($"Patient {patientId} not found");

        Coverage? coverage = null;
        if (coverageId.HasValue)
            coverage = await db.Coverages.FindAsync([coverageId.Value], ct);
        else
            coverage = await db.Coverages
                .Where(c => c.PatientId == patientId && c.IsActive)
                .OrderBy(c => c.CoverageOrder)
                .FirstOrDefaultAsync(ct);

        var check = new EligibilityCheck
        {
            OrganizationId = organizationId,
            LocationId = locationId,
            PatientId = patientId,
            CoverageId = coverage?.Id,
            AppointmentId = appointmentId,
            Status = EligibilityStatus.Pending,
            PayerId = coverage?.PayerId,
            MemberId = coverage?.MemberId ?? patient.MemberId,
            Source = "270/271"
        };

        db.EligibilityChecks.Add(check);
        await db.SaveChangesAsync(ct);

        await audit.LogAsync(organizationId, locationId, "system", "System",
            "EligibilityCheck.Created", "EligibilityCheck", check.Id.ToString(),
            $"Patient {patientId}", null, ct);

        await ProcessCheckAsync(check, patient, coverage, ct);
        return check;
    }

    public async Task ProcessPendingChecksAsync(CancellationToken ct = default)
    {
        var pending = await db.EligibilityChecks
            .Include(e => e.Patient)
            .Where(e => e.Status == EligibilityStatus.Pending)
            .Take(50)
            .ToListAsync(ct);

        foreach (var check in pending)
        {
            var coverage = check.CoverageId.HasValue
                ? await db.Coverages.FindAsync([check.CoverageId.Value], ct)
                : null;
            await ProcessCheckAsync(check, check.Patient!, coverage, ct);
        }
    }

    private async Task ProcessCheckAsync(EligibilityCheck check, Patient patient, Coverage? coverage, CancellationToken ct)
    {
        check.Status = EligibilityStatus.InProgress;
        check.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            var location = await db.Locations.FindAsync([check.LocationId], ct);
            var provider = await db.Providers
                .Where(p => p.LocationId == check.LocationId && p.IsActive)
                .FirstOrDefaultAsync(ct);

            var request = new Eligibility270Request(
                coverage?.PayerId ?? check.PayerId ?? "UNKNOWN",
                coverage?.MemberId ?? check.MemberId ?? "",
                provider?.Npi ?? "1234567890",
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth ?? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)));

            var response = await clearinghouse.CheckEligibilityAsync(request, ct);
            check.ConfidenceScore = response.ConfidenceScore;
            check.CompletedAt = DateTime.UtcNow;

            if (response.IsEligible && response.ConfidenceScore >= VerifiedThreshold)
            {
                check.Status = EligibilityStatus.Verified;
                check.BenefitSummary = $"Plan: {response.PlanName}. Remaining max: ${response.AnnualMaximumRemaining:N2}";

                var snapshot = new BenefitSnapshot
                {
                    EligibilityCheckId = check.Id,
                    OrganizationId = check.OrganizationId,
                    IsActive = true,
                    AnnualMaximum = response.AnnualMaximum,
                    AnnualMaximumRemaining = response.AnnualMaximumRemaining,
                    Deductible = response.Deductible,
                    DeductibleRemaining = response.DeductibleRemaining,
                    CoinsurancePercent = response.CoinsurancePercent,
                    PlanName = response.PlanName,
                    Raw271Json = response.RawJson,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                db.BenefitSnapshots.Add(snapshot);

                if (location != null)
                {
                    await pmsWriteBack.WriteEligibilityAsync(new EligibilityWriteBackRequest(
                        location.Name, location.ExternalClinicId, patient.ExternalPatientId,
                        "Verified", check.BenefitSummary), ct);
                }
            }
            else if (response.IsEligible)
            {
                check.Status = EligibilityStatus.NeedsReview;
                check.BenefitSummary = response.PlanName;
            }
            else
            {
                check.Status = EligibilityStatus.Failed;
                check.ErrorMessage = "Patient not eligible or coverage inactive";
            }

            check.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Eligibility check {CheckId} completed with status {Status}", check.Id, check.Status);
        }
        catch (Exception ex)
        {
            check.Status = EligibilityStatus.Failed;
            check.ErrorMessage = ex.Message;
            check.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            logger.LogError(ex, "Eligibility check {CheckId} failed", check.Id);
        }
    }

    public async Task<EligibilityCheck?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.EligibilityChecks
            .Include(e => e.BenefitSnapshot)
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<EligibilityCheck>> ListAsync(Guid organizationId, Guid? locationId, CancellationToken ct = default)
    {
        var query = db.EligibilityChecks
            .Include(e => e.Patient)
            .Include(e => e.BenefitSnapshot)
            .Where(e => e.OrganizationId == organizationId);

        if (locationId.HasValue)
            query = query.Where(e => e.LocationId == locationId.Value);

        return await query.OrderByDescending(e => e.CreatedAt).Take(100).ToListAsync(ct);
    }
}

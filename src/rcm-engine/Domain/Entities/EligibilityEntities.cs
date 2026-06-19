namespace RcmEngine.Domain.Entities;

public enum EligibilityStatus
{
    Pending,
    InProgress,
    Verified,
    Failed,
    NeedsReview
}

public class EligibilityCheck : Common.LocationScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid? CoverageId { get; set; }
    public Guid? AppointmentId { get; set; }
    public EligibilityStatus Status { get; set; } = EligibilityStatus.Pending;
    public string? PayerId { get; set; }
    public string? MemberId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? Source { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? BenefitSummary { get; set; }

    public Patient? Patient { get; set; }
    public BenefitSnapshot? BenefitSnapshot { get; set; }
}

public class BenefitSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EligibilityCheckId { get; set; }
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; set; }
    public decimal? AnnualMaximum { get; set; }
    public decimal? AnnualMaximumRemaining { get; set; }
    public decimal? Deductible { get; set; }
    public decimal? DeductibleRemaining { get; set; }
    public decimal? CoinsurancePercent { get; set; }
    public string? PlanName { get; set; }
    public string? Raw271Json { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public EligibilityCheck? EligibilityCheck { get; set; }
}

namespace RcmEngine.Domain.Entities;

public enum ClaimStatus
{
    Draft,
    ScrubFailed,
    ReadyToSubmit,
    Submitted,
    AckAccepted,
    AckRejected,
    Adjudicated,
    Paid,
    Denied,
    Closed
}

public class Claim : Common.LocationScopedEntity
{
    public Guid PatientId { get; set; }
    public Guid? ProviderId { get; set; }
    public string ExternalClaimId { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public DateOnly DateOfService { get; set; }
    public decimal TotalChargeAmount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? PayerClaimId { get; set; }
    public string? ScrubErrorsJson { get; set; }

    public Patient? Patient { get; set; }
    public Provider? Provider { get; set; }
    public ICollection<ClaimLine> Lines { get; set; } = [];
    public ICollection<ClaimStatusEvent> StatusHistory { get; set; } = [];
    public ICollection<ClaimSubmission> Submissions { get; set; } = [];
}

public class ClaimLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public int LineNumber { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public string? ToothNumber { get; set; }
    public string? Surface { get; set; }
    public decimal ChargeAmount { get; set; }
    public decimal? PaidAmount { get; set; }

    public Claim? Claim { get; set; }
}

public class ClaimSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public Guid OrganizationId { get; set; }
    public string ClearinghouseReference { get; set; } = string.Empty;
    public string Status { get; set; } = "Submitted";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string? Raw837Payload { get; set; }

    public Claim? Claim { get; set; }
}

public class ClaimStatusEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClaimId { get; set; }
    public ClaimStatus Status { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Claim? Claim { get; set; }
}

public class AckEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid? ClaimId { get; set; }
    public string AckType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public string? RawPayload { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}

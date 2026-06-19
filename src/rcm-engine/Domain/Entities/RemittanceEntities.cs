namespace RcmEngine.Domain.Entities;

public enum RemittanceStatus
{
    Received,
    Parsing,
    Matched,
    PartialMatch,
    Posted,
    Exception
}

public enum PostingStatus
{
    Pending,
    AutoPosted,
    ManualApproved,
    Failed,
    Skipped
}

public class Remittance : Common.TenantEntity
{
    public string EraReference { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public DateOnly PaymentDate { get; set; }
    public decimal TotalPaymentAmount { get; set; }
    public string? TraceNumber { get; set; }
    public RemittanceStatus Status { get; set; } = RemittanceStatus.Received;
    public string? Raw835Payload { get; set; }
    public string Source { get; set; } = "Clearinghouse";

    public ICollection<RemittanceLine> Lines { get; set; } = [];
}

public class RemittanceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RemittanceId { get; set; }
    public int LineNumber { get; set; }
    public string? PayerClaimId { get; set; }
    public string? PatientControlNumber { get; set; }
    public string ProcedureCode { get; set; } = string.Empty;
    public DateOnly? DateOfService { get; set; }
    public decimal BilledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal AdjustmentAmount { get; set; }
    public string? CarcCode { get; set; }
    public string? RarcCode { get; set; }
    public decimal MatchConfidence { get; set; }
    public Guid? MatchedClaimLineId { get; set; }
    public PostingStatus PostingStatus { get; set; } = PostingStatus.Pending;

    public Remittance? Remittance { get; set; }
    public ICollection<PostingAttempt> PostingAttempts { get; set; } = [];
}

public class PostingAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RemittanceLineId { get; set; }
    public Guid OrganizationId { get; set; }
    public PostingStatus Status { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PmsWriteBackReference { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public RemittanceLine? RemittanceLine { get; set; }
}

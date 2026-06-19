namespace RcmEngine.Domain.Entities;

public enum WorkItemType
{
    EligibilityReview,
    ScrubFailure,
    AckRejection,
    PostingException,
    Denial,
    Underpayment,
    General
}

public enum WorkItemStatus
{
    Open,
    Assigned,
    InReview,
    Resolved,
    Escalated
}

public enum WorkItemPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class WorkItem : Common.LocationScopedEntity
{
    public WorkItemType Type { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.Open;
    public WorkItemPriority Priority { get; set; } = WorkItemPriority.Medium;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AiSummary { get; set; }
    public string? SuggestedAction { get; set; }
    public string? CarcCode { get; set; }
    public string? RarcCode { get; set; }
    public Guid? ClaimId { get; set; }
    public Guid? RemittanceId { get; set; }
    public Guid? EligibilityCheckId { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }

    public Claim? Claim { get; set; }
    public Remittance? Remittance { get; set; }
}

public enum IntegrationJobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    DeadLetter
}

public class IntegrationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public IntegrationJobStatus Status { get; set; } = IntegrationJobStatus.Queued;
    public string? PayloadJson { get; set; }
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid? LocationId { get; set; }
    public string ActorId { get; set; } = string.Empty;
    public string ActorType { get; set; } = "User";
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scopes { get; set; } = "read,write";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public long UsageCount { get; set; }

    public Organization? Organization { get; set; }
}

public class ApiUsageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ApiKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Appointment : Common.LocationScopedEntity
{
    public string ExternalAppointmentId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = "Scheduled";
    public bool EligibilityTriggered { get; set; }

    public Patient? Patient { get; set; }
}

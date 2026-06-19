using Microsoft.EntityFrameworkCore;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;

namespace RcmEngine.Services;

public interface IAuditService
{
    Task LogAsync(Guid organizationId, Guid? locationId, string actorId, string actorType,
        string action, string entityType, string? entityId, string? details, string? ipAddress,
        CancellationToken ct = default);
}

public class AuditService(RcmDbContext db) : IAuditService
{
    public async Task LogAsync(Guid organizationId, Guid? locationId, string actorId, string actorType,
        string action, string entityType, string? entityId, string? details, string? ipAddress,
        CancellationToken ct = default)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            OrganizationId = organizationId,
            LocationId = locationId,
            ActorId = actorId,
            ActorType = actorType,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        });
        await db.SaveChangesAsync(ct);
    }
}

public interface IIntegrationJobService
{
    Task<IntegrationJob> EnqueueAsync(Guid organizationId, string jobType, string? payloadJson,
        string? idempotencyKey, CancellationToken ct = default);
    Task CompleteAsync(Guid jobId, string? resultJson, CancellationToken ct = default);
    Task FailAsync(Guid jobId, string error, CancellationToken ct = default);
}

public class IntegrationJobService(RcmDbContext db) : IIntegrationJobService
{
    public async Task<IntegrationJob> EnqueueAsync(Guid organizationId, string jobType, string? payloadJson,
        string? idempotencyKey, CancellationToken ct = default)
    {
        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var existing = await db.IntegrationJobs
                .FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey, ct);
            if (existing != null) return existing;
        }

        var job = new IntegrationJob
        {
            OrganizationId = organizationId,
            JobType = jobType,
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey,
            Status = IntegrationJobStatus.Queued
        };
        db.IntegrationJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job;
    }

    public async Task CompleteAsync(Guid jobId, string? resultJson, CancellationToken ct = default)
    {
        var job = await db.IntegrationJobs.FindAsync([jobId], ct)
            ?? throw new KeyNotFoundException($"Job {jobId} not found");
        job.Status = IntegrationJobStatus.Completed;
        job.ResultJson = resultJson;
        job.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task FailAsync(Guid jobId, string error, CancellationToken ct = default)
    {
        var job = await db.IntegrationJobs.FindAsync([jobId], ct)
            ?? throw new KeyNotFoundException($"Job {jobId} not found");
        job.AttemptCount++;
        job.ErrorMessage = error;
        job.Status = job.AttemptCount >= job.MaxAttempts
            ? IntegrationJobStatus.DeadLetter
            : IntegrationJobStatus.Failed;
        await db.SaveChangesAsync(ct);
    }
}

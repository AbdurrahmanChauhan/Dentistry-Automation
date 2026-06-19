using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;
using RcmEngine.Ports;

namespace RcmEngine.Services.WorkItems;

public record WorkItemCreateRequest
{
    public required Guid OrganizationId { get; init; }
    public required Guid LocationId { get; init; }
    public required WorkItemType Type { get; init; }
    public WorkItemPriority Priority { get; init; } = WorkItemPriority.Medium;
    public required string Title { get; init; }
    public required string Description { get; init; }
    public Guid? ClaimId { get; init; }
    public Guid? RemittanceId { get; init; }
    public Guid? EligibilityCheckId { get; init; }
    public string? CarcCode { get; init; }
    public string? RarcCode { get; init; }
    public string? AiSummary { get; init; }
    public string? SuggestedAction { get; init; }
}

public interface IWorkItemService
{
    Task<WorkItem> CreateAsync(WorkItemCreateRequest request, CancellationToken ct = default);
    Task CreateDenialFromRemittanceLineAsync(Remittance remittance, RemittanceLine line, CancellationToken ct = default);
    Task<IReadOnlyList<WorkItem>> ListAsync(Guid organizationId, WorkItemStatus? status, WorkItemType? type, CancellationToken ct = default);
    Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WorkItem> UpdateAsync(Guid id, WorkItemStatus? status, string? assignedTo, string? resolutionNotes, CancellationToken ct = default);
    Task EnrichWithAiSummaryAsync(Guid workItemId, IAiWorkerClient aiClient, CancellationToken ct = default);
}

public class WorkItemService(RcmDbContext db, ILogger<WorkItemService> logger) : IWorkItemService
{
    private static readonly Dictionary<string, WorkItemPriority> CarcPriority = new()
    {
        ["CO-45"] = WorkItemPriority.Medium,
        ["PR-1"] = WorkItemPriority.Low,
        ["CO-97"] = WorkItemPriority.High,
        ["OA-23"] = WorkItemPriority.Critical
    };

    public async Task<WorkItem> CreateAsync(WorkItemCreateRequest request, CancellationToken ct = default)
    {
        var priority = request.CarcCode != null && CarcPriority.TryGetValue(request.CarcCode, out var p)
            ? p : request.Priority;

        var item = new WorkItem
        {
            OrganizationId = request.OrganizationId,
            LocationId = request.LocationId,
            Type = request.Type,
            Status = WorkItemStatus.Open,
            Priority = priority,
            Title = request.Title,
            Description = request.Description,
            ClaimId = request.ClaimId,
            RemittanceId = request.RemittanceId,
            EligibilityCheckId = request.EligibilityCheckId,
            CarcCode = request.CarcCode,
            RarcCode = request.RarcCode,
            AiSummary = request.AiSummary,
            SuggestedAction = request.SuggestedAction
        };

        db.WorkItems.Add(item);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created work item {Id} type {Type}", item.Id, item.Type);
        return item;
    }

    public async Task CreateDenialFromRemittanceLineAsync(Remittance remittance, RemittanceLine line, CancellationToken ct = default)
    {
        await CreateAsync(new WorkItemCreateRequest
        {
            OrganizationId = remittance.OrganizationId,
            LocationId = Guid.Empty,
            Type = WorkItemType.Denial,
            Priority = WorkItemPriority.High,
            Title = $"Denial: {line.ProcedureCode} - {line.CarcCode}",
            Description = $"Claim {line.PayerClaimId} denied. Paid: ${line.PaidAmount}, Billed: ${line.BilledAmount}",
            RemittanceId = remittance.Id,
            CarcCode = line.CarcCode,
            RarcCode = line.RarcCode
        }, ct);
    }

    public async Task<IReadOnlyList<WorkItem>> ListAsync(Guid organizationId, WorkItemStatus? status, WorkItemType? type, CancellationToken ct = default)
    {
        var query = db.WorkItems
            .Where(w => w.OrganizationId == organizationId);

        if (status.HasValue) query = query.Where(w => w.Status == status.Value);
        if (type.HasValue) query = query.Where(w => w.Type == type.Value);

        return await query
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .Take(200)
            .ToListAsync(ct);
    }

    public async Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.WorkItems
            .Include(w => w.Claim)
            .Include(w => w.Remittance)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<WorkItem> UpdateAsync(Guid id, WorkItemStatus? status, string? assignedTo, string? resolutionNotes, CancellationToken ct = default)
    {
        var item = await db.WorkItems.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"WorkItem {id} not found");

        if (status.HasValue)
        {
            item.Status = status.Value;
            if (status.Value == WorkItemStatus.Resolved)
                item.ResolvedAt = DateTime.UtcNow;
        }
        if (assignedTo != null) item.AssignedTo = assignedTo;
        if (resolutionNotes != null) item.ResolutionNotes = resolutionNotes;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task EnrichWithAiSummaryAsync(Guid workItemId, IAiWorkerClient aiClient, CancellationToken ct = default)
    {
        var item = await GetByIdAsync(workItemId, ct)
            ?? throw new KeyNotFoundException($"WorkItem {workItemId} not found");

        var result = await aiClient.SummarizeDenialAsync(new DenialSummaryRequest(
            item.CarcCode ?? "UNKNOWN",
            item.RarcCode,
            item.Description,
            0, 0), ct);

        item.AiSummary = result.Summary;
        item.SuggestedAction = result.SuggestedAction;
        item.Priority = result.PriorityScore switch
        {
            >= 80 => WorkItemPriority.Critical,
            >= 60 => WorkItemPriority.High,
            >= 40 => WorkItemPriority.Medium,
            _ => WorkItemPriority.Low
        };
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}

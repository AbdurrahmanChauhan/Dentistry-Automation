using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;
using RcmEngine.Ports;
using RcmEngine.Services.WorkItems;

namespace RcmEngine.Services.Ack;

public interface IAckMonitoringService
{
    Task ProcessAcksAsync(CancellationToken ct = default);
}

public class AckMonitoringService(
    RcmDbContext db,
    IClearinghousePort clearinghouse,
    IWorkItemService workItems,
    ILogger<AckMonitoringService> logger) : IAckMonitoringService
{
    public async Task ProcessAcksAsync(CancellationToken ct = default)
    {
        var acks = await clearinghouse.PollAcksAsync(ct);

        foreach (var ack in acks)
        {
            Claim? claim = null;
            if (!string.IsNullOrEmpty(ack.ClaimReference))
            {
                claim = await db.Claims
                    .FirstOrDefaultAsync(c => c.PayerClaimId == ack.ClaimReference ||
                        c.ExternalClaimId == ack.ClaimReference, ct);
            }

            db.AckEvents.Add(new AckEvent
            {
                OrganizationId = claim?.OrganizationId ?? Guid.Empty,
                ClaimId = claim?.Id,
                AckType = ack.AckType,
                Status = ack.Status,
                RejectReason = ack.RejectReason,
                RawPayload = ack.RawPayload
            });

            if (claim != null)
            {
                if (ack.Status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    claim.Status = ClaimStatus.AckAccepted;
                    claim.StatusHistory.Add(new ClaimStatusEvent
                    {
                        ClaimId = claim.Id,
                        Status = ClaimStatus.AckAccepted,
                        Source = ack.AckType,
                        Details = "Claim acknowledged by payer"
                    });
                }
                else
                {
                    claim.Status = ClaimStatus.AckRejected;
                    claim.StatusHistory.Add(new ClaimStatusEvent
                    {
                        ClaimId = claim.Id,
                        Status = ClaimStatus.AckRejected,
                        Source = ack.AckType,
                        Details = ack.RejectReason
                    });

                    await workItems.CreateAsync(new WorkItemCreateRequest
                    {
                        OrganizationId = claim.OrganizationId,
                        LocationId = claim.LocationId,
                        Type = WorkItemType.AckRejection,
                        Priority = WorkItemPriority.High,
                        Title = $"Ack rejection: {claim.ExternalClaimId}",
                        Description = ack.RejectReason ?? "Claim rejected at acknowledgment",
                        ClaimId = claim.Id
                    }, ct);
                }

                claim.UpdatedAt = DateTime.UtcNow;
            }

            logger.LogInformation("Processed ack {AckType} status {Status} for claim ref {Ref}",
                ack.AckType, ack.Status, ack.ClaimReference);
        }

        await db.SaveChangesAsync(ct);
    }
}

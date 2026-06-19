using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformApi.Auth;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;
using RcmEngine.Ports;
using RcmEngine.Services.WorkItems;

namespace PlatformApi.Controllers;

[ApiController]
[Route("v1/work-items")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class WorkItemsController(IWorkItemService workItems, IAiWorkerClient aiClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] string? type, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        WorkItemStatus? ws = status != null && Enum.TryParse<WorkItemStatus>(status, true, out var s) ? s : null;
        WorkItemType? wt = type != null && Enum.TryParse<WorkItemType>(type, true, out var t) ? t : null;
        var items = await workItems.ListAsync(orgId, ws, wt, ct);
        return Ok(items.Select(MapItem));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await workItems.GetByIdAsync(id, ct);
        if (item == null) return NotFound();
        return Ok(MapItem(item));
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkItemRequest request, CancellationToken ct)
    {
        WorkItemStatus? status = request.Status != null && Enum.TryParse<WorkItemStatus>(request.Status, true, out var s) ? s : null;
        var item = await workItems.UpdateAsync(id, status, request.AssignedTo, request.ResolutionNotes, ct);
        return Ok(MapItem(item));
    }

    [HttpPost("{id:guid}/ai-summary")]
    public async Task<IActionResult> GenerateAiSummary(Guid id, CancellationToken ct)
    {
        await workItems.EnrichWithAiSummaryAsync(id, aiClient, ct);
        var item = await workItems.GetByIdAsync(id, ct);
        return Ok(MapItem(item!));
    }

    private static object MapItem(WorkItem w) => new
    {
        w.Id,
        Type = w.Type.ToString(),
        Status = w.Status.ToString(),
        Priority = w.Priority.ToString(),
        w.Title,
        w.Description,
        w.AiSummary,
        w.SuggestedAction,
        w.CarcCode,
        w.RarcCode,
        w.ClaimId,
        w.RemittanceId,
        w.AssignedTo,
        w.CreatedAt,
        w.ResolvedAt,
        w.ResolutionNotes
    };
}

[ApiController]
[Route("v1/locations")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class LocationsController(RcmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var locations = await db.Locations.Where(l => l.OrganizationId == orgId).ToListAsync(ct);
        return Ok(locations.Select(l => new { l.Id, l.Name, l.ExternalClinicId, l.PmsType, l.Region }));
    }

    [HttpGet("{id:guid}/kpis")]
    public async Task<IActionResult> GetKpis(Guid id, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var claims = await db.Claims.Where(c => c.OrganizationId == orgId && c.LocationId == id).ToListAsync(ct);
        var workItems = await db.WorkItems.Where(w => w.OrganizationId == orgId && w.LocationId == id).ToListAsync(ct);
        var eligibility = await db.EligibilityChecks.Where(e => e.OrganizationId == orgId && e.LocationId == id).ToListAsync(ct);
        var remittances = await db.Remittances.Where(r => r.OrganizationId == orgId).ToListAsync(ct);

        var denialRate = claims.Count > 0
            ? (decimal)claims.Count(c => c.Status == ClaimStatus.Denied) / claims.Count
            : 0;

        var verifiedRate = eligibility.Count > 0
            ? (decimal)eligibility.Count(e => e.Status == EligibilityStatus.Verified) / eligibility.Count
            : 0;

        return Ok(new
        {
            locationId = id,
            totalClaims = claims.Count,
            openWorkItems = workItems.Count(w => w.Status == WorkItemStatus.Open),
            denialRate = Math.Round(denialRate * 100, 1),
            eligibilityVerifiedRate = Math.Round(verifiedRate * 100, 1),
            remittancesReceived = remittances.Count,
            claimsSubmitted = claims.Count(c => c.Status >= ClaimStatus.Submitted)
        });
    }
}

[ApiController]
[Route("v1/patients")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class PatientsController(RcmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? locationId, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var query = db.Patients.Where(p => p.OrganizationId == orgId);
        if (locationId.HasValue) query = query.Where(p => p.LocationId == locationId.Value);
        var patients = await query.Take(100).ToListAsync(ct);
        return Ok(patients.Select(p => new { p.Id, p.FirstName, p.LastName, p.ExternalPatientId, p.LocationId }));
    }
}

public record UpdateWorkItemRequest(string? Status, string? AssignedTo, string? ResolutionNotes);

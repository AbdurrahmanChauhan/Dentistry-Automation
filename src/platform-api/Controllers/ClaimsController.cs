using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformApi.Auth;
using RcmEngine.Domain.Entities;
using RcmEngine.Services.Claims;
using RcmEngine.Services.Rules;

namespace PlatformApi.Controllers;

[ApiController]
[Route("v1/claims")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class ClaimsController(IClaimSubmissionService claims) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        ClaimStatus? claimStatus = status != null && Enum.TryParse<ClaimStatus>(status, true, out var s) ? s : null;
        var list = await claims.ListAsync(orgId, claimStatus, ct);
        return Ok(list.Select(MapClaim));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var claim = await claims.GetByIdAsync(id, ct);
        if (claim == null) return NotFound();
        return Ok(MapClaimDetail(claim));
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> Ingest([FromBody] IngestClaimsRequest request, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var claim = await claims.IngestFromDentalBridgeAsync(orgId, request.LocationId, ct);
        return Ok(MapClaim(claim));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var claim = await claims.ScrubAndSubmitAsync(id, ct);
        return Ok(MapClaimDetail(claim));
    }

    private static object MapClaim(Claim c) => new
    {
        c.Id,
        c.ExternalClaimId,
        c.PayerId,
        c.PayerName,
        Status = c.Status.ToString(),
        c.DateOfService,
        c.TotalChargeAmount,
        c.PaidAmount,
        c.PayerClaimId,
        LineCount = c.Lines.Count
    };

    private static object MapClaimDetail(Claim c) => new
    {
        c.Id,
        c.ExternalClaimId,
        c.PayerId,
        c.PayerName,
        Status = c.Status.ToString(),
        c.DateOfService,
        c.TotalChargeAmount,
        c.PaidAmount,
        c.PayerClaimId,
        c.ScrubErrorsJson,
        Patient = c.Patient == null ? null : new { c.Patient.FirstName, c.Patient.LastName },
        Lines = c.Lines.Select(l => new { l.LineNumber, l.ProcedureCode, l.ToothNumber, l.ChargeAmount, l.PaidAmount }),
        StatusHistory = c.StatusHistory.OrderBy(h => h.OccurredAt).Select(h => new
        {
            Status = h.Status.ToString(),
            h.Source,
            h.Details,
            h.OccurredAt
        }),
        Submissions = c.Submissions.Select(s => new { s.ClearinghouseReference, s.Status, s.SubmittedAt })
    };
}

public record IngestClaimsRequest(Guid LocationId);

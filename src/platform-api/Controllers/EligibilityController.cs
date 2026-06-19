using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlatformApi.Auth;
using RcmEngine.Domain.Entities;
using RcmEngine.Services.Ack;
using RcmEngine.Services.Eligibility;
using RcmEngine.Services.Posting;

namespace PlatformApi.Controllers;

[ApiController]
[Route("v1")]
public class HealthController(RcmDbContext db) : ControllerBase
{
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health()
    {
        var dbOk = await db.Database.CanConnectAsync();
        return Ok(new
        {
            status = dbOk ? "healthy" : "degraded",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            checks = new { database = dbOk ? "up" : "down" }
        });
    }
}

[ApiController]
[Route("v1/eligibility")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class EligibilityController(IEligibilityOrchestrator orchestrator) : ControllerBase
{
    [HttpPost("check")]
    public async Task<IActionResult> TriggerCheck([FromBody] EligibilityCheckRequest request, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var check = await orchestrator.TriggerCheckAsync(
            orgId, request.LocationId, request.PatientId, request.CoverageId, request.AppointmentId, ct);

        return Accepted(new EligibilityCheckResponse(
            check.Id, check.Status.ToString(), check.ConfidenceScore, check.BenefitSummary, check.CompletedAt));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var check = await orchestrator.GetByIdAsync(id, ct);
        if (check == null) return NotFound();
        return Ok(MapCheck(check));
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? locationId, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var checks = await orchestrator.ListAsync(orgId, locationId, ct);
        return Ok(checks.Select(MapCheck));
    }

    private static object MapCheck(EligibilityCheck check) => new
    {
        check.Id,
        check.PatientId,
        check.LocationId,
        Status = check.Status.ToString(),
        check.ConfidenceScore,
        check.BenefitSummary,
        check.CompletedAt,
        Patient = check.Patient == null ? null : new { check.Patient.FirstName, check.Patient.LastName },
        Benefits = check.BenefitSnapshot == null ? null : new
        {
            check.BenefitSnapshot.PlanName,
            check.BenefitSnapshot.AnnualMaximum,
            check.BenefitSnapshot.AnnualMaximumRemaining,
            check.BenefitSnapshot.DeductibleRemaining,
            check.BenefitSnapshot.CoinsurancePercent,
            check.BenefitSnapshot.ExpiresAt
        }
    };
}

public record EligibilityCheckRequest(Guid LocationId, Guid PatientId, Guid? CoverageId, Guid? AppointmentId);
public record EligibilityCheckResponse(Guid Id, string Status, decimal ConfidenceScore, string? BenefitSummary, DateTime? CompletedAt);

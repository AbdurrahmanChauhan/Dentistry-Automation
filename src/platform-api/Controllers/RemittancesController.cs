using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatformApi.Auth;
using RcmEngine.Services.Posting;

namespace PlatformApi.Controllers;

[ApiController]
[Route("v1/remittances")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class RemittancesController(
    IRemittanceService remittances,
    IPostingService posting) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var list = await remittances.ListAsync(orgId, ct);
        return Ok(list.Select(r => new
        {
            r.Id,
            r.EraReference,
            r.PayerName,
            r.PayerId,
            r.PaymentDate,
            r.TotalPaymentAmount,
            Status = r.Status.ToString(),
            LineCount = r.Lines.Count,
            PostedLines = r.Lines.Count(l => l.PostingStatus == Domain.Entities.PostingStatus.AutoPosted ||
                l.PostingStatus == Domain.Entities.PostingStatus.ManualApproved)
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var remittance = await remittances.GetByIdAsync(id, ct);
        if (remittance == null) return NotFound();
        return Ok(new
        {
            remittance.Id,
            remittance.EraReference,
            remittance.PayerName,
            remittance.PaymentDate,
            remittance.TotalPaymentAmount,
            Status = remittance.Status.ToString(),
            Lines = remittance.Lines.Select(l => new
            {
                l.Id,
                l.LineNumber,
                l.ProcedureCode,
                l.PaidAmount,
                l.BilledAmount,
                l.CarcCode,
                l.RarcCode,
                l.MatchConfidence,
                PostingStatus = l.PostingStatus.ToString(),
                l.MatchedClaimLineId
            })
        });
    }

    [HttpPost("poll")]
    public async Task<IActionResult> PollClearinghouse(CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        var ingested = await remittances.IngestFromClearinghouseAsync(orgId, ct);
        return Ok(new { count = ingested.Count, remittances = ingested.Select(r => r.Id) });
    }

    [HttpPost("upload-835")]
    public async Task<IActionResult> Upload835([FromBody] Upload835Request request, CancellationToken ct)
    {
        var orgId = User.GetOrganizationId();
        await remittances.Process835FileAsync(orgId, request.Raw835, ct);
        return Accepted(new { message = "835 processed" });
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> AutoPost(Guid id, CancellationToken ct)
    {
        var posted = await posting.AutoPostRemittanceAsync(id, ct);
        return Ok(new { postedCount = posted });
    }

    [HttpPost("lines/{lineId:guid}/post")]
    public async Task<IActionResult> PostLine(Guid lineId, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        try
        {
            var attempt = await posting.PostRemittanceLineAsync(lineId, force, ct);
            return Ok(new
            {
                attempt.Id,
                Status = attempt.Status.ToString(),
                attempt.ConfidenceScore,
                attempt.PmsWriteBackReference
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }
}

public record Upload835Request(string Raw835);

using System.Text.Json;
using Microsoft.Extensions.Logging;
using RcmEngine.Ports;

namespace Integrations.Clearinghouse;

public class MockClearinghousePort(ILogger<MockClearinghousePort> logger) : IClearinghousePort
{
    private int _submitCounter;
    private readonly Queue<AckEventDto> _pendingAcks = new();

    public Task<Eligibility271Response> CheckEligibilityAsync(Eligibility270Request request, CancellationToken ct = default)
    {
        logger.LogDebug("Mock 270/271 for member {MemberId}", request.MemberId);

        var isEligible = !string.IsNullOrEmpty(request.MemberId) && request.MemberId != "INVALID";
        var response = new Eligibility271Response(
            isEligible,
            "Delta Dental PPO",
            1500m,
            875m,
            50m,
            0m,
            80m,
            JsonSerializer.Serialize(new { request.PayerId, request.MemberId, isEligible }),
            isEligible ? 0.98m : 0.5m);

        return Task.FromResult(response);
    }

    public Task<SubmissionResult> Submit837DAsync(string claimPayload, CancellationToken ct = default)
    {
        var refId = $"PAY-CLM-{Interlocked.Increment(ref _submitCounter):D3}";
        logger.LogInformation("Mock 837D submitted: {RefId}", refId);

        _pendingAcks.Enqueue(new AckEventDto("277CA", "Accepted", refId, null,
            $"{{\"reference\":\"{refId}\",\"status\":\"Accepted\"}}"));

        return Task.FromResult(new SubmissionResult(refId, "Submitted", claimPayload));
    }

    public Task<ClaimStatus277> GetClaimStatusAsync(string payerClaimId, CancellationToken ct = default)
    {
        return Task.FromResult(new ClaimStatus277(payerClaimId, "Adjudicated", "Claim processed"));
    }

    public Task<IReadOnlyList<Remittance835>> PollRemittancesAsync(CancellationToken ct = default)
    {
        var dos = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var remit = new Remittance835(
            $"ERA-{DateTime.UtcNow:yyyyMMdd}-001",
            "Delta Dental",
            "DD001",
            DateOnly.FromDateTime(DateTime.UtcNow),
            350m,
            "TRN-123456",
            JsonSerializer.Serialize(new { trace = "TRN-123456" }),
            [
                new Remittance835Line(1, "PAY-CLM-001", "PAT-001", "D0120", dos, 75m, 60m, 15m, "CO-45", null),
                new Remittance835Line(2, "PAY-CLM-001", "PAT-001", "D1110", dos, 125m, 100m, 25m, "CO-45", null),
                new Remittance835Line(3, "PAY-CLM-001", "PAT-001", "D2391", dos, 250m, 190m, 60m, "CO-45", null)
            ]);

        return Task.FromResult<IReadOnlyList<Remittance835>>([remit]);
    }

    public Task<IReadOnlyList<AckEventDto>> PollAcksAsync(CancellationToken ct = default)
    {
        var acks = new List<AckEventDto>();
        while (_pendingAcks.Count > 0)
            acks.Add(_pendingAcks.Dequeue());
        return Task.FromResult<IReadOnlyList<AckEventDto>>(acks);
    }
}

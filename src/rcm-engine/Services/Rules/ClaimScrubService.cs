using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;

namespace RcmEngine.Services.Rules;

public record ScrubResult(bool Passed, IReadOnlyList<string> Errors);

public interface IClaimScrubService
{
    ScrubResult Scrub(Claim claim);
}

public class ClaimScrubService : IClaimScrubService
{
    private static readonly HashSet<string> ValidCdtPrefixes = ["D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9"];

    public ScrubResult Scrub(Claim claim)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(claim.PayerId))
            errors.Add("PayerId is required");

        if (claim.TotalChargeAmount <= 0)
            errors.Add("Total charge amount must be greater than zero");

        if (claim.Lines.Count == 0)
            errors.Add("Claim must have at least one procedure line");

        foreach (var line in claim.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.ProcedureCode))
                errors.Add($"Line {line.LineNumber}: Procedure code is required");
            else if (!line.ProcedureCode.StartsWith('D') || line.ProcedureCode.Length != 5)
                errors.Add($"Line {line.LineNumber}: Invalid CDT format '{line.ProcedureCode}'");
            else if (!ValidCdtPrefixes.Contains(line.ProcedureCode[..2]))
                errors.Add($"Line {line.LineNumber}: Unknown CDT category '{line.ProcedureCode[..2]}'");

            if (line.ChargeAmount <= 0)
                errors.Add($"Line {line.LineNumber}: Charge amount must be positive");

            if (RequiresTooth(line.ProcedureCode) && string.IsNullOrWhiteSpace(line.ToothNumber))
                errors.Add($"Line {line.LineNumber}: Tooth number required for {line.ProcedureCode}");
        }

        return new ScrubResult(errors.Count == 0, errors);
    }

    private static bool RequiresTooth(string cdt) =>
        cdt.StartsWith("D2") || cdt.StartsWith("D3") || cdt.StartsWith("D4");
}

public interface IClaimSubmissionService
{
    Task<Claim> IngestFromDentalBridgeAsync(Guid organizationId, Guid locationId, CancellationToken ct = default);
    Task<Claim> ScrubAndSubmitAsync(Guid claimId, CancellationToken ct = default);
    Task<IReadOnlyList<Claim>> ListAsync(Guid organizationId, ClaimStatus? status, CancellationToken ct = default);
    Task<Claim?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

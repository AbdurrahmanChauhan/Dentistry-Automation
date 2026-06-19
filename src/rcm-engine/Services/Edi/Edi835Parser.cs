using RcmEngine.Ports;

namespace RcmEngine.Services.Edi;

/// <summary>
/// Simplified 835 ERA parser for MVP. Parses structured mock/sample 835 files.
/// Production would use full X12 5010 parser.
/// </summary>
public static class Edi835Parser
{
    public record Parsed835(
        string EraReference,
        string PayerName,
        string PayerId,
        DateOnly PaymentDate,
        decimal TotalPaymentAmount,
        string? TraceNumber,
        IReadOnlyList<Remittance835Line> Lines);

    public static Parsed835 Parse(string raw835)
    {
        if (raw835.TrimStart().StartsWith('{'))
            return ParseJson(raw835);

        return ParseX12Lite(raw835);
    }

    private static Parsed835 ParseJson(string json)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        var lines = new List<Remittance835Line>();
        var lineNum = 1;

        if (root.TryGetProperty("lines", out var linesEl))
        {
            foreach (var line in linesEl.EnumerateArray())
            {
                lines.Add(new Remittance835Line(
                    lineNum++,
                    line.TryGetProperty("payerClaimId", out var pc) ? pc.GetString() : null,
                    line.TryGetProperty("patientControlNumber", out var pn) ? pn.GetString() : null,
                    line.GetProperty("procedureCode").GetString() ?? "",
                    line.TryGetProperty("dateOfService", out var dos) && DateOnly.TryParse(dos.GetString(), out var d) ? d : null,
                    line.TryGetProperty("billedAmount", out var ba) ? ba.GetDecimal() : 0,
                    line.TryGetProperty("paidAmount", out var pa) ? pa.GetDecimal() : 0,
                    line.TryGetProperty("adjustmentAmount", out var aa) ? aa.GetDecimal() : 0,
                    line.TryGetProperty("carcCode", out var cc) ? cc.GetString() : null,
                    line.TryGetProperty("rarcCode", out var rc) ? rc.GetString() : null));
            }
        }

        return new Parsed835(
            root.TryGetProperty("eraReference", out var er) ? er.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            root.TryGetProperty("payerName", out var pn2) ? pn2.GetString() ?? "Unknown Payer" : "Unknown Payer",
            root.TryGetProperty("payerId", out var pi) ? pi.GetString() ?? "00000" : "00000",
            root.TryGetProperty("paymentDate", out var pd) && DateOnly.TryParse(pd.GetString(), out var payDate)
                ? payDate : DateOnly.FromDateTime(DateTime.UtcNow),
            root.TryGetProperty("totalPaymentAmount", out var ta) ? ta.GetDecimal() : lines.Sum(l => l.PaidAmount),
            root.TryGetProperty("traceNumber", out var tr) ? tr.GetString() : null,
            lines);
    }

    private static Parsed835 ParseX12Lite(string x12)
    {
        var segments = x12.Split('~', StringSplitOptions.RemoveEmptyEntries);
        var payerName = "Delta Dental";
        var payerId = "DD001";
        var traceNumber = "";
        var paymentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalPayment = 0m;
        var lines = new List<Remittance835Line>();
        var lineNum = 1;
        string? currentClaimId = null;

        foreach (var segment in segments)
        {
            var parts = segment.Split('*');
            if (parts.Length == 0) continue;

            switch (parts[0])
            {
                case "N1" when parts.Length > 2 && parts[1] == "PR":
                    payerName = parts[2];
                    break;
                case "TRN" when parts.Length > 2:
                    traceNumber = parts[2];
                    break;
                case "BPR" when parts.Length > 3:
                    decimal.TryParse(parts[2], out totalPayment);
                    if (parts.Length > 16 && DateOnly.TryParseExact(parts[16], "yyyyMMdd", out var pd))
                        paymentDate = pd;
                    break;
                case "CLP" when parts.Length > 4:
                    currentClaimId = parts[1];
                    break;
                case "SVC" when parts.Length > 3:
                    var procParts = parts[1].Split(':');
                    var procCode = procParts.Length > 1 ? procParts[1] : parts[1];
                    decimal.TryParse(parts[2], out var billed);
                    decimal.TryParse(parts[3], out var paid);
                    lines.Add(new Remittance835Line(lineNum++, currentClaimId, null, procCode, null,
                        billed, paid, billed - paid, null, null));
                    break;
                case "CAS" when parts.Length > 3:
                    if (lines.Count > 0)
                    {
                        var last = lines[^1];
                        lines[^1] = last with { CarcCode = parts[1] + "-" + parts[2] };
                    }
                    break;
            }
        }

        return new Parsed835(
            traceNumber.Length > 0 ? $"ERA-{traceNumber}" : $"ERA-{Guid.NewGuid():N}",
            payerName, payerId, paymentDate,
            totalPayment > 0 ? totalPayment : lines.Sum(l => l.PaidAmount),
            traceNumber, lines);
    }
}

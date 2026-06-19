using RcmEngine.Domain.Entities;
using RcmEngine.Services.Edi;
using RcmEngine.Services.Rules;
using Xunit;

namespace RcmEngine.Tests;

public class ClaimScrubServiceTests
{
    private readonly ClaimScrubService _scrub = new();

    [Fact]
    public void Scrub_PassesValidClaim()
    {
        var claim = new Claim
        {
            PayerId = "DD001",
            TotalChargeAmount = 200m,
            Lines =
            [
                new ClaimLine { LineNumber = 1, ProcedureCode = "D0120", ChargeAmount = 75m },
                new ClaimLine { LineNumber = 2, ProcedureCode = "D1110", ChargeAmount = 125m }
            ]
        };

        var result = _scrub.Scrub(claim);
        Assert.True(result.Passed);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Scrub_FailsMissingTooth()
    {
        var claim = new Claim
        {
            PayerId = "DD001",
            TotalChargeAmount = 250m,
            Lines = [new ClaimLine { LineNumber = 1, ProcedureCode = "D2391", ChargeAmount = 250m }]
        };

        var result = _scrub.Scrub(claim);
        Assert.False(result.Passed);
        Assert.Contains(result.Errors, e => e.Contains("Tooth number"));
    }

    [Fact]
    public void Scrub_FailsInvalidCdt()
    {
        var claim = new Claim
        {
            PayerId = "DD001",
            TotalChargeAmount = 100m,
            Lines = [new ClaimLine { LineNumber = 1, ProcedureCode = "X9999", ChargeAmount = 100m }]
        };

        var result = _scrub.Scrub(claim);
        Assert.False(result.Passed);
    }
}

public class Edi835ParserTests
{
    [Fact]
    public void Parse_Json835_ReturnsLines()
    {
        var json = """
            {
              "eraReference": "ERA-TEST",
              "payerName": "Delta Dental",
              "payerId": "DD001",
              "paymentDate": "2026-06-01",
              "totalPaymentAmount": 350,
              "lines": [
                {"payerClaimId": "CLM-1", "procedureCode": "D0120", "billedAmount": 75, "paidAmount": 60, "adjustmentAmount": 15, "carcCode": "CO-45"}
              ]
            }
            """;

        var result = Edi835Parser.Parse(json);
        Assert.Equal("ERA-TEST", result.EraReference);
        Assert.Single(result.Lines);
        Assert.Equal("D0120", result.Lines[0].ProcedureCode);
    }

    [Fact]
    public void Parse_X12Lite_ReturnsLines()
    {
        var x12 = "CLP*PAY-CLM-001*1*450*350~SVC*AD:D0120*75*60~CAS*CO*45*15~";
        var result = Edi835Parser.Parse(x12);
        Assert.NotEmpty(result.Lines);
        Assert.Equal("D0120", result.Lines[0].ProcedureCode);
    }
}

public class PaymentMatchEngineTests
{
    [Fact]
    public void CanAutoPost_RequiresHighConfidence()
    {
        Assert.True(RcmEngine.Services.Posting.PaymentMatchEngine.CanAutoPost(0.98m));
        Assert.False(RcmEngine.Services.Posting.PaymentMatchEngine.CanAutoPost(0.80m));
    }
}

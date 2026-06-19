using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RcmEngine.Ports;

namespace Integrations.Ai;

public class AiWorkerOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
}

public class HttpAiWorkerClient(HttpClient http, IOptions<AiWorkerOptions> options, ILogger<HttpAiWorkerClient> logger) : IAiWorkerClient
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<EobExtractionResult> ExtractEobAsync(byte[] pdfContent, string fileName, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(pdfContent), "file", fileName);

            var response = await http.PostAsync($"{options.Value.BaseUrl}/extract/eob", content, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EobExtractionResult>(JsonOptions, ct);
            return result ?? new EobExtractionResult(false, 0, [], null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI worker unavailable, using mock extraction");
            return MockAiWorkerClient.MockExtract(fileName);
        }
    }

    public async Task<DenialSummaryResult> SummarizeDenialAsync(DenialSummaryRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await http.PostAsJsonAsync($"{options.Value.BaseUrl}/summarize/denial", request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<DenialSummaryResult>(JsonOptions, ct);
            return result ?? MockAiWorkerClient.MockSummarize(request);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI worker unavailable, using mock summarization");
            return MockAiWorkerClient.MockSummarize(request);
        }
    }
}

public class MockAiWorkerClient : IAiWorkerClient
{
    public Task<EobExtractionResult> ExtractEobAsync(byte[] pdfContent, string fileName, CancellationToken ct = default)
        => Task.FromResult(MockExtract(fileName));

    public Task<DenialSummaryResult> SummarizeDenialAsync(DenialSummaryRequest request, CancellationToken ct = default)
        => Task.FromResult(MockSummarize(request));

    internal static EobExtractionResult MockExtract(string fileName) =>
        new(true, 0.92m, [
            new Remittance835Line(1, "PAY-CLM-001", "PAT-001", "D0120",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), 75m, 60m, 15m, "CO-45", null)
        ], JsonSerializer.Serialize(new { fileName, extracted = true }));

    internal static DenialSummaryResult MockSummarize(DenialSummaryRequest request)
    {
        var action = request.CarcCode switch
        {
            "CO-45" => "Review contracted fee schedule; likely contractual adjustment, no appeal needed",
            "CO-97" => "Verify frequency limits; resubmit with supporting documentation if appropriate",
            "OA-23" => "Confirm prior authorization; contact payer if auth on file",
            _ => "Review denial reason and payer policy; consider appeal or write-off"
        };

        return new DenialSummaryResult(
            $"Denial {request.CarcCode}: Payment differs from billed amount for claim context: {request.ClaimContext}",
            action,
            request.CarcCode == "OA-23" ? 85 : request.CarcCode == "CO-97" ? 70 : 45);
    }
}

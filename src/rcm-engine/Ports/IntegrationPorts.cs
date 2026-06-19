namespace RcmEngine.Ports;

public record AppointmentDto(
    string AppointmentId,
    string PatientId,
    string PatientFirstName,
    string PatientLastName,
    DateTime AppointmentDateTime,
    string ClinicServerId,
    string ProviderId);

public record CoverageDto(
    string CoverageId,
    string PatientId,
    string PayerName,
    string PayerId,
    string MemberId,
    string GroupNumber,
    int CoverageOrder,
    decimal? CoveragePercentage);

public record ClaimProcedureDto(
    string ClaimId,
    string PatientId,
    string ProcedureCode,
    string? ToothNumber,
    string? Surface,
    decimal ChargeAmount,
    DateOnly DateOfService,
    string ProviderId,
    string PayerId,
    string PayerName);

public record PaymentWriteBackRequest(
    string DentalOffice,
    string PracticeLocation,
    string UniqueKey,
    string PatientId,
    decimal PaymentAmount,
    DateOnly PaymentDate,
    DateOnly? DateOfService,
    string? ProviderId,
    string? PatientFirstName,
    string? PatientLastName);

public record EligibilityWriteBackRequest(
    string DentalOffice,
    string PracticeLocation,
    string PatientId,
    string VerificationStatus,
    string? BenefitSummary);

public interface IDentalBridgeClient
{
    Task<IReadOnlyList<AppointmentDto>> GetAppointmentsAsync(string clinicId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<CoverageDto>> GetCoverageAsync(string patientId, CancellationToken ct = default);
    Task<IReadOnlyList<ClaimProcedureDto>> GetClaimProceduresAsync(string clinicId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public interface IPmsWriteBackPort
{
    Task<bool> WritePaymentAsync(PaymentWriteBackRequest request, CancellationToken ct = default);
    Task<bool> WriteEligibilityAsync(EligibilityWriteBackRequest request, CancellationToken ct = default);
}

public record Eligibility270Request(
    string PayerId,
    string MemberId,
    string ProviderNpi,
    string PatientFirstName,
    string PatientLastName,
    DateOnly PatientDob);

public record Eligibility271Response(
    bool IsEligible,
    string PlanName,
    decimal? AnnualMaximum,
    decimal? AnnualMaximumRemaining,
    decimal? Deductible,
    decimal? DeductibleRemaining,
    decimal? CoinsurancePercent,
    string RawJson,
    decimal ConfidenceScore);

public record SubmissionResult(string ReferenceId, string Status, string? RawPayload);
public record ClaimStatus277(string PayerClaimId, string Status, string? Details);
public record AckEventDto(string AckType, string Status, string? ClaimReference, string? RejectReason, string RawPayload);
public record Remittance835Line(
    int LineNumber,
    string? PayerClaimId,
    string? PatientControlNumber,
    string ProcedureCode,
    DateOnly? DateOfService,
    decimal BilledAmount,
    decimal PaidAmount,
    decimal AdjustmentAmount,
    string? CarcCode,
    string? RarcCode);

public record Remittance835(
    string EraReference,
    string PayerName,
    string PayerId,
    DateOnly PaymentDate,
    decimal TotalPaymentAmount,
    string? TraceNumber,
    string RawPayload,
    IReadOnlyList<Remittance835Line> Lines);

public interface IClearinghousePort
{
    Task<Eligibility271Response> CheckEligibilityAsync(Eligibility270Request request, CancellationToken ct = default);
    Task<SubmissionResult> Submit837DAsync(string claimPayload, CancellationToken ct = default);
    Task<ClaimStatus277> GetClaimStatusAsync(string payerClaimId, CancellationToken ct = default);
    Task<IReadOnlyList<Remittance835>> PollRemittancesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AckEventDto>> PollAcksAsync(CancellationToken ct = default);
}

public interface IAiWorkerClient
{
    Task<EobExtractionResult> ExtractEobAsync(byte[] pdfContent, string fileName, CancellationToken ct = default);
    Task<DenialSummaryResult> SummarizeDenialAsync(DenialSummaryRequest request, CancellationToken ct = default);
}

public record EobExtractionResult(
    bool Success,
    decimal ConfidenceScore,
    IReadOnlyList<Remittance835Line> Lines,
    string? RawJson);

public record DenialSummaryRequest(
    string CarcCode,
    string? RarcCode,
    string ClaimContext,
    decimal BilledAmount,
    decimal PaidAmount);

public record DenialSummaryResult(
    string Summary,
    string SuggestedAction,
    int PriorityScore);

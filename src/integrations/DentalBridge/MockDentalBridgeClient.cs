using System.Text.Json;
using Microsoft.Extensions.Logging;
using RcmEngine.Ports;

namespace Integrations.DentalBridge;

public class MockDentalBridgeClient(ILogger<MockDentalBridgeClient> logger) : IDentalBridgeClient
{
    public Task<IReadOnlyList<AppointmentDto>> GetAppointmentsAsync(string clinicId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        logger.LogDebug("Mock DentalBridge: GetAppointments for {ClinicId}", clinicId);
        var appointments = new List<AppointmentDto>
        {
            new("APT-001", "PAT-002", "Sarah", "Johnson",
                DateTime.UtcNow.AddDays(2), clinicId, "PROV-001"),
            new("APT-002", "PAT-003", "Michael", "Brown",
                DateTime.UtcNow.AddDays(3), clinicId, "PROV-001")
        };
        return Task.FromResult<IReadOnlyList<AppointmentDto>>(appointments);
    }

    public Task<IReadOnlyList<CoverageDto>> GetCoverageAsync(string patientId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<CoverageDto>>([
            new CoverageDto("COV-001", patientId, "Delta Dental", "DD001", "MEM-12345", "GRP-100", 1, 80m)
        ]);
    }

    public Task<IReadOnlyList<ClaimProcedureDto>> GetClaimProceduresAsync(string clinicId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        logger.LogDebug("Mock DentalBridge: GetClaimProcedures for {ClinicId}", clinicId);
        var dos = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        return Task.FromResult<IReadOnlyList<ClaimProcedureDto>>([
            new ClaimProcedureDto("CLM-002", "PAT-002", "D0120", null, null, 75m, dos, "PROV-001", "DD001", "Delta Dental"),
            new ClaimProcedureDto("CLM-002", "PAT-002", "D1110", null, null, 125m, dos, "PROV-001", "DD001", "Delta Dental"),
            new ClaimProcedureDto("CLM-003", "PAT-003", "D2391", "19", "MO", 280m, dos.AddDays(1), "PROV-001", "DD001", "Delta Dental")
        ]);
    }
}

public class MockPmsWriteBackPort(ILogger<MockPmsWriteBackPort> logger) : IPmsWriteBackPort
{
    private readonly List<object> _writeBackLog = [];

    public IReadOnlyList<object> WriteBackLog => _writeBackLog;

    public Task<bool> WritePaymentAsync(PaymentWriteBackRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Mock PMS write-back payment: {Key} ${Amount} for patient {PatientId}",
            request.UniqueKey, request.PaymentAmount, request.PatientId);
        _writeBackLog.Add(request);
        return Task.FromResult(true);
    }

    public Task<bool> WriteEligibilityAsync(EligibilityWriteBackRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("Mock PMS write-back eligibility: {PatientId} status {Status}",
            request.PatientId, request.VerificationStatus);
        _writeBackLog.Add(request);
        return Task.FromResult(true);
    }
}

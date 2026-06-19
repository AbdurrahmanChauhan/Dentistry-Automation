using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RcmEngine.Data;
using RcmEngine.Domain.Entities;

namespace RcmEngine.Services;

public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}

public class DataSeeder(RcmDbContext db, ILogger<DataSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await db.Organizations.AnyAsync(ct)) return;

        var org = new Organization
        {
            Name = "Demo Dental Group",
            Slug = "demo-dental-group"
        };
        db.Organizations.Add(org);

        var location = new Location
        {
            OrganizationId = org.Id,
            Name = "Demo Clinic - Phoenix",
            ExternalClinicId = "CLINIC-001",
            PmsType = "OpenDental",
            Region = "Southwest"
        };
        db.Locations.Add(location);

        db.Providers.Add(new Provider
        {
            OrganizationId = org.Id,
            LocationId = location.Id,
            Npi = "1234567890",
            FirstName = "Jane",
            LastName = "Smith",
            ExternalProviderId = "PROV-001"
        });

        var patients = new[]
        {
            new Patient { OrganizationId = org.Id, LocationId = location.Id, ExternalPatientId = "PAT-001", FirstName = "John", LastName = "Doe", DateOfBirth = new DateOnly(1985, 3, 15), MemberId = "MEM-12345" },
            new Patient { OrganizationId = org.Id, LocationId = location.Id, ExternalPatientId = "PAT-002", FirstName = "Sarah", LastName = "Johnson", DateOfBirth = new DateOnly(1990, 7, 22), MemberId = "MEM-67890" },
            new Patient { OrganizationId = org.Id, LocationId = location.Id, ExternalPatientId = "PAT-003", FirstName = "Michael", LastName = "Brown", DateOfBirth = new DateOnly(1978, 11, 8), MemberId = "MEM-11111" }
        };
        db.Patients.AddRange(patients);

        await db.SaveChangesAsync(ct);

        foreach (var patient in patients)
        {
            db.Coverages.Add(new Coverage
            {
                OrganizationId = org.Id,
                LocationId = location.Id,
                PatientId = patient.Id,
                PayerName = "Delta Dental",
                PayerId = "DD001",
                MemberId = patient.MemberId!,
                GroupNumber = "GRP-100",
                CoverageOrder = 1
            });
        }

        var demoKeyHash = HashApiKey("da-demo-key-change-in-production");
        db.ApiKeys.Add(new ApiKey
        {
            OrganizationId = org.Id,
            KeyHash = demoKeyHash,
            Name = "Demo API Key",
            Scopes = "read,write"
        });

        var claim1 = new Claim
        {
            OrganizationId = org.Id,
            LocationId = location.Id,
            PatientId = patients[0].Id,
            ExternalClaimId = "CLM-001",
            PayerId = "DD001",
            PayerName = "Delta Dental",
            DateOfService = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
            Status = ClaimStatus.Submitted,
            TotalChargeAmount = 450m,
            PayerClaimId = "PAY-CLM-001"
        };
        claim1.Lines.Add(new ClaimLine { LineNumber = 1, ProcedureCode = "D0120", ChargeAmount = 75m });
        claim1.Lines.Add(new ClaimLine { LineNumber = 2, ProcedureCode = "D1110", ChargeAmount = 125m });
        claim1.Lines.Add(new ClaimLine { LineNumber = 3, ProcedureCode = "D2391", ToothNumber = "14", ChargeAmount = 250m });
        db.Claims.Add(claim1);

        db.Appointments.Add(new Appointment
        {
            OrganizationId = org.Id,
            LocationId = location.Id,
            ExternalAppointmentId = "APT-001",
            PatientId = patients[1].Id,
            ScheduledAt = DateTime.UtcNow.AddDays(2),
            Status = "Scheduled"
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded demo organization {OrgId}", org.Id);
    }

    public static string HashApiKey(string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

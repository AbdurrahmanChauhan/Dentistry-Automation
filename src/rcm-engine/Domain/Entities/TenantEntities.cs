namespace RcmEngine.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Location> Locations { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
}

public class Location
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExternalClinicId { get; set; } = string.Empty;
    public string PmsType { get; set; } = "OpenDental";
    public string? Region { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; }
    public ICollection<Provider> Providers { get; set; } = [];
}

public class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid LocationId { get; set; }
    public string Npi { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ExternalProviderId { get; set; }
    public bool IsActive { get; set; } = true;

    public Location? Location { get; set; }
}

public class Patient : Common.LocationScopedEntity
{
    public string ExternalPatientId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? MemberId { get; set; }

    public ICollection<Coverage> Coverages { get; set; } = [];
    public ICollection<Claim> Claims { get; set; } = [];
}

public class Coverage : Common.LocationScopedEntity
{
    public Guid PatientId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string GroupNumber { get; set; } = string.Empty;
    public int CoverageOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public Patient? Patient { get; set; }
}

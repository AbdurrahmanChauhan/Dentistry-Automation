using Microsoft.EntityFrameworkCore;
using RcmEngine.Domain.Entities;

namespace RcmEngine.Data;

public class RcmDbContext : DbContext
{
    public RcmDbContext(DbContextOptions<RcmDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Coverage> Coverages => Set<Coverage>();
    public DbSet<EligibilityCheck> EligibilityChecks => Set<EligibilityCheck>();
    public DbSet<BenefitSnapshot> BenefitSnapshots => Set<BenefitSnapshot>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimLine> ClaimLines => Set<ClaimLine>();
    public DbSet<ClaimSubmission> ClaimSubmissions => Set<ClaimSubmission>();
    public DbSet<ClaimStatusEvent> ClaimStatusEvents => Set<ClaimStatusEvent>();
    public DbSet<AckEvent> AckEvents => Set<AckEvent>();
    public DbSet<Remittance> Remittances => Set<Remittance>();
    public DbSet<RemittanceLine> RemittanceLines => Set<RemittanceLine>();
    public DbSet<PostingAttempt> PostingAttempts => Set<PostingAttempt>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<IntegrationJob> IntegrationJobs => Set<IntegrationJob>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ApiUsageLog> ApiUsageLogs => Set<ApiUsageLog>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<Location>(e =>
        {
            e.HasIndex(x => new { x.OrganizationId, x.ExternalClinicId });
            e.HasOne(x => x.Organization).WithMany(x => x.Locations).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<Patient>(e =>
        {
            e.HasIndex(x => new { x.OrganizationId, x.LocationId, x.ExternalPatientId });
        });

        modelBuilder.Entity<EligibilityCheck>(e =>
            e.HasOne(x => x.BenefitSnapshot).WithOne(x => x!.EligibilityCheck)
                .HasForeignKey<BenefitSnapshot>(x => x.EligibilityCheckId));

        modelBuilder.Entity<Claim>(e =>
        {
            e.HasIndex(x => new { x.OrganizationId, x.PayerClaimId });
            e.HasIndex(x => new { x.OrganizationId, x.Status });
        });

        modelBuilder.Entity<ClaimLine>(e =>
            e.HasOne(x => x.Claim).WithMany(x => x.Lines).HasForeignKey(x => x.ClaimId));

        modelBuilder.Entity<RemittanceLine>(e =>
            e.HasIndex(x => new { x.RemittanceId, x.LineNumber });

        modelBuilder.Entity<WorkItem>(e =>
            e.HasIndex(x => new { x.OrganizationId, x.Status, x.Priority, x.CreatedAt });

        modelBuilder.Entity<IntegrationJob>(e =>
            e.HasIndex(x => new { x.Status, x.JobType });
            e.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL");

        modelBuilder.Entity<AuditEvent>(e =>
            e.HasIndex(x => new { x.OrganizationId, x.Timestamp });

        modelBuilder.Entity<ApiKey>(e =>
            e.HasIndex(x => x.KeyHash).IsUnique());

        modelBuilder.Entity<ApiUsageLog>(e =>
            e.HasIndex(x => new { x.OrganizationId, x.Timestamp });
    }
}

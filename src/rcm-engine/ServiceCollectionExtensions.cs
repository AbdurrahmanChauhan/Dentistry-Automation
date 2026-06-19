using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RcmEngine.Services.Ack;
using RcmEngine.Services.Claims;
using RcmEngine.Services.Eligibility;
using RcmEngine.Services.Posting;
using RcmEngine.Services.Rules;
using RcmEngine.Services.WorkItems;

namespace RcmEngine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRcmEngine(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<Data.RcmDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IEligibilityOrchestrator, EligibilityOrchestrator>();
        services.AddScoped<IClaimScrubService, ClaimScrubService>();
        services.AddScoped<IClaimSubmissionService, ClaimSubmissionService>();
        services.AddScoped<IAckMonitoringService, AckMonitoringService>();
        services.AddScoped<IRemittanceService, RemittanceService>();
        services.AddScoped<IPaymentMatchEngine, PaymentMatchEngine>();
        services.AddScoped<IPostingService, PostingService>();
        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IIntegrationJobService, IntegrationJobService>();
        services.AddScoped<IDataSeeder, DataSeeder>();

        return services;
    }
}

using RcmEngine.Services.Ack;
using RcmEngine.Services.Eligibility;

namespace PlatformApi.Background;

public class WorkflowBackgroundService(
    IServiceProvider services,
    ILogger<WorkflowBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Workflow background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var eligibility = scope.ServiceProvider.GetRequiredService<IEligibilityOrchestrator>();
                var acks = scope.ServiceProvider.GetRequiredService<IAckMonitoringService>();

                await eligibility.ProcessPendingChecksAsync(stoppingToken);
                await acks.ProcessAcksAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background workflow cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}

using Integrations.Ai;
using Integrations.Clearinghouse;
using Integrations.DentalBridge;
using Microsoft.Extensions.DependencyInjection;
using RcmEngine.Ports;

namespace Integrations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMockIntegrations(this IServiceCollection services)
    {
        services.AddSingleton<IDentalBridgeClient, MockDentalBridgeClient>();
        services.AddSingleton<IClearinghousePort, MockClearinghousePort>();
        services.AddSingleton<IPmsWriteBackPort, MockPmsWriteBackPort>();
        services.AddSingleton<IAiWorkerClient, MockAiWorkerClient>();
        return services;
    }

    public static IServiceCollection AddHttpIntegrations(this IServiceCollection services, string aiWorkerBaseUrl)
    {
        services.AddSingleton<IDentalBridgeClient, MockDentalBridgeClient>();
        services.AddSingleton<IClearinghousePort, MockClearinghousePort>();
        services.AddSingleton<IPmsWriteBackPort, MockPmsWriteBackPort>();

        services.Configure<AiWorkerOptions>(o => o.BaseUrl = aiWorkerBaseUrl);
        services.AddHttpClient<IAiWorkerClient, HttpAiWorkerClient>();
        return services;
    }
}

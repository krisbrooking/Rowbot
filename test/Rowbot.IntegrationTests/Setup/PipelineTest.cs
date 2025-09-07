using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rowbot.Connectors.Sqlite;
using Rowbot.Pipelines.Summary;

namespace Rowbot.IntegrationTests.Setup;

public static class PipelineTest
{
    public static async Task<PipelineSummary> RunPipelineAsync(
        Func<IPipelineBuilder, Pipeline> pipeline,
        Func<IServiceCollection, IServiceCollection>? additionalServices = null)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddRowbot([typeof(EmptyPipelineClass)]);
                services.AddSqliteConnector();
                additionalServices?.Invoke(services);
            })
            .Build();

        var pipelineBuilder = host.Services.GetRequiredService<IPipelineBuilder>();

        return await pipeline(pipelineBuilder).InvokeAsync();
    }

    public static IPipelineRunner RunPipelinesAsync(
        Type[] pipelineTypes,
        Func<IServiceCollection, IServiceCollection>? additionalServices = null)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddRowbot(pipelineTypes);
                services.AddSqliteConnector();
                additionalServices?.Invoke(services);
            })
            .Build();

        return host.Services.GetRequiredService<IPipelineRunner>();
    }

    public static IPipelineRunner RunPipelinesAsync<TPipeline>(
        Func<IServiceCollection, IServiceCollection>? additionalServices = null)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddRowbot([typeof(TPipeline)]);
                services.AddSqliteConnector();
                additionalServices?.Invoke(services);
            })
            .Build();

        return host.Services.GetRequiredService<IPipelineRunner>();
    }
}

public sealed class EmptyPipelineClass : IPipeline
{
}
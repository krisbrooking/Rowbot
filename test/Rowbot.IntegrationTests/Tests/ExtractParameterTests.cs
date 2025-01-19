using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rowbot.IntegrationTests.Setup.Entities;
using Rowbot.Loaders.Framework;

namespace Rowbot.IntegrationTests.Tests;

public class ExtractParameterTests
{
    [Fact]
    public async Task SecondExtractBlock_ShouldIncludeParametersFromPreviousBlock()
    {
        var data = new TargetData();
        
        var runner = BuildRunner(data,typeof(OrderPipelines));
        await runner.RunAsync();
        
        Assert.Equal(25, data.OrderLines.Count);
        Assert.Equal(1, data.OrderLines[5].OrderId);
        Assert.Equal(2, data.OrderLines[10].OrderId);
    }
    
    public static IPipelineRunner BuildRunner(TargetData data, params Type[] pipelineTypes)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                services.AddRowbot(pipelineTypes);
                services.AddSingleton(data);
            })
            .Build();

        return host.Services.GetRequiredService<IPipelineRunner>();
    }
}

public class OrderPipelines(IPipelineBuilder pipelineBuilder, TargetData targetData) : IPipeline
{
    public Pipeline Build() =>
        pipelineBuilder
            .Extract<SourceOrder>(builder => builder
                .FromList(
                    Enumerable.Range(0, 5)
                        .Select(x => new SourceOrder { OrderId = x })
                        .ToList())
            )
            .Extract<SourceOrderLine>((builder, input) => builder
                .FromList(
                    Enumerable.Range(0, 5)
                        .Select(x => new SourceOrderLine
                            { OrderLineId = x, OrderId = input.OrderId })
                        .ToList())
                .WithExtractor(async (context, connector) =>
                {
                    var id = input.OrderId;
                    return await connector.QueryAsync(context.GetParameters());
                })
            )
            .Transform<SourceOrderLine>(source => source)
            .Load(builder => builder
                .ToList(targetData.OrderLines));
}

public class TargetData
{
    public List<SourceOrderLine> OrderLines { get; set; } = new();
}
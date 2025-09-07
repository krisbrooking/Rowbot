using Rowbot.IntegrationTests.Setup;
using Rowbot.IntegrationTests.Setup.Entities;

namespace Rowbot.IntegrationTests.Tests;

public class ExtractParameterTests
{
    [Fact]
    public async Task DeferredExtractBlock_ShouldIncludeInputFromPreviousBlock()
    {
        var targetData = new TargetData();

        await PipelineTest.RunPipelineAsync(pipelineBuilder => pipelineBuilder
            .Extract<SourceOrder>(builder => builder
                .FromList(
                Enumerable.Range(0, 5)
                    .Select(x => new SourceOrder { OrderId = x })
                    .ToList())
            )
            .Extract<SourceOrderLine>((builder, input) => builder
                .FromList(
                Enumerable.Range(0, 5)
                    .Select(x => new SourceOrderLine { OrderLineId = x, OrderId = input.OrderId })
                    .ToList())
            )
            .Load(builder => builder
                .ToList(targetData.OrderLines)
            ));

        Assert.Equal(25, targetData.OrderLines.Count);
        Assert.Equal(1, targetData.OrderLines[5].OrderId);
        Assert.Equal(2, targetData.OrderLines[10].OrderId);
    }
}

public class TargetData
{
    public List<SourceOrderLine> OrderLines { get; set; } = new();
}

public class TestPipeline(IPipelineBuilder pipelineBuilder) : IPipeline
{
    [Tag("Test")]
    public Pipeline Test() => pipelineBuilder
        .Extract<TestResult<int>>(builder => builder
            .FromList(
                Enumerable.Range(0, 5)
                    .Select(x => new TestResult<int>(x, x))
                    .ToList())
        )
        .Ensure(x => x.Source == x.Target);
}
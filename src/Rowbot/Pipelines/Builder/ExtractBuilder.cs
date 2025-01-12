using Rowbot.Extractors;
using Rowbot.Extractors.Framework;
using Rowbot.Pipelines.Blocks;
using Rowbot.Pipelines.Builder;

namespace Rowbot;

public interface IExtractBuilder<TInput, TOutput>
{
    /// <summary>
    /// Adds a read connector to the pipeline.
    /// </summary>
    /// <param name="configure">Read connector configuration</param>
    /// <typeparam name="TConnector">Read connector implementing <see cref="IReadConnector{TInput,TOutput}"/></typeparam>
    /// <returns>Extract block builder at extractor step</returns>
    IExtractBuilderConnectorStep<TInput, TOutput> WithConnector<TConnector>(Action<TConnector>? configure = null)
        where TConnector : IReadConnector<TInput, TOutput>;
}

public interface IExtractBuilderConnectorStep<TInput, TOutput> : IExtractBuilderExtractorStep<TInput, TOutput>
{
    /// <summary>
    /// Adds an extractor to the pipeline.
    /// </summary>
    /// <param name="configure">Extractor configuration</param>
    /// <typeparam name="TExtractor">Extractor implementing <see cref="IExtractor{TInput,TOutput}"/></typeparam>
    /// <returns>Completed extract block builder</returns>
    IExtractBuilderExtractorStep<TInput, TOutput> WithExtractor<TExtractor>(Action<TExtractor>? configure = null)
        where TExtractor : IExtractor<TInput, TOutput>;

    /// <summary>
    /// Adds an inline extractor to the pipeline.
    /// </summary>
    /// <param name="extractor">Returns sequence of items extracted from source.
    ///     <param name="extractor arg1">Extract context</param>
    ///     <param name="extractor arg2">Read connector</param>
    /// </param>
    /// <returns>Completed extract block builder</returns>
    IExtractBuilderExtractorStep<TInput, TOutput> WithExtractor(
        Func<ExtractContext<TInput>, IReadConnector<TInput, TOutput>, Task<IEnumerable<TOutput>>> extractor);
}

public interface IExtractBuilderExtractorStep<TInput, TOutput>
{
}

internal interface IExtractBuilderConnectorStepInternal<TInput, TOutput>
{
    bool HasExtractor { get; }
    IExtractBuilderExtractorStep<TInput, TOutput> AddDefaultExtractor();
}

public class ExtractBuilder<TInput, TOutput>(PipelineBuilderContext context, int batchSize)
    : IExtractBuilder<TInput, TOutput>
{
    private readonly PipelineBuilderContext _context = context;
    private readonly int _batchSize = batchSize;
    
    public IExtractBuilderConnectorStep<TInput, TOutput> WithConnector<TConnector>(Action<TConnector>? configure = null) 
        where TConnector : IReadConnector<TInput, TOutput>
    {
        var readConnector = _context.ServiceFactory.CreateReadConnector<TConnector, TInput, TOutput>();
        configure?.Invoke(readConnector);
        
        return new ExtractConnector<TInput, TOutput>(_context, _batchSize, readConnector);
    }
}

public class ExtractConnector<TInput, TOutput>(
    PipelineBuilderContext context, 
    int batchSize, 
    IReadConnector<TInput, TOutput> readConnector)
    : IExtractBuilderConnectorStep<TInput, TOutput>, IExtractBuilderConnectorStepInternal<TInput, TOutput>
{
    private readonly PipelineBuilderContext _context = context;
    private readonly int _batchSize = batchSize;
    private readonly IReadConnector<TInput, TOutput> _readConnector = readConnector;
    
    public bool HasExtractor { get; private set; }

    public IExtractBuilderExtractorStep<TInput, TOutput> WithExtractor<TExtractor>(Action<TExtractor>? configure = null)
        where TExtractor : IExtractor<TInput, TOutput>
    {
        if (_readConnector is null)
        {
            throw new InvalidOperationException("Read connector is not configured");
        }

        if (HasExtractor)
        {
            return this;
        }
        
        var extractor = _context.ServiceFactory.CreateExtractor<TExtractor, TInput, TOutput>(_readConnector);
        configure?.Invoke(extractor);

        if (_context.Blocks.Count == 0)
        {
            _context.Blocks.Enqueue(new ExtractBlock<TInput, TOutput>(
                extractor, 
                _context.LoggerFactory, 
                _batchSize, 
                new BlockOptions()));
        }
        else
        {
            _context.Blocks.Enqueue(new ExtractTransformBlock<TInput, TOutput>(
                extractor, 
                _context.LoggerFactory, 
                _batchSize, 
                new BlockOptions()));
        }
        
        HasExtractor = true;

        return this;
    }

    public IExtractBuilderExtractorStep<TInput, TOutput> WithExtractor(
        Func<ExtractContext<TInput>, IReadConnector<TInput, TOutput>, Task<IEnumerable<TOutput>>> inlineExtractor)
        => WithExtractor<InlineExtractor<TInput, TOutput>>(extractor => extractor.ExtractorDelegate = inlineExtractor);

    public IExtractBuilderExtractorStep<TInput, TOutput> AddDefaultExtractor() 
        => WithExtractor<DefaultExtractor<TInput, TOutput>>();
}
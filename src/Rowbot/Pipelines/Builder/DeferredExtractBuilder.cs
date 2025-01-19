using Rowbot.Extractors;
using Rowbot.Extractors.Framework;
using Rowbot.Pipelines.Builder;

namespace Rowbot;

public class DeferredExtractBuilder<TInput, TOutput>(PipelineBuilderContext context)
    : IExtractBuilder<TInput, TOutput>
{
    private readonly PipelineBuilderContext _context = context;
    
    public IExtractBuilderConnectorStep<TInput, TOutput> WithConnector<TConnector>(Action<TConnector>? configure = null) 
        where TConnector : IReadConnector<TInput, TOutput>
    {
        var readConnector = _context.ServiceFactory.CreateReadConnector<TConnector, TInput, TOutput>();
        configure?.Invoke(readConnector);
        
        return new DeferredExtractConnector<TInput, TOutput>(_context, readConnector);
    }
}

public class DeferredExtractConnector<TInput, TOutput>(
    PipelineBuilderContext context, 
    IReadConnector<TInput, TOutput> readConnector)
    : IExtractBuilderConnectorStep<TInput, TOutput>, IExtractBuilderConnectorStepInternal<TInput, TOutput>
{
    private readonly PipelineBuilderContext _context = context;
    private readonly IReadConnector<TInput, TOutput> _readConnector = readConnector;
    private readonly List<ExtractParameter> _parameters = [];

    public ExtractParameter[] Parameters => _parameters.ToArray();
    public bool HasExtractor => Extractor is not null;
    public IExtractor<TInput, TOutput>? Extractor { get; private set; }

    public IExtractBuilderConnectorStep<TInput, TOutput> WithParameter<T>(string parameterName, T parameterValue)
    {
        _parameters.Add(new ExtractParameter(parameterName, parameterValue!.GetType(), parameterValue));

        return this;
    }

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
        
        Extractor = extractor;

        return this;
    }

    public IExtractBuilderExtractorStep<TInput, TOutput> WithExtractor(
        Func<ExtractContext<TInput>, IReadConnector<TInput, TOutput>, Task<IEnumerable<TOutput>>> inlineExtractor)
        => WithExtractor<InlineExtractor<TInput, TOutput>>(extractor => extractor.ExtractorDelegate = inlineExtractor);

    public IExtractBuilderExtractorStep<TInput, TOutput> AddDefaultExtractor() 
        => WithExtractor<DefaultExtractor<TInput, TOutput>>();
}
using Microsoft.Extensions.Logging;
using Rowbot.Null;
using Rowbot.Pipelines.Blocks;
using Rowbot.Pipelines.Builder;
using System.Linq.Expressions;

namespace Rowbot;

/// <summary>
/// Non-generic IPipelineBuilder is the entry point for a pipeline of blocks.
/// </summary>
public interface IPipelineBuilder
{
    /// <summary>
    /// Adds a pipeline dependency. The pipeline runner ensures that any other pipeline that loads data of type
    /// <typeparamref name="TDependsOn"/> is executed prior to the execution of this pipeline.
    /// </summary>
    /// <typeparam name="TDependsOn">Data type on which this pipeline depends</typeparam>
    IPipelineBuilder DependsOn<TDependsOn>()
         where TDependsOn : class;

    /// <summary>
    /// Adds an extract block. An extract block uses a read connector and an extractor to get data from a data source.
    /// </summary>
    /// <param name="builder">Returns completed extract block builder
    ///     <param name="builder arg1">Entry point for the extract block builder</param>
    /// </param>
    /// <param name="options">Extract block options</param>
    /// <typeparam name="TOutput">Data type to extract</typeparam>
    /// <returns>Generic IPipelineBuilder</returns>
    /// <remarks>
    /// The first extract block has the same input and output type <typeparamref name="TOutput" />.
    /// </remarks>
    IPipelineBuilder<TOutput, TOutput> Extract<TOutput>(
        Func<IExtractBuilder<TOutput, TOutput>, IExtractBuilderExtractorStep<TOutput, TOutput>> builder,
        ExtractOptions? options = null);
}

 /// <summary>
 /// Generic IPipelineBuilder creates a pipeline of blocks.
 /// </summary>
 /// <typeparam name="TPrevious">Input type of previous block</typeparam>
 /// <typeparam name="TInput">Output type of previous block (and input type of current block)</typeparam>
 /// <remarks>
 /// IPipelineBuilder takes two generic arguments which represent the input and output types of the previous block in the pipeline.
 /// TPrevious is the input type of the previous block. It is unused by the current block but necessary to create a chain of IPipelineBuilders.
 /// TInput is the output type of the previous block, and therefore, the input type of the current block.
 /// Linking blocks is achieved using a generic method argument, TOutput. To continue the pipeline, this generic 
 /// argument is returned as the TInput type of the next block in the pipeline.
 /// </remarks>
public interface IPipelineBuilder<TPrevious, TInput>
{
    /// <summary>
    /// Adds an extract block. An extract block uses a read connector and an extractor to get data from a source.
    /// </summary>
    /// <param name="builder">Returns completed extract block builder
    ///     <param name="builder arg1">Entry point for the extract block builder</param>
    /// </param>
    /// <param name="options">Extract block options</param>
    /// <typeparam name="TInput">Output type of the previous block</typeparam>
    /// <typeparam name="TOutput">Data type to extract</typeparam>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TOutput> Extract<TOutput>(
        Func<IExtractBuilder<TInput, TOutput>, IExtractBuilderExtractorStep<TInput, TOutput>> builder,
        ExtractOptions? options = null);

    /// <summary>
    /// Adds an extract block. An extract block uses a read connector and an extractor to get data from a source.
    /// </summary>
    /// <param name="builder">Returns completed extract block builder
    ///     <param name="builder arg1">Entry point for the extract block builder</param>
    ///     <param name="builder arg2">Input entity instance passed through from previous extract block</param>
    /// </param>
    /// <param name="options">Extract block options</param>
    /// <typeparam name="TInput">Output type of the previous block</typeparam>
    /// <typeparam name="TOutput">Data type to extract</typeparam>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TOutput> Extract<TOutput>(
        Func<IExtractBuilder<TInput, TOutput>, TInput, IExtractBuilderExtractorStep<TInput, TOutput>> builder,
        ExtractOptions? options = null);

    /// <summary>
    /// Adds a transform block. A transform block is used to convert from a data type <typeparamref name="TInput"/>
    /// to another data type <typeparamref name="TOutput"/>.
    /// </summary>
    /// <param name="transform">Transformer function</param>
    /// <param name="options">Transform block options</param>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TOutput> Transform<TOutput>(
        Func<TInput[], Task<TOutput[]>> transform,
        TransformOptions? options = null);

    /// <summary>
    /// Adds a transform block. A transform block is used to convert from <typeparamref name="TInput"/>
    /// to <typeparamref name="TOutput"/>.
    /// </summary>
    /// <param name="transform">Transformer function</param>
    /// <param name="options">Transform block options</param>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TOutput> Transform<TOutput>(
        Func<TInput[], TOutput[]> transform,
        TransformOptions? options = null);

    /// <summary>
    /// <para>
    /// Adds a transform block. A transform block is used to convert from <typeparamref name="TInput"/>
    /// to <typeparamref name="TOutput"/>.
    /// </para>
    /// <para>This overload immediately converts output IEnumerable to array for input to next block.</para>
    /// </summary>
    /// <param name="transform">Transformer function</param>
    /// <param name="options">Transform block options</param>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TOutput> Transform<TOutput>(
        Func<TInput[], IEnumerable<TOutput>> transform,
        TransformOptions? options = null);

    /// <summary>
    /// Adds a transform block. Apply is a special kind of transform where the transformation is described declaratively
    /// as a mapper configuration. The mapper is applied to every item <typeparamref name="TInput"/>.
    /// </summary>
    /// <param name="mapper">Mapper configuration</param>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TOutput> Apply<TOutput>(Action<MapperConfiguration<TInput, TOutput>> mapper);

    /// <summary>
    /// Adds a transform block. Apply is a special kind of transform where the transformation is described declaratively
    /// as a mapper configuration. The mapper is applied to every item <typeparamref name="TInput"/>.
    /// </summary>
    /// <param name="mapper">Mapper configuration</param>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TInput> Apply(Action<MapperConfiguration<TInput, TInput>> mapper);

    /// <summary>
    /// Adds a test block. Test executes a predicate against each item and logs on failure. The test block does not
    /// transform the item.
    /// </summary>
    /// <param name="testPredicate">The predicate to execute</param>
    /// <param name="testIdentifierSelector">The property to identify the item from the log</param>
    /// <param name="logLevel">Log level</param>
    /// <param name="maxLogCount">Limits the number of logs produced</param>
    /// <typeparam name="TProperty">The identifier property type</typeparam>
    /// <returns>Pipeline block</returns>
    IPipelineBuilder<TInput, TInput> Test<TProperty>(
        Expression<Func<TInput, bool>> testPredicate,
        Expression<Func<TInput, TProperty>> testIdentifierSelector,
        LogLevel logLevel = LogLevel.Warning,
        int maxLogCount = 10);

    /// <summary>
    /// Adds a load block. A load block uses a write connector and a loader to push data to a destination.
    /// </summary>
    /// <param name="builder">Returns completed load block builder
    ///     <param name="builder arg1">Entry point for the load block builder</param>
    /// </param>
    /// <param name="options">Load block options</param>
    /// <returns>Completed pipeline definition</returns>
    Pipeline Load(
        Func<ILoadBuilder<TInput>, ILoadBuilderLoaderStep<TInput>> builder,
        LoadOptions? options = null);
}

public class PipelineBuilder(ILoggerFactory loggerFactory, ServiceFactory genericServiceFactory)
    : IPipelineBuilder
{
     private readonly PipelineBuilderContext _context = new(loggerFactory, genericServiceFactory);

     public IPipelineBuilder DependsOn<TDependsOn>()
         where TDependsOn : class
     {
         _context.DependencyResolution.AddSourceEntity(typeof(TDependsOn));

         return this;
     }
     
     public IPipelineBuilder<TOutput, TOutput> Extract<TOutput>(
         Func<IExtractBuilder<TOutput, TOutput>, IExtractBuilderExtractorStep<TOutput, TOutput>> builder,
         ExtractOptions? options = null)
     {
         var extractBuilder = new ExtractBuilder<TOutput, TOutput>(_context, options);

         var extractorStep = builder(extractBuilder);

         var connectorStep = (IExtractBuilderConnectorStepInternal<TOutput, TOutput>)extractorStep;
         if (!connectorStep.HasExtractor)
         {
             connectorStep.AddDefaultExtractor();
         }

         return new PipelineBuilder<TOutput, TOutput>(_context);
     }
 }

public class PipelineBuilder<TPrevious, TInput>(PipelineBuilderContext context) 
    : IPipelineBuilder<TPrevious, TInput>
{
    private readonly PipelineBuilderContext _context = context;

    internal PipelineBuilderContext Context => _context;

    public IPipelineBuilder<TInput, TOutput> Extract<TOutput>(
        Func<IExtractBuilder<TInput, TOutput>, IExtractBuilderExtractorStep<TInput, TOutput>> builder,
        ExtractOptions? options = null)
    {
        var extractBuilder = new ExtractBuilder<TInput, TOutput>(_context, options);

        var extractorStep = builder(extractBuilder);

        var connectorStep = (IExtractBuilderConnectorStepInternal<TInput, TOutput>)extractorStep;
        if (!connectorStep.HasExtractor)
        {
            connectorStep.AddDefaultExtractor();
        }

        return new PipelineBuilder<TInput, TOutput>(_context);
    }

    public IPipelineBuilder<TInput, TOutput> Extract<TOutput>(
        Func<IExtractBuilder<TInput, TOutput>, TInput, IExtractBuilderExtractorStep<TInput, TOutput>> builder,
        ExtractOptions? options = null)
    {
        _context.Blocks.Enqueue(new DeferredExtractBlock<TInput, TOutput>(builder, _context.ServiceFactory, _context.LoggerFactory, options ?? new ExtractOptions()));

        return new PipelineBuilder<TInput, TOutput>(_context);
    }

    public IPipelineBuilder<TInput, TOutput> Transform<TOutput>(
        Func<TInput[], Task<TOutput[]>> transform,
        TransformOptions? options = null)
    {
        var transformer = _context.ServiceFactory.CreateAsyncTransformer<AsyncTransformer<TInput, TOutput>, TInput, TOutput>();
        transformer.TransformDelegate = transform;
        
        _context.Blocks.Enqueue(new TransformBlock<TInput, TOutput>(transformer, _context.LoggerFactory, options ?? new TransformOptions()));
        
        return new PipelineBuilder<TInput, TOutput>(_context);
    }

    public IPipelineBuilder<TInput, TOutput> Transform<TOutput>(
        Func<TInput[], TOutput[]> transform,
        TransformOptions? options = null)
    {
        var transformer = _context.ServiceFactory.CreateTransformer<Transformer<TInput, TOutput>, TInput, TOutput>();
        transformer.TransformDelegate = transform;
        
        _context.Blocks.Enqueue(new TransformBlock<TInput, TOutput>(transformer, _context.LoggerFactory, options ?? new TransformOptions()));
        
        return new PipelineBuilder<TInput, TOutput>(_context);
    }

    public IPipelineBuilder<TInput, TOutput> Transform<TOutput>(
        Func<TInput[], IEnumerable<TOutput>> transform,
        TransformOptions? options = null)
    {
        var transformer = _context.ServiceFactory.CreateTransformer<Transformer<TInput, TOutput>, TInput, TOutput>();
        transformer.TransformDelegate = input => transform(input).ToArray();
        
        _context.Blocks.Enqueue(new TransformBlock<TInput, TOutput>(transformer, _context.LoggerFactory, options ?? new TransformOptions()));
        
        return new PipelineBuilder<TInput, TOutput>(_context);
    }

    public IPipelineBuilder<TInput, TOutput> Apply<TOutput>(Action<MapperConfiguration<TInput, TOutput>> mapper)
    {
        var mapperConfiguration = new MapperConfiguration<TInput, TOutput>();
        mapper(mapperConfiguration);
        
        var transformer = _context.ServiceFactory.CreateTransformer<MapperTransformer<TInput, TOutput>, TInput, TOutput>();
        transformer.Mapper = new Mapper<TInput, TOutput>(mapperConfiguration);
        
        _context.Blocks.Enqueue(new TransformBlock<TInput, TOutput>(transformer, _context.LoggerFactory, new TransformOptions()));
        
        return new PipelineBuilder<TInput, TOutput>(_context);
    }

    public IPipelineBuilder<TInput, TInput> Apply(Action<MapperConfiguration<TInput, TInput>> mapper) => 
        Apply<TInput>(mapper);

    public IPipelineBuilder<TInput, TInput> Test<TProperty>(
        Expression<Func<TInput, bool>> testPredicate,
        Expression<Func<TInput, TProperty>> testIdentifierSelector,
        LogLevel logLevel = LogLevel.Warning,
        int maxLogCount = 10)
    {
        var transformer = _context.ServiceFactory.CreateTransformer<TestTransformer<TInput, TInput, TProperty>, TInput, TInput>();
        transformer.Init(testPredicate, testIdentifierSelector, _context.LoggerFactory, logLevel, maxLogCount);

        _context.Blocks.Enqueue(new TransformBlock<TInput, TInput>(transformer, _context.LoggerFactory, new TransformOptions()));

        return new PipelineBuilder<TInput, TInput>(_context);
    }

    public Pipeline Load(
        Func<ILoadBuilder<TInput>, ILoadBuilderLoaderStep<TInput>> builder,
        LoadOptions? options = null)
    {
        var loadBuilder = new LoadBuilder<TInput>(_context, options);
        
        var loaderStep = builder(loadBuilder);

        var connectorStep = (ILoadBuilderConnectorInternal<TInput>)loaderStep;
        if (!connectorStep.HasLoader)
        {
            connectorStep.AddDefaultLoader();
        }

        var contextClone = _context.Clone();
        _context.Reset();

        return new Pipeline(contextClone);
    }
}

public static class PipelineBuilderExtensions
{
    public static Pipeline Ensure<TInput>(
        this IPipelineBuilder<TInput, TInput> pipelineBuilder,
        Expression<Func<TInput, bool>> testPredicate)
        where TInput : ITestResult
    {
        if (pipelineBuilder is not PipelineBuilder<TInput, TInput> pipelineBuilderInternal)
        {
            throw new ArgumentException();
        }

        var transformer = pipelineBuilderInternal.Context.ServiceFactory.CreateTransformer<EnsureTransformer<TInput, TInput>, TInput, TInput>();
        transformer.PredicateBody = testPredicate.Body.ToString();
        transformer.Predicate = testPredicate.Compile();

        pipelineBuilderInternal.Context.Blocks.Enqueue(new TransformBlock<TInput, TInput>(transformer, pipelineBuilderInternal.Context.LoggerFactory, new TransformOptions()));

        return pipelineBuilder.Load(builder => builder.WithConnector<NullWriteConnector<TInput>>());
    }
}
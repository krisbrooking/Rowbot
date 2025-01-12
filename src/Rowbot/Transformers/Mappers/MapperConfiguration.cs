using Rowbot.Transformers.Mappers.Actions;
using Rowbot.Transformers.Mappers.Configuration;

namespace Rowbot;

public interface IMapperConfiguration { }

/// <summary>
/// Builder for configuring mappers.
/// </summary>
/// <typeparam name="TSource">Source entity</typeparam>
/// <typeparam name="TTarget">Target entity</typeparam>
public sealed class MapperConfiguration<TSource, TTarget> : IMapperConfiguration
{
    private readonly List<ISourceMapperAction<TSource, TTarget>> _sourceMapperActions;
    private readonly List<ITargetMapperAction<TTarget>> _targetMapperActions;

    public MapperConfiguration()
    {
        _sourceMapperActions = new List<ISourceMapperAction<TSource, TTarget>>();
        _targetMapperActions = new List<ITargetMapperAction<TTarget>>();

        Map = new PropertyMapperConfiguration<TSource, TTarget>(this, mapper => _sourceMapperActions.Add(mapper));
        Transform = new TransformMapperConfiguration<TSource, TTarget>(this, mapper => _sourceMapperActions.Add(mapper), mapper => _targetMapperActions.Add(mapper));
    }

    /// <summary>
    /// Map individual properties or entire entity from source to target
    /// </summary>
    public PropertyMapperConfiguration<TSource, TTarget> Map { get; set; }
    /// <summary>
    /// Transform properties from source to target or in-place on target
    /// </summary>
    public TransformMapperConfiguration<TSource, TTarget> Transform { get; set; }

    internal IEnumerable<ISourceMapperAction<TSource, TTarget>> BuildSourceMappers() => _sourceMapperActions;
    internal IEnumerable<ITargetMapperAction<TTarget>> BuildTargetMappers() => _targetMapperActions;
}

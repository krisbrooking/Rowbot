namespace Rowbot.Transformers.Mappers.Actions;

/// <summary>
/// <para>
/// Copies and/or transforms values of properties from a source to a target entity.
/// </para>
/// <para>
/// A mapper action has a <see cref="SourceMapperActionType"/> which acts as a sort order and 
/// is used when mapper actions are applied. The apply order allows mapper actions to be
/// layered (subsequent action is applied over top of previous action result) or overridden.
/// </para>
/// <para>
/// Apply order = <see cref="SourceMapperActionType.Property"/>, <see cref="SourceMapperActionType.Transform"/>
/// </para>
/// </summary>
/// <typeparam name="TSource">Source entity</typeparam>
/// <typeparam name="TTarget">Target entity</typeparam>
public interface ISourceMapperAction<TSource, TTarget>
{
    /// <summary>
    /// Type of mapper action
    /// </summary>
    SourceMapperActionType ActionType { get; }
    /// <summary>
    /// Invoke the <see cref="Action"/> to apply the mapper
    /// </summary>
    Action<TSource, TTarget> Apply { get; }
}
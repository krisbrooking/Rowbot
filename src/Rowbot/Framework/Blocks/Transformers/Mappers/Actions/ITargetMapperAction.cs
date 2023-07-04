﻿namespace Rowbot.Framework.Blocks.Transformers.Mappers.Actions
{
    /// <summary>
    /// <para>
    /// A mapper action is a wrapper for an action delegate that copies and/or transforms
    /// values of properties from a source to a target entity.
    /// </para>
    /// <para>
    /// A mapper action has a <see cref="TargetMapperActionType"/> which acts as a sort order and 
    /// is used when mapper actions are applied. The apply order allows mapper actions to be
    /// layered (subsequent action is applied over top of previous action result) or overridden.
    /// </para>
    /// <para>
    /// Apply order = <see cref="TargetMapperActionType.Transform"/>
    /// </para>
    /// </summary>
    /// <typeparam name="TTarget">Target entity</typeparam>
    public interface ITargetMapperAction<TTarget>
    {
        /// <summary>
        /// Type of mapper action
        /// </summary>
        TargetMapperActionType ActionType { get; }
        /// <summary>
        /// Invoke the <see cref="Action"/> to apply the mapper
        /// </summary>
        Action<TTarget> Apply { get; }
    }
}

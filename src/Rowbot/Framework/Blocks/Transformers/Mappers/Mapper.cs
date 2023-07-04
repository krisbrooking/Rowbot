using Rowbot.Framework.Blocks.Transformers.Mappers.Actions;

namespace Rowbot
{
    /// <summary>
    /// Configures and applies entity, property, and transform mappers from source to target.
    /// </summary>
    /// <typeparam name="TSource">Source entity</typeparam>
    /// <typeparam name="TTarget">Target entity</typeparam>
    public class Mapper<TSource, TTarget>
    {
        private readonly MapperConfiguration<TSource, TTarget> _configuration;
        private readonly IEnumerable<ISourceMapperAction<TSource, TTarget>> _sourceMapperActions;
        private readonly IEnumerable<ITargetMapperAction<TTarget>> _targetMapperActions;

        public Mapper()
        {
            _configuration = new MapperConfiguration<TSource, TTarget>();
            _sourceMapperActions = _configuration.BuildSourceMappers();
            _targetMapperActions = _configuration.BuildTargetMappers();
            IsDefault = true;
        }

        public Mapper(MapperConfiguration<TSource, TTarget> configuration)
        {
            _configuration = configuration;
            _sourceMapperActions = _configuration.BuildSourceMappers();
            _targetMapperActions = _configuration.BuildTargetMappers();
            IsDefault = false;
        }

        public Mapper(Action<MapperConfiguration<TSource, TTarget>> configure)
        {
            _configuration = new();
            configure?.Invoke(_configuration);
            _sourceMapperActions = _configuration.BuildSourceMappers();
            _targetMapperActions = _configuration.BuildTargetMappers();
            IsDefault = false;
        }

        public bool IsDefault { get; init; }

        /// <summary>
        /// Apply mappers to a single source entity
        /// </summary>
        public TTarget Apply(TSource source)
        {
            var target = Activator.CreateInstance<TTarget>();

            foreach (var mapperAction in _sourceMapperActions.OrderBy(x => x.ActionType))
            {
                mapperAction.Apply(source, target);
            }

            foreach (var mapperAction in _targetMapperActions.OrderBy(x => x.ActionType))
            {
                mapperAction.Apply(target);
            }

            return target;
        }

        /// <summary>
        /// Apply mappers to a single source entity
        /// </summary>
        public TTarget ApplySource(TSource source) => Apply(source);
        public TTarget[] ApplySource(TSource[] source) => Apply(source);

        /// <summary>
        /// Apply mappers to a single target entity
        /// </summary>
        public TTarget Apply(TTarget target)
        {
            foreach (var mapperAction in _targetMapperActions.OrderBy(x => x.ActionType))
            {
                mapperAction.Apply(target);
            }

            return target;
        }

        /// <summary>
        /// Apply mappers to a single target entity
        /// </summary>
        public TTarget ApplyTarget(TTarget target) => Apply(target);
        public TTarget[] ApplyTarget(TTarget[] target) => Apply(target);

        /// <summary>
        /// Apply mappers to an array of source entities
        /// </summary>
        public TTarget[] Apply(TSource[] source)
        {
            var target = new TTarget[source.Length];

            for (var i = 0; i < source.Length; i++)
            {
                target[i] = Apply(source[i]);
            }

            return target;
        }

        /// <summary>
        /// Apply mappers to an array of target entities
        /// </summary>
        public TTarget[] Apply(TTarget[] target)
        {
            for (var i = 0; i < target.Length; i++)
            {
                target[i] = Apply(target[i]);
            }

            return target;
        }

        public static implicit operator Mapper<TSource, TTarget>(Action<MapperConfiguration<TSource, TTarget>> configure)
        {
            var configuration = new MapperConfiguration<TSource, TTarget>();
            configure?.Invoke(configuration);

            return new Mapper<TSource, TTarget>(configuration);
        }
    }
}

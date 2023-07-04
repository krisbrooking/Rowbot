using System.Collections.Concurrent;

namespace Rowbot.Framework.Blocks.Connectors.Synchronisation
{
    public interface ISharedLockManager
    {
        SharedReadLock GetSharedReadLock(string name);
        SharedWriteLock GetSharedWriteLock(string name);
    }

    public sealed class SharedLockManager : ISharedLockManager
    {
        private readonly ConcurrentDictionary<string, Lazy<ReaderWriterLockSlim>> _sharedLocks;

        public SharedLockManager()
        {
            _sharedLocks = new();
        }

        public SharedReadLock GetSharedReadLock(string name) => new SharedReadLock(GetSharedLock(name));
        public SharedWriteLock GetSharedWriteLock(string name) => new SharedWriteLock(GetSharedLock(name));

        private ReaderWriterLockSlim GetSharedLock(string name) =>
            _sharedLocks.GetOrAdd(name,
                x => new Lazy<ReaderWriterLockSlim>(
                    () => new ReaderWriterLockSlim()
                )
            ).Value;
    }
}

﻿namespace Rowbot.Connectors.Common.Synchronisation;

public class SharedReadLock : IDisposable
{
    internal ReaderWriterLockSlim LockObject { get; }

    /// <summary>
    /// <para>
    /// Creates a new, or references an existing instance of a shared lock. Uses <see cref="ReaderWriterLockSlim" />, 
    /// therefore, the shared lock allows multiple threads for reading or exclusive access for writing.
    /// </para>
    /// <para>
    /// Rowbot is responsible for sharing (storing and providing) instances of the lock.
    /// </para>
    /// </summary>
    public SharedReadLock(ReaderWriterLockSlim lockObject)
    {
        LockObject = lockObject;
        LockObject.EnterReadLock();
    }

    public void Dispose()
    {
        LockObject.ExitReadLock();
    }

    public static implicit operator ReaderWriterLockSlim(SharedReadLock sharedLock) => sharedLock.LockObject;
}
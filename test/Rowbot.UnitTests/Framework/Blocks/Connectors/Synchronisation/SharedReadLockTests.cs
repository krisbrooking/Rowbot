using Rowbot.Framework.Blocks.Connectors.Synchronisation;
using System.Threading;
using Xunit;

namespace Rowbot.UnitTests.Framework.Blocks.Connectors.Synchronisation
{
    public class SharedReadLockTests
    {
        [Fact]
        public void SharedReadLock_Should_ExitReadLock_OnDispose()
        {
            var isLockHeldBeforeDispose = false;
            var isLockHeldAfterDispose = true;

            var readLock = new SharedReadLock(new ReaderWriterLockSlim());
            isLockHeldBeforeDispose = ((ReaderWriterLockSlim)readLock).IsReadLockHeld;
            readLock.Dispose();
            isLockHeldAfterDispose = ((ReaderWriterLockSlim)readLock).IsReadLockHeld;

            Assert.True(isLockHeldBeforeDispose);
            Assert.False(isLockHeldAfterDispose);
        }

        [Fact]
        public void SharedReadLock_Should_ReturnSameLockObject_WhenTwoLocksHaveSameName()
        {
            var lockManager = new SharedLockManager();

            var readLock1 = lockManager.GetSharedReadLock("Lock1");
            var hashCode1 = ((ReaderWriterLockSlim)readLock1).GetHashCode();
            readLock1.Dispose();
            var readLock2 = lockManager.GetSharedReadLock("Lock1");
            var hashCode2 = ((ReaderWriterLockSlim)readLock2).GetHashCode();
            readLock2.Dispose();

            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void SharedReadLock_Should_ReturnDifferentLockObject_WhenTwoLocksHaveDifferentNames()
        {
            var lockManager = new SharedLockManager();

            var readLock1 = lockManager.GetSharedReadLock("Lock1");
            var hashCode1 = ((ReaderWriterLockSlim)readLock1).GetHashCode();
            readLock1.Dispose();
            var readLock2 = lockManager.GetSharedReadLock("Lock2");
            var hashCode2 = ((ReaderWriterLockSlim)readLock2).GetHashCode();
            readLock2.Dispose();

            Assert.NotEqual(hashCode1, hashCode2);
        }
    }
}

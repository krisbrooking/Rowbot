using Rowbot.Connectors.Common.Synchronisation;

namespace Rowbot.UnitTests.Connectors.Common.Synchronisation
{
    public class SharedWriteLockTests
    {
        [Fact]
        public void SharedWriteLock_Should_ExitWriteLock_OnDispose()
        {
            var isLockHeldBeforeDispose = false;
            var isLockHeldAfterDispose = true;

            var writeLock = new SharedWriteLock(new ReaderWriterLockSlim());
            isLockHeldBeforeDispose = ((ReaderWriterLockSlim)writeLock).IsWriteLockHeld;
            writeLock.Dispose();
            isLockHeldAfterDispose = ((ReaderWriterLockSlim)writeLock).IsWriteLockHeld;

            Assert.True(isLockHeldBeforeDispose);
            Assert.False(isLockHeldAfterDispose);
        }

        [Fact]
        public void SharedWriteLock_Should_ReturnSameLockObject_WhenTwoLocksHaveSameName()
        {
            var lockManager = new SharedLockManager();

            var writeLock1 = lockManager.GetSharedWriteLock("Lock1");
            var hashCode1 = ((ReaderWriterLockSlim)writeLock1).GetHashCode();
            writeLock1.Dispose();
            var writeLock2 = lockManager.GetSharedWriteLock("Lock1");
            var hashCode2 = ((ReaderWriterLockSlim)writeLock2).GetHashCode();
            writeLock2.Dispose();

            Assert.Equal(hashCode1, hashCode2);
        }

        [Fact]
        public void SharedWriteLock_Should_ReturnDifferentLockObject_WhenTwoLocksHaveDifferentNames()
        {
            var lockManager = new SharedLockManager();

            var writeLock1 = lockManager.GetSharedWriteLock("Lock1");
            var hashCode1 = ((ReaderWriterLockSlim)writeLock1).GetHashCode();
            writeLock1.Dispose();
            var writeLock2 = lockManager.GetSharedWriteLock("Lock2");
            var hashCode2 = ((ReaderWriterLockSlim)writeLock2).GetHashCode();
            writeLock2.Dispose();

            Assert.NotEqual(hashCode1, hashCode2);
        }
    }
}

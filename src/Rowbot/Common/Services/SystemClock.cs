namespace Rowbot.Common.Services
{
    public interface ISystemClock
    {
        DateTimeOffset UtcNow { get; }
        DateTimeOffset LocalNow { get; }
    }

    public sealed class SystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTimeOffset LocalNow => DateTimeOffset.Now;
    }
}

namespace Cronograph.Shared;
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
    long EpochUtcNow { get; }
}
public class DateTimeService : IDateTime
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public long EpochUtcNow => DateTimeOffset.UtcNow.ToEpoch();
}
public static class DateTimeExtensions
{
    public static long ToEpoch(this DateTimeOffset dateTime) =>
        (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
}
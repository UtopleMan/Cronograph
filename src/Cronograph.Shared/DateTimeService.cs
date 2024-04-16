namespace Cronograph.Shared;
public class DateTimeService : IDateTime
{
    DateTimeOffset IDateTime.UtcNow => DateTimeOffset.UtcNow;
    long IDateTime.EpochUtcNow => (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
}

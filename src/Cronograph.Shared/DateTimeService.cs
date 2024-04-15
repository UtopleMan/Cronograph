namespace Cronograph.Shared;
public class DateTimeService : IDateTime
{
    DateTimeOffset IDateTime.UtcNow => DateTimeOffset.UtcNow;
}

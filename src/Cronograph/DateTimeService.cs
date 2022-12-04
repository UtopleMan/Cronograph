using System;

namespace Cronograph;

internal class DateTimeService : IDateTime
{
    DateTimeOffset IDateTime.UtcNow => DateTimeOffset.UtcNow;
}

using System;

namespace Cronograph;

public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}

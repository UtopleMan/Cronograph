﻿namespace Cronograph.Shared;
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
    long EpochUtcNow { get; }
}

namespace Cronograph.Shared;
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}

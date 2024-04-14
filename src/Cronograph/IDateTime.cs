namespace Cronograph;
public interface IDateTime
{
    DateTimeOffset UtcNow { get; }
}

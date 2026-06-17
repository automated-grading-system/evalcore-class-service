namespace Class.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

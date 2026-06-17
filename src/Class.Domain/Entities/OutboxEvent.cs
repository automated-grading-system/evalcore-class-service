namespace Class.Domain.Entities;

public sealed class OutboxEvent
{
    public Guid Id { get; set; }

    public string EventType { get; set; } = string.Empty;

    public int EventVersion { get; set; }

    public string RoutingKey { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int PublishAttempts { get; set; }

    public string? LastError { get; set; }
}

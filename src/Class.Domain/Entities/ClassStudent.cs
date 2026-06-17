namespace Class.Domain.Entities;

public sealed class ClassStudent
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public Guid StudentId { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}

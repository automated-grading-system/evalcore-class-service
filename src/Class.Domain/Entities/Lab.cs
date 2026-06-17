namespace Class.Domain.Entities;

public sealed class Lab
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string RequirementObjectKey { get; set; } = string.Empty;

    public string CollectionObjectKey { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset Deadline { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? AssetsCompletedAt { get; set; }
}

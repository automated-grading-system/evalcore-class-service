namespace Class.Application.Dto;

public sealed record LabDto(
    Guid Id,
    Guid ClassId,
    string Title,
    string? Description,
    string RequirementObjectKey,
    string CollectionObjectKey,
    string Status,
    DateTimeOffset Deadline,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? AssetsCompletedAt);

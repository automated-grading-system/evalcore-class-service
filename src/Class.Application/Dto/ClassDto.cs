namespace Class.Application.Dto;

public sealed record ClassDto(
    Guid Id,
    string Name,
    string? Description,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

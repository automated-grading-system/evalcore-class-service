namespace Class.Application.Dto;

public sealed record ClassStudentDto(
    Guid Id,
    Guid ClassId,
    Guid StudentId,
    DateTimeOffset JoinedAt);

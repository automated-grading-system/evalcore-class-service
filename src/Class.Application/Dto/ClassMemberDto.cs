namespace Class.Application.Dto;

public sealed record ClassMemberDto(
    Guid Id,
    Guid ClassId,
    Guid StudentId,
    string? StudentName,
    string? StudentEmail,
    DateTimeOffset JoinedAt);

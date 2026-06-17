namespace Class.Application.Dto;

public sealed record PresignedUrlDto(string Url, DateTimeOffset ExpiresAt);

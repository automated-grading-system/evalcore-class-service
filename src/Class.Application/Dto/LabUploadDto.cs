namespace Class.Application.Dto;

public sealed record LabUploadDto(
    string RequirementUploadUrl,
    string CollectionUploadUrl,
    DateTimeOffset ExpiresAt);

using System.ComponentModel.DataAnnotations;

namespace Class.Application.Requests;

public sealed class CreateLabRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTimeOffset Deadline { get; set; }

    [Required]
    public string RequirementFileName { get; set; } = string.Empty;

    [Required]
    public string CollectionFileName { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace Class.Application.Requests;

public sealed class UpdateLabRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTimeOffset Deadline { get; set; }
}

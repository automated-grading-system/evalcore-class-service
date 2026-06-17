using System.ComponentModel.DataAnnotations;

namespace Class.Application.Requests;

public sealed class UpdateClassRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

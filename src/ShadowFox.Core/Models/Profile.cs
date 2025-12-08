using System.ComponentModel.DataAnnotations;

namespace ShadowFox.Core.Models;

public class Profile
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Tags { get; set; }

    [MaxLength(200)]
    public string? Group { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(4000)]
    public string FingerprintJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastOpenedAt { get; set; }
}

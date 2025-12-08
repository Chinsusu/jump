using System.ComponentModel.DataAnnotations;

namespace ShadowFox.Core.Models;

public class Group
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

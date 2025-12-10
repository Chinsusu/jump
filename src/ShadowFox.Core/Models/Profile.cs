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

    public int? GroupId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    [MaxLength(4000)]
    public string FingerprintJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastOpenedAt { get; set; }

    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    public int UsageCount { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    // Navigation property
    public Group? Group { get; set; }

    // Factory methods for different creation scenarios
    public static Profile CreateNew(string name, string fingerprintJson, int? groupId = null, string? tags = null, string? notes = null)
    {
        var now = DateTime.UtcNow;
        return new Profile
        {
            Name = name,
            FingerprintJson = fingerprintJson,
            GroupId = groupId,
            Tags = tags,
            Notes = notes,
            CreatedAt = now,
            LastModifiedAt = now,
            IsActive = true,
            UsageCount = 0
        };
    }

    public static Profile CreateFromClone(Profile source, string newName, string newFingerprintJson)
    {
        var now = DateTime.UtcNow;
        return new Profile
        {
            Name = newName,
            FingerprintJson = newFingerprintJson,
            GroupId = source.GroupId,
            Tags = source.Tags,
            Notes = source.Notes,
            CreatedAt = now,
            LastModifiedAt = now,
            IsActive = true,
            UsageCount = 0
        };
    }

    public static Profile CreateFromImport(string name, string fingerprintJson, int? groupId = null, string? tags = null, string? notes = null)
    {
        var now = DateTime.UtcNow;
        return new Profile
        {
            Name = name,
            FingerprintJson = fingerprintJson,
            GroupId = groupId,
            Tags = tags,
            Notes = notes,
            CreatedAt = now,
            LastModifiedAt = now,
            IsActive = true,
            UsageCount = 0
        };
    }

    public void UpdateLastOpened()
    {
        LastOpenedAt = DateTime.UtcNow;
        UsageCount++;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateModified()
    {
        LastModifiedAt = DateTime.UtcNow;
    }
}

namespace ShadowFox.Core.Models;

public static class ProfileExtensions
{
    public static Profile CloneProfile(this Profile source)
    {
        return new Profile
        {
            Id = source.Id,
            Name = source.Name,
            Tags = source.Tags,
            Group = source.Group,
            Notes = source.Notes,
            FingerprintJson = source.FingerprintJson,
            CreatedAt = source.CreatedAt,
            LastOpenedAt = source.LastOpenedAt
        };
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
using ShadowFox.Core.Models;

namespace ShadowFox.Core.Validation;

/// <summary>
/// Provides validation logic for Profile entities and their fingerprint properties.
/// </summary>
public static class ProfileValidator
{
    private static readonly Regex UserAgentPattern = new(
        @"^Mozilla\/5\.0 \(.+\) AppleWebKit\/[\d.]+ \(KHTML, like Gecko\) Chrome\/[\d.]+ Safari\/[\d.]+$",
        RegexOptions.Compiled);

    private static readonly string[] ValidPlatforms = { "Win32", "Win64", "MacIntel", "Linux x86_64", "Linux i686" };
    
    private static readonly string[] ValidTimezones = 
    {
        "America/New_York", "America/Chicago", "America/Los_Angeles", "America/Denver",
        "Europe/London", "Europe/Paris", "Europe/Berlin", "Europe/Madrid", "Europe/Rome",
        "Asia/Shanghai", "Asia/Tokyo", "Asia/Seoul", "Asia/Kolkata", "Asia/Dubai",
        "Asia/Ho_Chi_Minh", "Australia/Sydney", "Australia/Melbourne", "Pacific/Auckland"
    };

    private static readonly string[] ValidLocales = 
    {
        "en-US", "en-GB", "en-CA", "en-AU", "de-DE", "fr-FR", "es-ES", "it-IT",
        "pt-BR", "ru-RU", "zh-CN", "zh-TW", "ja-JP", "ko-KR", "ar-SA", "hi-IN"
    };

    /// <summary>
    /// Validates a Profile entity.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <returns>A validation result indicating success or failure with error details.</returns>
    public static ValidationResult ValidateProfile(Profile profile)
    {
        var result = new ValidationResult();

        if (profile == null)
        {
            result.AddError("Profile cannot be null.");
            return result;
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            result.AddError("Profile name is required.");
        }
        else if (profile.Name.Length > 200)
        {
            result.AddError("Profile name cannot exceed 200 characters.");
        }

        // Validate tags
        if (!string.IsNullOrEmpty(profile.Tags) && profile.Tags.Length > 500)
        {
            result.AddError("Profile tags cannot exceed 500 characters.");
        }

        // Validate group (GroupId is nullable int, Group is navigation property)
        if (profile.Group != null && !string.IsNullOrEmpty(profile.Group.Name) && profile.Group.Name.Length > 200)
        {
            result.AddError("Profile group name cannot exceed 200 characters.");
        }

        // Validate notes
        if (!string.IsNullOrEmpty(profile.Notes) && profile.Notes.Length > 1000)
        {
            result.AddError("Profile notes cannot exceed 1000 characters.");
        }

        // Validate fingerprint JSON
        if (string.IsNullOrWhiteSpace(profile.FingerprintJson))
        {
            result.AddError("Profile fingerprint JSON is required.");
        }
        else if (profile.FingerprintJson.Length > 4000)
        {
            result.AddError("Profile fingerprint JSON cannot exceed 4000 characters.");
        }
        else
        {
            // Validate fingerprint structure
            var fingerprintValidation = ValidateFingerprintJson(profile.FingerprintJson);
            result.AddErrors(fingerprintValidation.Errors);
        }

        // Validate timestamps
        if (profile.CreatedAt == default)
        {
            result.AddError("Profile creation timestamp is required.");
        }

        return result;
    }

    /// <summary>
    /// Validates a Fingerprint object.
    /// </summary>
    /// <param name="fingerprint">The fingerprint to validate.</param>
    /// <returns>A validation result indicating success or failure with error details.</returns>
    public static ValidationResult ValidateFingerprint(Fingerprint fingerprint)
    {
        var result = new ValidationResult();

        if (fingerprint == null)
        {
            result.AddError("Fingerprint cannot be null.");
            return result;
        }

        // Validate User Agent
        if (string.IsNullOrWhiteSpace(fingerprint.UserAgent))
        {
            result.AddError("User Agent is required.");
        }
        else if (!UserAgentPattern.IsMatch(fingerprint.UserAgent))
        {
            result.AddError("User Agent format is invalid. Must be a valid Chrome user agent string.");
        }

        // Validate Platform
        if (string.IsNullOrWhiteSpace(fingerprint.Platform))
        {
            result.AddError("Platform is required.");
        }
        else if (!ValidPlatforms.Contains(fingerprint.Platform))
        {
            result.AddError($"Platform must be one of: {string.Join(", ", ValidPlatforms)}");
        }

        // Validate Hardware Concurrency
        if (fingerprint.HardwareConcurrency < 1 || fingerprint.HardwareConcurrency > 32)
        {
            result.AddError("Hardware concurrency must be between 1 and 32.");
        }

        // Validate Device Memory
        if (fingerprint.DeviceMemory < 1 || fingerprint.DeviceMemory > 128)
        {
            result.AddError("Device memory must be between 1 and 128 GB.");
        }

        // Validate Screen Resolution
        if (fingerprint.ScreenWidth < 800 || fingerprint.ScreenWidth > 7680)
        {
            result.AddError("Screen width must be between 800 and 7680 pixels.");
        }

        if (fingerprint.ScreenHeight < 600 || fingerprint.ScreenHeight > 4320)
        {
            result.AddError("Screen height must be between 600 and 4320 pixels.");
        }

        // Validate Device Pixel Ratio
        if (fingerprint.DevicePixelRatio < 0.5 || fingerprint.DevicePixelRatio > 4.0)
        {
            result.AddError("Device pixel ratio must be between 0.5 and 4.0.");
        }

        // Validate Timezone
        if (string.IsNullOrWhiteSpace(fingerprint.Timezone))
        {
            result.AddError("Timezone is required.");
        }
        else if (!ValidTimezones.Contains(fingerprint.Timezone))
        {
            result.AddError($"Timezone must be a valid IANA timezone identifier.");
        }

        // Validate Locale
        if (string.IsNullOrWhiteSpace(fingerprint.Locale))
        {
            result.AddError("Locale is required.");
        }
        else if (!ValidLocales.Contains(fingerprint.Locale))
        {
            result.AddError($"Locale must be a valid locale identifier.");
        }

        // Validate Languages
        if (fingerprint.Languages == null || fingerprint.Languages.Length == 0)
        {
            result.AddError("At least one language is required.");
        }
        else if (fingerprint.Languages.Length > 10)
        {
            result.AddError("Cannot have more than 10 languages.");
        }

        // Validate WebGL properties
        if (string.IsNullOrWhiteSpace(fingerprint.WebGlUnmaskedVendor))
        {
            result.AddError("WebGL unmasked vendor is required.");
        }

        if (string.IsNullOrWhiteSpace(fingerprint.WebGlUnmaskedRenderer))
        {
            result.AddError("WebGL unmasked renderer is required.");
        }

        // Validate Noise Levels
        if (fingerprint.CanvasNoiseLevel < 0 || fingerprint.CanvasNoiseLevel > 1.0)
        {
            result.AddError("Canvas noise level must be between 0 and 1.0.");
        }

        if (fingerprint.AudioNoiseLevel < 0 || fingerprint.AudioNoiseLevel > 1.0)
        {
            result.AddError("Audio noise level must be between 0 and 1.0.");
        }

        // Validate Font List
        if (fingerprint.FontList == null || fingerprint.FontList.Length == 0)
        {
            result.AddError("At least one font is required.");
        }
        else if (fingerprint.FontList.Length > 200)
        {
            result.AddError("Cannot have more than 200 fonts.");
        }

        // Validate Spoof Level
        if (!Enum.IsDefined(typeof(SpoofLevel), fingerprint.SpoofLevel))
        {
            result.AddError("Spoof level must be a valid SpoofLevel value.");
        }

        return result;
    }

    /// <summary>
    /// Validates fingerprint JSON string by attempting to deserialize it.
    /// </summary>
    /// <param name="fingerprintJson">The fingerprint JSON string to validate.</param>
    /// <returns>A validation result indicating success or failure with error details.</returns>
    public static ValidationResult ValidateFingerprintJson(string fingerprintJson)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(fingerprintJson))
        {
            result.AddError("Fingerprint JSON cannot be empty.");
            return result;
        }

        try
        {
            var fingerprint = JsonSerializer.Deserialize<Fingerprint>(fingerprintJson);
            if (fingerprint != null)
            {
                var fingerprintValidation = ValidateFingerprint(fingerprint);
                result.AddErrors(fingerprintValidation.Errors);
            }
            else
            {
                result.AddError("Failed to deserialize fingerprint JSON.");
            }
        }
        catch (JsonException ex)
        {
            result.AddError($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            result.AddError($"Error validating fingerprint JSON: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Validates that a profile name is unique (placeholder for repository check).
    /// </summary>
    /// <param name="name">The profile name to validate.</param>
    /// <param name="excludeId">Optional ID to exclude from uniqueness check (for updates).</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static ValidationResult ValidateProfileNameUniqueness(string name, int? excludeId = null)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(name))
        {
            result.AddError("Profile name cannot be empty.");
            return result;
        }

        // Note: This is a placeholder. In actual implementation, this would check against the repository.
        // The repository layer will handle the actual uniqueness validation.
        
        return result;
    }
}
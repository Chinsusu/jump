using System.Text.Json;
using ShadowFox.Core.Validation;

namespace ShadowFox.Core.Models;

public record Fingerprint
{
    public string UserAgent { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public int HardwareConcurrency { get; init; }
    public int DeviceMemory { get; init; }
    public int ScreenWidth { get; init; }
    public int ScreenHeight { get; init; }
    public double DevicePixelRatio { get; init; }
    public string Timezone { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public string[] Languages { get; init; } = Array.Empty<string>();
    public string WebGlUnmaskedVendor { get; init; } = string.Empty;
    public string WebGlUnmaskedRenderer { get; init; } = string.Empty;
    public double CanvasNoiseLevel { get; init; }
    public double AudioNoiseLevel { get; init; }
    public string[] FontList { get; init; } = Array.Empty<string>();
    public SpoofLevel SpoofLevel { get; init; } = SpoofLevel.Ultra;

    // Factory methods for different creation scenarios
    public static Fingerprint CreateBasic(string userAgent, string platform, int screenWidth, int screenHeight)
    {
        return new Fingerprint
        {
            UserAgent = userAgent,
            Platform = platform,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            SpoofLevel = SpoofLevel.Basic,
            HardwareConcurrency = 4,
            DeviceMemory = 8,
            DevicePixelRatio = 1.0,
            Timezone = "America/New_York",
            Locale = "en-US",
            Languages = new[] { "en-US", "en" },
            WebGlUnmaskedVendor = "Intel Inc.",
            WebGlUnmaskedRenderer = "Intel(R) Iris(R) Xe Graphics",
            CanvasNoiseLevel = 0,
            AudioNoiseLevel = 0,
            FontList = new[] { "Arial", "Helvetica", "Times New Roman" }
        };
    }

    public static Fingerprint CreateAdvanced(string userAgent, string platform, int screenWidth, int screenHeight, 
        int hardwareConcurrency, int deviceMemory, string timezone, string locale)
    {
        return new Fingerprint
        {
            UserAgent = userAgent,
            Platform = platform,
            ScreenWidth = screenWidth,
            ScreenHeight = screenHeight,
            HardwareConcurrency = hardwareConcurrency,
            DeviceMemory = deviceMemory,
            DevicePixelRatio = 1.0,
            Timezone = timezone,
            Locale = locale,
            Languages = new[] { locale, locale.Split('-')[0] },
            WebGlUnmaskedVendor = "Intel Inc.",
            WebGlUnmaskedRenderer = "Intel(R) Iris(R) Xe Graphics",
            CanvasNoiseLevel = 0.01,
            AudioNoiseLevel = 0.0005,
            FontList = new[] { "Arial", "Helvetica", "Times New Roman", "Segoe UI", "Roboto" },
            SpoofLevel = SpoofLevel.Advanced
        };
    }

    // Validation methods
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(UserAgent))
            errors.Add("UserAgent cannot be empty");

        if (string.IsNullOrWhiteSpace(Platform))
            errors.Add("Platform cannot be empty");

        if (HardwareConcurrency < 1 || HardwareConcurrency > 32)
            errors.Add("HardwareConcurrency must be between 1 and 32");

        if (DeviceMemory < 1 || DeviceMemory > 128)
            errors.Add("DeviceMemory must be between 1 and 128 GB");

        if (ScreenWidth < 320 || ScreenWidth > 7680)
            errors.Add("ScreenWidth must be between 320 and 7680 pixels");

        if (ScreenHeight < 240 || ScreenHeight > 4320)
            errors.Add("ScreenHeight must be between 240 and 4320 pixels");

        if (DevicePixelRatio < 0.5 || DevicePixelRatio > 4.0)
            errors.Add("DevicePixelRatio must be between 0.5 and 4.0");

        if (string.IsNullOrWhiteSpace(Timezone))
            errors.Add("Timezone cannot be empty");

        if (string.IsNullOrWhiteSpace(Locale))
            errors.Add("Locale cannot be empty");

        if (Languages.Length == 0)
            errors.Add("Languages array cannot be empty");

        if (CanvasNoiseLevel < 0 || CanvasNoiseLevel > 0.1)
            errors.Add("CanvasNoiseLevel must be between 0 and 0.1");

        if (AudioNoiseLevel < 0 || AudioNoiseLevel > 0.01)
            errors.Add("AudioNoiseLevel must be between 0 and 0.01");

        if (FontList.Length == 0)
            errors.Add("FontList cannot be empty");

        return errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
    }

    // Cloning method that preserves data but regenerates noise
    public Fingerprint CloneWithNewNoise(Random? random = null)
    {
        var rng = random ?? new Random();
        
        // Preserve all data but regenerate noise values based on spoof level
        var newCanvasNoise = SpoofLevel switch
        {
            SpoofLevel.Basic => 0,
            SpoofLevel.Advanced => rng.NextDouble() * 0.02 + 0.005, // 0.005-0.025
            SpoofLevel.Ultra => rng.NextDouble() * 0.06 + 0.02,     // 0.02-0.08
            _ => CanvasNoiseLevel
        };

        var newAudioNoise = SpoofLevel switch
        {
            SpoofLevel.Basic => 0,
            SpoofLevel.Advanced => rng.NextDouble() * 0.0008 + 0.0002, // 0.0002-0.001
            SpoofLevel.Ultra => rng.NextDouble() * 0.0014 + 0.0001,    // 0.0001-0.0015
            _ => AudioNoiseLevel
        };

        return this with 
        { 
            CanvasNoiseLevel = newCanvasNoise,
            AudioNoiseLevel = newAudioNoise
        };
    }

    // Serialization methods
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        return JsonSerializer.Serialize(this, options);
    }

    public static Fingerprint FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON cannot be null or empty", nameof(json));

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var fingerprint = JsonSerializer.Deserialize<Fingerprint>(json, options);
            return fingerprint ?? throw new InvalidOperationException("Deserialization resulted in null");
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON format: {ex.Message}", nameof(json), ex);
        }
    }

    // Check if fingerprint has characteristics appropriate for its spoof level
    public bool HasValidSpoofLevelCharacteristics()
    {
        return SpoofLevel switch
        {
            SpoofLevel.Basic => CanvasNoiseLevel == 0 && AudioNoiseLevel == 0,
            SpoofLevel.Advanced => CanvasNoiseLevel > 0 && CanvasNoiseLevel <= 0.03 && 
                                 AudioNoiseLevel > 0 && AudioNoiseLevel <= 0.002,
            SpoofLevel.Ultra => CanvasNoiseLevel >= 0.02 && CanvasNoiseLevel <= 0.08 && 
                              AudioNoiseLevel >= 0.0001 && AudioNoiseLevel <= 0.0015,
            _ => false
        };
    }
}

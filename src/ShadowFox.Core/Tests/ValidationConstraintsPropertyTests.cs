using FsCheck;
using FsCheck.Xunit;
using ShadowFox.Core.Models;
using ShadowFox.Core.Validation;
using Xunit;

namespace ShadowFox.Core.Tests;

public class ValidationConstraintsPropertyTests
{
    /// <summary>
    /// **Feature: profile-management, Property 17: Profile validation enforces constraints**
    /// **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ProfileValidationEnforcesConstraints()
    {
        return Prop.ForAll(
            GenerateInvalidFingerprint(),
            invalidFingerprint =>
            {
                // Act - Validate the invalid fingerprint
                var validationResult = ProfileValidator.ValidateFingerprint(invalidFingerprint);
                
                // Assert - Invalid fingerprints should fail validation
                var hasValidationErrors = !validationResult.IsValid && validationResult.Errors.Count > 0;
                
                // Test specific constraint violations based on the invalid data
                var hasScreenResolutionError = invalidFingerprint.ScreenWidth < 800 || invalidFingerprint.ScreenWidth > 7680 ||
                                             invalidFingerprint.ScreenHeight < 600 || invalidFingerprint.ScreenHeight > 4320;
                
                var hasHardwareConcurrencyError = invalidFingerprint.HardwareConcurrency < 1 || invalidFingerprint.HardwareConcurrency > 32;
                
                var hasDeviceMemoryError = invalidFingerprint.DeviceMemory < 1 || invalidFingerprint.DeviceMemory > 128;
                
                var hasDevicePixelRatioError = invalidFingerprint.DevicePixelRatio < 0.5 || invalidFingerprint.DevicePixelRatio > 4.0;
                
                var hasNoiseError = invalidFingerprint.CanvasNoiseLevel < 0 || invalidFingerprint.CanvasNoiseLevel > 1.0 ||
                                  invalidFingerprint.AudioNoiseLevel < 0 || invalidFingerprint.AudioNoiseLevel > 1.0;
                
                // If any constraint is violated, validation should fail
                var shouldHaveErrors = hasScreenResolutionError || hasHardwareConcurrencyError || 
                                     hasDeviceMemoryError || hasDevicePixelRatioError || hasNoiseError ||
                                     string.IsNullOrWhiteSpace(invalidFingerprint.UserAgent) ||
                                     string.IsNullOrWhiteSpace(invalidFingerprint.Platform) ||
                                     string.IsNullOrWhiteSpace(invalidFingerprint.Timezone) ||
                                     string.IsNullOrWhiteSpace(invalidFingerprint.Locale) ||
                                     invalidFingerprint.Languages?.Length == 0 ||
                                     string.IsNullOrWhiteSpace(invalidFingerprint.WebGlUnmaskedVendor) ||
                                     string.IsNullOrWhiteSpace(invalidFingerprint.WebGlUnmaskedRenderer) ||
                                     invalidFingerprint.FontList?.Length == 0;
                
                // Validation should correctly identify constraint violations
                return (shouldHaveErrors == hasValidationErrors).ToProperty();
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 17: Profile validation enforces constraints**
    /// **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property ValidFingerprintPassesValidation()
    {
        return Prop.ForAll(
            GenerateValidFingerprint(),
            validFingerprint =>
            {
                // Act - Validate the valid fingerprint
                var validationResult = ProfileValidator.ValidateFingerprint(validFingerprint);
                
                // Assert - Valid fingerprints should pass validation
                return validationResult.IsValid.ToProperty();
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 17: Profile validation enforces constraints**
    /// **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UserAgentValidationEnforcesFormat()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s)),
            userAgent =>
            {
                var fingerprint = CreateValidBaseFingerprint() with { UserAgent = userAgent };
                var validationResult = ProfileValidator.ValidateFingerprint(fingerprint);
                
                // Check if user agent matches Chrome pattern
                var isValidChromeUA = userAgent.StartsWith("Mozilla/5.0") && 
                                    userAgent.Contains("AppleWebKit/") && 
                                    userAgent.Contains("Chrome/") && 
                                    userAgent.Contains("Safari/");
                
                // Validation should pass only for valid Chrome user agents
                return (isValidChromeUA == validationResult.IsValid).ToProperty();
            });
    }

    private static Arbitrary<Fingerprint> GenerateInvalidFingerprint()
    {
        var random = new System.Random();
        return Arb.From(Gen.OneOf(
            // Invalid screen resolution
            Gen.Fresh(() => CreateValidBaseFingerprint() with 
            { 
                ScreenWidth = random.Next(-100, 799),
                ScreenHeight = random.Next(-100, 599)
            }),
            
            // Invalid hardware concurrency
            Gen.Fresh(() => CreateValidBaseFingerprint() with 
            { 
                HardwareConcurrency = random.Next(-10, 0)
            }),
            
            // Invalid device memory
            Gen.Fresh(() => CreateValidBaseFingerprint() with 
            { 
                DeviceMemory = random.Next(-10, 0)
            }),
            
            // Invalid device pixel ratio
            Gen.Fresh(() => CreateValidBaseFingerprint() with 
            { 
                DevicePixelRatio = random.NextDouble() * 1.4 - 1.0 // Range: -1.0 to 0.4
            }),
            
            // Invalid noise levels
            Gen.Fresh(() => CreateValidBaseFingerprint() with 
            { 
                CanvasNoiseLevel = random.NextDouble() * 0.9 - 1.0 // Range: -1.0 to -0.1
            }),
            
            // Empty required fields
            Gen.Fresh(() => CreateValidBaseFingerprint() with { UserAgent = "" }),
            Gen.Fresh(() => CreateValidBaseFingerprint() with { Platform = "" }),
            Gen.Fresh(() => CreateValidBaseFingerprint() with { Timezone = "" }),
            Gen.Fresh(() => CreateValidBaseFingerprint() with { Locale = "" }),
            Gen.Fresh(() => CreateValidBaseFingerprint() with { Languages = Array.Empty<string>() }),
            Gen.Fresh(() => CreateValidBaseFingerprint() with { FontList = Array.Empty<string>() })
        ));
    }

    private static Arbitrary<Fingerprint> GenerateValidFingerprint()
    {
        var random = new System.Random();
        var platforms = new[] { "Win32", "Win64", "MacIntel", "Linux x86_64" };
        var timezones = new[] { "America/New_York", "Europe/London", "Asia/Tokyo" };
        var locales = new[] { "en-US", "en-GB", "de-DE", "fr-FR" };
        var spoofLevels = new[] { SpoofLevel.Basic, SpoofLevel.Advanced, SpoofLevel.Ultra };
        
        return Arb.From(Gen.Fresh(() => new Fingerprint
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            Platform = platforms[random.Next(platforms.Length)],
            HardwareConcurrency = random.Next(1, 33),
            DeviceMemory = random.Next(1, 129),
            ScreenWidth = random.Next(800, 7681),
            ScreenHeight = random.Next(600, 4321),
            DevicePixelRatio = random.NextDouble() * 3.5 + 0.5, // Range: 0.5 to 4.0
            Timezone = timezones[random.Next(timezones.Length)],
            Locale = locales[random.Next(locales.Length)],
            Languages = new[] { "en-US", "en" },
            WebGlUnmaskedVendor = "Intel Inc.",
            WebGlUnmaskedRenderer = "Intel(R) Iris(R) Xe Graphics",
            CanvasNoiseLevel = random.NextDouble(), // Range: 0.0 to 1.0
            AudioNoiseLevel = random.NextDouble(), // Range: 0.0 to 1.0
            FontList = new[] { "Arial", "Helvetica", "Times New Roman" },
            SpoofLevel = spoofLevels[random.Next(spoofLevels.Length)]
        }));
    }

    private static Fingerprint CreateValidBaseFingerprint()
    {
        return new Fingerprint
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            Platform = "Win64",
            HardwareConcurrency = 8,
            DeviceMemory = 16,
            ScreenWidth = 1920,
            ScreenHeight = 1080,
            DevicePixelRatio = 1.0,
            Timezone = "America/New_York",
            Locale = "en-US",
            Languages = new[] { "en-US", "en" },
            WebGlUnmaskedVendor = "Intel Inc.",
            WebGlUnmaskedRenderer = "Intel(R) Iris(R) Xe Graphics",
            CanvasNoiseLevel = 0.05,
            AudioNoiseLevel = 0.001,
            FontList = new[] { "Arial", "Helvetica", "Times New Roman" },
            SpoofLevel = SpoofLevel.Ultra
        };
    }
}
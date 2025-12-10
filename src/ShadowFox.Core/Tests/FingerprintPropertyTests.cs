using FsCheck;
using FsCheck.Xunit;
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;
using Xunit;

namespace ShadowFox.Core.Tests;

public class FingerprintPropertyTests
{
    /// <summary>
    /// **Feature: profile-management, Property 2: Spoof level determines fingerprint characteristics**
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SpoofLevelDeterminesFingerprintCharacteristics(SpoofLevel spoofLevel)
    {
        var generator = new FingerprintGenerator();
        var fingerprint = generator.Generate(spoofLevel);
        
        // Verify the fingerprint has the correct spoof level
        var correctSpoofLevel = fingerprint.SpoofLevel == spoofLevel;
        
        // Verify noise characteristics match the spoof level
        var correctNoiseCharacteristics = spoofLevel switch
        {
            SpoofLevel.Basic => fingerprint.CanvasNoiseLevel == 0 && fingerprint.AudioNoiseLevel == 0,
            SpoofLevel.Advanced => fingerprint.CanvasNoiseLevel > 0 && fingerprint.CanvasNoiseLevel <= 0.03 && 
                                 fingerprint.AudioNoiseLevel > 0 && fingerprint.AudioNoiseLevel <= 0.002,
            SpoofLevel.Ultra => fingerprint.CanvasNoiseLevel >= 0.02 && fingerprint.CanvasNoiseLevel <= 0.08 && 
                              fingerprint.AudioNoiseLevel >= 0.0001 && fingerprint.AudioNoiseLevel <= 0.0015,
            _ => false
        };
        
        // Verify complexity characteristics match the spoof level
        var correctComplexity = spoofLevel switch
        {
            SpoofLevel.Basic => fingerprint.Languages.Length == 1 && 
                              fingerprint.FontList.Length >= 10 && fingerprint.FontList.Length <= 20 &&
                              fingerprint.DevicePixelRatio == 1.0,
            SpoofLevel.Advanced => fingerprint.Languages.Length <= 2 && 
                                 fingerprint.FontList.Length >= 40 && fingerprint.FontList.Length <= 80 &&
                                 fingerprint.DevicePixelRatio <= 1.5,
            SpoofLevel.Ultra => fingerprint.Languages.Length <= 4 && 
                              fingerprint.FontList.Length >= 80 && fingerprint.FontList.Length <= 140 &&
                              fingerprint.DevicePixelRatio <= 2.5,
            _ => false
        };
        
        // Verify hardware characteristics are within expected ranges for spoof level
        var correctHardware = spoofLevel switch
        {
            SpoofLevel.Basic => (fingerprint.HardwareConcurrency == 4 || fingerprint.HardwareConcurrency == 8) &&
                              (fingerprint.DeviceMemory == 8 || fingerprint.DeviceMemory == 16),
            SpoofLevel.Advanced => fingerprint.HardwareConcurrency <= 12 && fingerprint.DeviceMemory <= 32,
            SpoofLevel.Ultra => fingerprint.HardwareConcurrency <= 24 && fingerprint.DeviceMemory <= 64,
            _ => false
        };
        
        // Use the fingerprint's own validation method
        var hasValidCharacteristics = fingerprint.HasValidSpoofLevelCharacteristics();
        
        return correctSpoofLevel && correctNoiseCharacteristics && correctComplexity && correctHardware && hasValidCharacteristics;
    }

    /// <summary>
    /// **Feature: profile-management, Property 5: Profile cloning preserves fingerprint data**
    /// **Validates: Requirements 2.1, 2.4**
    /// </summary>
    [Property(MaxTest = 100)]
    public bool ProfileCloningPreservesFingerprintData(SpoofLevel spoofLevel)
    {
        var generator = new FingerprintGenerator();
        var originalFingerprint = generator.Generate(spoofLevel);
        var clonedFingerprint = generator.Clone(originalFingerprint);
        
        // Core data should be preserved
        var coreDataPreserved = 
            originalFingerprint.UserAgent == clonedFingerprint.UserAgent &&
            originalFingerprint.Platform == clonedFingerprint.Platform &&
            originalFingerprint.HardwareConcurrency == clonedFingerprint.HardwareConcurrency &&
            originalFingerprint.DeviceMemory == clonedFingerprint.DeviceMemory &&
            originalFingerprint.ScreenWidth == clonedFingerprint.ScreenWidth &&
            originalFingerprint.ScreenHeight == clonedFingerprint.ScreenHeight &&
            originalFingerprint.DevicePixelRatio == clonedFingerprint.DevicePixelRatio &&
            originalFingerprint.Timezone == clonedFingerprint.Timezone &&
            originalFingerprint.Locale == clonedFingerprint.Locale &&
            originalFingerprint.WebGlUnmaskedVendor == clonedFingerprint.WebGlUnmaskedVendor &&
            originalFingerprint.WebGlUnmaskedRenderer == clonedFingerprint.WebGlUnmaskedRenderer &&
            originalFingerprint.SpoofLevel == clonedFingerprint.SpoofLevel;
        
        // Languages and fonts should be preserved
        var languagesPreserved = originalFingerprint.Languages.SequenceEqual(clonedFingerprint.Languages);
        var fontsPreserved = originalFingerprint.FontList.SequenceEqual(clonedFingerprint.FontList);
        
        // Noise values should be regenerated (different) for Advanced and Ultra levels
        var noiseRegenerated = spoofLevel switch
        {
            SpoofLevel.Basic => originalFingerprint.CanvasNoiseLevel == clonedFingerprint.CanvasNoiseLevel &&
                              originalFingerprint.AudioNoiseLevel == clonedFingerprint.AudioNoiseLevel,
            SpoofLevel.Advanced or SpoofLevel.Ultra => 
                originalFingerprint.CanvasNoiseLevel != clonedFingerprint.CanvasNoiseLevel ||
                originalFingerprint.AudioNoiseLevel != clonedFingerprint.AudioNoiseLevel,
            _ => true
        };
        
        // Cloned fingerprint should still have valid characteristics for its spoof level
        var cloneHasValidCharacteristics = clonedFingerprint.HasValidSpoofLevelCharacteristics();
        
        // Both fingerprints should pass validation
        var originalValid = originalFingerprint.Validate().IsValid;
        var cloneValid = clonedFingerprint.Validate().IsValid;
        
        return coreDataPreserved && languagesPreserved && fontsPreserved && noiseRegenerated && 
               cloneHasValidCharacteristics && originalValid && cloneValid;
    }
}
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;
using Xunit;

namespace ShadowFox.Core.Tests;

public class FingerprintSerializationTests
{
    [Fact]
    public void Fingerprint_ToJson_ShouldSerializeCorrectly()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var fingerprint = generator.Generate(SpoofLevel.Advanced);

        // Act
        var json = fingerprint.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("userAgent", json);
        Assert.Contains("spoofLevel", json);
    }

    [Fact]
    public void Fingerprint_FromJson_ShouldDeserializeCorrectly()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var originalFingerprint = generator.Generate(SpoofLevel.Ultra);
        var json = originalFingerprint.ToJson();

        // Act
        var deserializedFingerprint = Fingerprint.FromJson(json);

        // Assert
        Assert.Equal(originalFingerprint.UserAgent, deserializedFingerprint.UserAgent);
        Assert.Equal(originalFingerprint.Platform, deserializedFingerprint.Platform);
        Assert.Equal(originalFingerprint.SpoofLevel, deserializedFingerprint.SpoofLevel);
        Assert.Equal(originalFingerprint.CanvasNoiseLevel, deserializedFingerprint.CanvasNoiseLevel);
        Assert.Equal(originalFingerprint.AudioNoiseLevel, deserializedFingerprint.AudioNoiseLevel);
    }

    [Fact]
    public void Fingerprint_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var originalFingerprint = generator.Generate(SpoofLevel.Basic);

        // Act
        var json = originalFingerprint.ToJson();
        var roundTripFingerprint = Fingerprint.FromJson(json);

        // Assert - Compare individual properties since arrays don't compare by value in records
        Assert.Equal(originalFingerprint.UserAgent, roundTripFingerprint.UserAgent);
        Assert.Equal(originalFingerprint.Platform, roundTripFingerprint.Platform);
        Assert.Equal(originalFingerprint.HardwareConcurrency, roundTripFingerprint.HardwareConcurrency);
        Assert.Equal(originalFingerprint.DeviceMemory, roundTripFingerprint.DeviceMemory);
        Assert.Equal(originalFingerprint.ScreenWidth, roundTripFingerprint.ScreenWidth);
        Assert.Equal(originalFingerprint.ScreenHeight, roundTripFingerprint.ScreenHeight);
        Assert.Equal(originalFingerprint.DevicePixelRatio, roundTripFingerprint.DevicePixelRatio);
        Assert.Equal(originalFingerprint.Timezone, roundTripFingerprint.Timezone);
        Assert.Equal(originalFingerprint.Locale, roundTripFingerprint.Locale);
        Assert.Equal(originalFingerprint.Languages, roundTripFingerprint.Languages);
        Assert.Equal(originalFingerprint.WebGlUnmaskedVendor, roundTripFingerprint.WebGlUnmaskedVendor);
        Assert.Equal(originalFingerprint.WebGlUnmaskedRenderer, roundTripFingerprint.WebGlUnmaskedRenderer);
        Assert.Equal(originalFingerprint.CanvasNoiseLevel, roundTripFingerprint.CanvasNoiseLevel);
        Assert.Equal(originalFingerprint.AudioNoiseLevel, roundTripFingerprint.AudioNoiseLevel);
        Assert.Equal(originalFingerprint.FontList, roundTripFingerprint.FontList);
        Assert.Equal(originalFingerprint.SpoofLevel, roundTripFingerprint.SpoofLevel);
    }

    [Fact]
    public void Fingerprint_FromJson_WithInvalidJson_ShouldThrowException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Fingerprint.FromJson(invalidJson));
    }

    [Fact]
    public void Fingerprint_FromJson_WithEmptyString_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Fingerprint.FromJson(""));
    }

    [Fact]
    public void Fingerprint_Validate_ShouldReturnValidResult()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var fingerprint = generator.Generate(SpoofLevel.Advanced);

        // Act
        var result = fingerprint.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Fingerprint_CloneWithNewNoise_ShouldPreserveDataButChangeNoise()
    {
        // Arrange
        var generator = new FingerprintGenerator();
        var originalFingerprint = generator.Generate(SpoofLevel.Ultra);

        // Act
        var clonedFingerprint = originalFingerprint.CloneWithNewNoise();

        // Assert
        Assert.Equal(originalFingerprint.UserAgent, clonedFingerprint.UserAgent);
        Assert.Equal(originalFingerprint.Platform, clonedFingerprint.Platform);
        Assert.Equal(originalFingerprint.SpoofLevel, clonedFingerprint.SpoofLevel);
        
        // For Ultra level, noise should be different
        Assert.True(originalFingerprint.CanvasNoiseLevel != clonedFingerprint.CanvasNoiseLevel ||
                   originalFingerprint.AudioNoiseLevel != clonedFingerprint.AudioNoiseLevel);
    }
}
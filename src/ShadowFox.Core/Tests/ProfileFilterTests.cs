using ShadowFox.Core.Models;
using Xunit;

namespace ShadowFox.Core.Tests;

public class ProfileFilterTests
{
    [Fact]
    public void ProfileFilter_MatchesProfile_WithSearchQuery_ReturnsCorrectResult()
    {
        // Arrange
        var profile = Profile.CreateNew("Test Profile", "{\"userAgent\":\"test\"}", tags: "web,testing");
        var filter = ProfileFilter.BySearchQuery("Test");

        // Act
        var matches = filter.MatchesProfile(profile);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void ProfileFilter_MatchesProfile_WithTags_ReturnsCorrectResult()
    {
        // Arrange
        var profile = Profile.CreateNew("Test Profile", "{\"userAgent\":\"test\"}", tags: "web,testing,automation");
        var filter = ProfileFilter.ByTags("testing");

        // Act
        var matches = filter.MatchesProfile(profile);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void ProfileExtensions_HasTag_ReturnsCorrectResult()
    {
        // Arrange
        var profile = Profile.CreateNew("Test Profile", "{\"userAgent\":\"test\"}", tags: "web, testing, automation");

        // Act & Assert
        Assert.True(profile.HasTag("testing"));
        Assert.True(profile.HasTag("web"));
        Assert.False(profile.HasTag("nonexistent"));
    }

    [Fact]
    public void ProfileExtensions_GenerateCloneName_ReturnsCorrectFormat()
    {
        // Arrange
        var profile = Profile.CreateNew("Original Profile", "{\"userAgent\":\"test\"}");

        // Act
        var cloneName = profile.GenerateCloneName();

        // Assert
        Assert.Equal("Original Profile - Copy", cloneName);
    }
}
using FsCheck;
using FsCheck.Xunit;
using ShadowFox.Core.Models;
using Xunit;

namespace ShadowFox.Core.Tests;

public class UsageTrackingPropertyTests
{
    /// <summary>
    /// **Feature: profile-management, Property 19: Usage tracking records access**
    /// **Validates: Requirements 7.1**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UsageTrackingRecordsAccess()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 200),
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 4000),
            (name, fingerprintJson) =>
            {
                // Create a new profile
                var profile = Profile.CreateNew(name.Trim(), fingerprintJson);
                
                // Initially, profile should never have been used
                var initiallyNeverUsed = profile.IsNeverUsed();
                var initialUsageCount = profile.UsageCount;
                var initialLastOpened = profile.LastOpenedAt;
                
                // Record the time before opening
                var beforeOpen = DateTime.UtcNow;
                
                // Simulate opening the profile (browser launch)
                profile.UpdateLastOpened();
                
                // Record the time after opening
                var afterOpen = DateTime.UtcNow;
                
                // Verify usage tracking behavior
                var noLongerNeverUsed = !profile.IsNeverUsed();
                var usageCountIncremented = profile.UsageCount == initialUsageCount + 1;
                var lastOpenedUpdated = profile.LastOpenedAt.HasValue;
                var lastOpenedInRange = profile.LastOpenedAt >= beforeOpen && profile.LastOpenedAt <= afterOpen;
                var lastModifiedUpdated = profile.LastModifiedAt >= beforeOpen && profile.LastModifiedAt <= afterOpen;
                
                // Open the profile multiple times to test accumulation
                var previousUsageCount = profile.UsageCount;
                var previousLastOpened = profile.LastOpenedAt;
                
                profile.UpdateLastOpened();
                profile.UpdateLastOpened();
                
                var usageCountAccumulates = profile.UsageCount == previousUsageCount + 2;
                var lastOpenedKeepsUpdating = profile.LastOpenedAt >= previousLastOpened;
                
                return initiallyNeverUsed.ToProperty()
                    .And((initialUsageCount == 0).ToProperty())
                    .And((initialLastOpened == null).ToProperty())
                    .And(noLongerNeverUsed.ToProperty())
                    .And(usageCountIncremented.ToProperty())
                    .And(lastOpenedUpdated.ToProperty())
                    .And(lastOpenedInRange.ToProperty())
                    .And(lastModifiedUpdated.ToProperty())
                    .And(usageCountAccumulates.ToProperty())
                    .And(lastOpenedKeepsUpdating.ToProperty());
            });
    }

    /// <summary>
    /// **Feature: profile-management, Property 20: Usage calculations are accurate**
    /// **Validates: Requirements 7.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property UsageCalculationsAreAccurate()
    {
        return Prop.ForAll(
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 200),
            Arb.From<string>().Filter(s => !string.IsNullOrWhiteSpace(s) && s.Length <= 4000),
            Arb.From<int>().Filter(n => n >= 0 && n <= 100), // Number of usage sessions
            (name, fingerprintJson, sessionCount) =>
            {
                // Create a new profile
                var profile = Profile.CreateNew(name.Trim(), fingerprintJson);
                
                // Simulate multiple usage sessions
                var expectedUsageCount = 0;
                DateTime? expectedLastOpened = null;
                
                for (int i = 0; i < sessionCount; i++)
                {
                    // Simulate some time passing between sessions
                    System.Threading.Thread.Sleep(1); // Ensure different timestamps
                    
                    var beforeOpen = DateTime.UtcNow;
                    profile.UpdateLastOpened();
                    var afterOpen = DateTime.UtcNow;
                    
                    expectedUsageCount++;
                    expectedLastOpened = profile.LastOpenedAt;
                    
                    // Verify each session is recorded correctly
                    var currentUsageCorrect = profile.UsageCount == expectedUsageCount;
                    var timestampInRange = profile.LastOpenedAt >= beforeOpen && profile.LastOpenedAt <= afterOpen;
                    
                    if (!currentUsageCorrect || !timestampInRange)
                        return false.ToProperty();
                }
                
                // Final verification
                var finalUsageCount = profile.UsageCount == expectedUsageCount;
                var finalLastOpened = profile.LastOpenedAt == expectedLastOpened;
                
                // Test time since last used calculation
                var timeSinceLastUsed = profile.GetTimeSinceLastUsed();
                var timeSinceCalculationCorrect = sessionCount == 0 
                    ? timeSinceLastUsed == TimeSpan.MaxValue 
                    : timeSinceLastUsed >= TimeSpan.Zero && timeSinceLastUsed < TimeSpan.FromMinutes(1);
                
                // Test never used status
                var neverUsedStatus = profile.IsNeverUsed();
                var neverUsedCorrect = sessionCount == 0 ? neverUsedStatus : !neverUsedStatus;
                
                return finalUsageCount.ToProperty()
                    .And(finalLastOpened.ToProperty())
                    .And(timeSinceCalculationCorrect.ToProperty())
                    .And(neverUsedCorrect.ToProperty());
            });
    }
}
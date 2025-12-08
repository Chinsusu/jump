using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;

namespace ShadowFox.UI.ViewModels;

public partial class ProfileEditViewModel : ViewModelBase
{
    private readonly FingerprintGenerator generator;

    [ObservableProperty]
    private Profile profile = new();

    [ObservableProperty]
    private string fingerprintUserAgent = string.Empty;

    public ProfileEditViewModel(FingerprintGenerator generator)
    {
        this.generator = generator;
    }

    public void SetProfile(Profile profile)
    {
        Profile = profile;
        FingerprintUserAgent = ExtractUserAgent(profile.FingerprintJson);
    }

    [RelayCommand]
    private void RandomizeFingerprint()
    {
        var fp = generator.Generate(SpoofLevel.Ultra);
        Profile.FingerprintJson = JsonSerializer.Serialize(fp);
        FingerprintUserAgent = fp.UserAgent;
        OnPropertyChanged(nameof(Profile));
    }

    private static string ExtractUserAgent(string json)
    {
        try
        {
            var fp = JsonSerializer.Deserialize<Fingerprint>(json);
            return fp?.UserAgent ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

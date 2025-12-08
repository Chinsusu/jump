using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.UI.Models;
using ShadowFox.UI.Views;

namespace ShadowFox.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IProfileRepository profileRepository;
    private readonly IServiceProvider serviceProvider;
    private readonly FingerprintGenerator fingerprintGenerator;

    public ObservableCollection<Profile> Profiles { get; } = new();

    [ObservableProperty]
    private Profile? selectedProfile;

    [ObservableProperty]
    private NavigationTab selectedTab = NavigationTab.ProfileManagement;

    [ObservableProperty]
    private ProfileSubTab selectedProfileSubTab = ProfileSubTab.AllProfiles;

    public ObservableCollection<GroupItem> GroupItems { get; } = new();

    public MainViewModel(
        IProfileRepository profileRepository,
        IServiceProvider serviceProvider,
        FingerprintGenerator fingerprintGenerator)
    {
        this.profileRepository = profileRepository;
        this.serviceProvider = serviceProvider;
        this.fingerprintGenerator = fingerprintGenerator;
        _ = LoadProfilesAsync();
        SeedGroups();
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        var items = await profileRepository.GetAllAsync();
        Profiles.Clear();
        foreach (var profile in items)
        {
            Profiles.Add(profile);
        }

        RefreshGroups();
    }

    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        var vm = serviceProvider.GetRequiredService<ProfileEditViewModel>();
        var profile = new Profile
        {
            Name = $"New Profile {DateTime.Now:MM-dd HH:mm}",
            FingerprintJson = JsonSerializer.Serialize(fingerprintGenerator.Generate()),
            CreatedAt = DateTime.UtcNow
        };

        vm.SetProfile(profile);
        var window = serviceProvider.GetRequiredService<ProfileEditWindow>();
        window.DataContext = vm;
        var result = window.ShowDialog();
        if (result == true)
        {
            await profileRepository.AddAsync(vm.Profile);
            await LoadProfilesAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        var confirm = MessageBox.Show("Delete profile?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        await profileRepository.DeleteAsync(SelectedProfile.Id);
        await LoadProfilesAsync();
    }

    [RelayCommand]
    private async Task EditProfileAsync()
    {
        if (SelectedProfile == null)
        {
            return;
        }

        var vm = serviceProvider.GetRequiredService<ProfileEditViewModel>();
        vm.SetProfile(SelectedProfile.CloneProfile());

        var window = serviceProvider.GetRequiredService<ProfileEditWindow>();
        window.DataContext = vm;

        var result = window.ShowDialog();
        if (result == true)
        {
            SelectedProfile.Name = vm.Profile.Name;
            SelectedProfile.Tags = vm.Profile.Tags;
            SelectedProfile.Group = vm.Profile.Group;
            SelectedProfile.Notes = vm.Profile.Notes;
            SelectedProfile.FingerprintJson = vm.Profile.FingerprintJson;
            SelectedProfile.LastOpenedAt = vm.Profile.LastOpenedAt;

            await profileRepository.UpdateAsync(SelectedProfile);
            await LoadProfilesAsync();
        }
    }

    [RelayCommand]
    private void SelectProfileTab() => SelectedTab = NavigationTab.ProfileManagement;

    [RelayCommand]
    private void SelectProxyTab() => SelectedTab = NavigationTab.ProxyManagement;

    [RelayCommand]
    private void SelectShopTab() => SelectedTab = NavigationTab.ShopProxy;

    [RelayCommand]
    private void SelectAllProfilesSubTab() => SelectedProfileSubTab = ProfileSubTab.AllProfiles;

    [RelayCommand]
    private void SelectGroupsSubTab() => SelectedProfileSubTab = ProfileSubTab.Groups;

    [RelayCommand]
    private void SelectUnassignedSubTab() => SelectedProfileSubTab = ProfileSubTab.Unassigned;

    private void SeedGroups()
    {
        GroupItems.Clear();
        GroupItems.Add(new GroupItem { Name = "Unassigned", ProfilesCount = Profiles.Count });
    }

    private void RefreshGroups()
    {
        var unassigned = GroupItems.FirstOrDefault(g => g.Name == "Unassigned");
        if (unassigned != null)
        {
            unassigned.ProfilesCount = Profiles.Count;
            GroupItems[GroupItems.IndexOf(unassigned)] = unassigned;
        }
        else
        {
            GroupItems.Add(new GroupItem { Name = "Unassigned", ProfilesCount = Profiles.Count });
        }
    }
}

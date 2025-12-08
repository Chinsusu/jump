using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic;
using ShadowFox.Core.Models;
using ShadowFox.Core.Services;
using ShadowFox.Infrastructure.Repositories;
using ShadowFox.UI.Models;
using ShadowFox.UI.Views;

namespace ShadowFox.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IProfileRepository profileRepository;
    private readonly IGroupRepository groupRepository;
    private readonly IServiceProvider serviceProvider;
    private readonly FingerprintGenerator fingerprintGenerator;

    public ObservableCollection<Profile> Profiles { get; } = new();
    public ObservableCollection<GroupItem> GroupItems { get; } = new();
    public ICollectionView GroupsView { get; }

    [ObservableProperty]
    private Profile? selectedProfile;

    [ObservableProperty]
    private NavigationTab selectedTab = NavigationTab.ProfileManagement;

    [ObservableProperty]
    private ProfileSubTab selectedProfileSubTab = ProfileSubTab.AllProfiles;

    [ObservableProperty]
    private string searchGroupText = string.Empty;

    public MainViewModel(
        IProfileRepository profileRepository,
        IGroupRepository groupRepository,
        IServiceProvider serviceProvider,
        FingerprintGenerator fingerprintGenerator)
    {
        this.profileRepository = profileRepository;
        this.groupRepository = groupRepository;
        this.serviceProvider = serviceProvider;
        this.fingerprintGenerator = fingerprintGenerator;

        GroupsView = CollectionViewSource.GetDefaultView(GroupItems);
        GroupsView.Filter = FilterGroup;

        _ = LoadProfilesAsync();
        _ = LoadGroupsAsync();
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
        RefreshGroupCounts();
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

    [RelayCommand]
    private async Task AddGroupAsync()
    {
        var input = Interaction.InputBox("Enter new group name", "New group", "");
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        var name = input.Trim();
        if (await groupRepository.ExistsAsync(name))
        {
            MessageBox.Show("Group already exists.", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await groupRepository.AddAsync(new Group { Name = name });
        await LoadGroupsAsync();
    }

    [RelayCommand]
    private async Task DeleteSelectedGroupsAsync()
    {
        var targets = GroupItems.Where(g => g.IsSelected && g.Name != "Unassigned").ToList();
        if (!targets.Any())
        {
            MessageBox.Show("Select at least one group (Unassigned cannot be deleted).", "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Delete {targets.Count} group(s)? Profiles will be unassigned.", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var g in targets)
        {
            await groupRepository.DeleteAsync(g.Id);
            await UnassignProfilesInGroupAsync(g.Name);
        }

        await LoadGroupsAsync();
        await LoadProfilesAsync();
    }

    [RelayCommand]
    private Task RefreshGroupsAsync()
    {
        GroupsView.Refresh();
        return Task.CompletedTask;
    }

    private async Task LoadGroupsAsync()
    {
        var groups = await groupRepository.GetAllAsync();

        GroupItems.Clear();
        foreach (var g in groups)
        {
            GroupItems.Add(new GroupItem
            {
                Id = g.Id,
                Name = g.Name,
                ProfilesCount = Profiles.Count(p => string.Equals(p.Group, g.Name, StringComparison.OrdinalIgnoreCase))
            });
        }

        var unassignedCount = Profiles.Count(p => string.IsNullOrWhiteSpace(p.Group));
        GroupItems.Add(new GroupItem { Name = "Unassigned", ProfilesCount = unassignedCount });
        GroupsView.Refresh();
    }

    private void RefreshGroupCounts()
    {
        foreach (var group in GroupItems)
        {
            if (group.Name == "Unassigned")
            {
                group.ProfilesCount = Profiles.Count(p => string.IsNullOrWhiteSpace(p.Group));
            }
            else
            {
                group.ProfilesCount = Profiles.Count(p =>
                    string.Equals(p.Group, group.Name, StringComparison.OrdinalIgnoreCase));
            }
        }
        GroupsView.Refresh();
    }

    private bool FilterGroup(object obj)
    {
        if (obj is not GroupItem g)
            return false;

        if (string.IsNullOrWhiteSpace(SearchGroupText))
            return true;

        return g.Name.Contains(SearchGroupText, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnSearchGroupTextChanged(string value)
    {
        GroupsView.Refresh();
    }

    private async Task UnassignProfilesInGroupAsync(string groupName)
    {
        var matches = Profiles.Where(p => string.Equals(p.Group, groupName, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var p in matches)
        {
            p.Group = null;
            await profileRepository.UpdateAsync(p);
        }
    }
}

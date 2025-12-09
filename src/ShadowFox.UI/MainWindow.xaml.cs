using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ShadowFox.UI;

public partial class MainWindow : Window
{
    public record class GroupItem
    {
        public string Name { get; set; }
        public int ProfileCount { get; set; }
        public bool Selected { get; set; }

        public GroupItem(string name, int profileCount, bool selected = false)
        {
            Name = name;
            ProfileCount = profileCount;
            Selected = selected;
        }
    }
    public record class ProfileItem
    {
        public string Name { get; set; }
        public string Proxy { get; set; }
        public string Group { get; set; }
        public string Tags { get; set; }
        public string Status { get; set; }
        public string BrowserVersion { get; set; }
        public string LastEdited { get; set; }

        public ProfileItem(string name, string proxy, string group, string tags, string status, string browserVersion, string lastEdited)
        {
            Name = name;
            Proxy = proxy;
            Group = group;
            Tags = tags;
            Status = status;
            BrowserVersion = browserVersion;
            LastEdited = lastEdited;
        }
    }
    public record class ProxyItem
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Ip { get; set; }
        public string Isp { get; set; }
        public string Country { get; set; }
        public int ProfileCount { get; set; }
        public string Group { get; set; }
        public string Tags { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Selected { get; set; }

        public ProxyItem(string address, string name, string status, string ip, string isp, string country, int profileCount, string group, string tags, string username, string password, bool selected = false)
        {
            Address = address;
            Name = name;
            Status = status;
            Ip = ip;
            Isp = isp;
            Country = country;
            ProfileCount = profileCount;
            Group = group;
            Tags = tags;
            Username = username;
            Password = password;
            Selected = selected;
        }
    }

    public static readonly DependencyProperty SelectedGroupProperty =
        DependencyProperty.Register(nameof(SelectedGroup), typeof(GroupItem), typeof(MainWindow),
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedProfileGroupProperty =
        DependencyProperty.Register(nameof(SelectedProfileGroup), typeof(GroupItem), typeof(MainWindow),
            new PropertyMetadata(null));

    public static readonly DependencyProperty NewProfileNameProperty =
        DependencyProperty.Register(nameof(NewProfileName), typeof(string), typeof(MainWindow),
            new PropertyMetadata("New Profile"));

    public static readonly DependencyProperty SelectedOsProperty =
        DependencyProperty.Register(nameof(SelectedOs), typeof(string), typeof(MainWindow),
            new PropertyMetadata("Windows"));

    public static readonly DependencyProperty SelectedBrowserVersionProperty =
        DependencyProperty.Register(nameof(SelectedBrowserVersion), typeof(string), typeof(MainWindow),
            new PropertyMetadata("Version 141"));

    public static readonly DependencyProperty SummaryUserAgentProperty =
        DependencyProperty.Register(nameof(SummaryUserAgent), typeof(string), typeof(MainWindow),
            new PropertyMetadata("Chrome/141 Windows"));

    public static readonly DependencyProperty SummaryResolutionProperty =
        DependencyProperty.Register(nameof(SummaryResolution), typeof(string), typeof(MainWindow),
            new PropertyMetadata("1440x900"));

    public static readonly DependencyProperty SummaryLanguagesProperty =
        DependencyProperty.Register(nameof(SummaryLanguages), typeof(string), typeof(MainWindow),
            new PropertyMetadata("en-US"));

    public static readonly DependencyProperty SummaryTimezoneProperty =
        DependencyProperty.Register(nameof(SummaryTimezone), typeof(string), typeof(MainWindow),
            new PropertyMetadata("Automatic"));

    public static readonly DependencyProperty SummaryWebRtcProperty =
        DependencyProperty.Register(nameof(SummaryWebRtc), typeof(string), typeof(MainWindow),
            new PropertyMetadata("Altered"));

    public static readonly DependencyProperty IsCreatingProfileProperty =
        DependencyProperty.Register(nameof(IsCreatingProfile), typeof(bool), typeof(MainWindow),
            new PropertyMetadata(false));

    public GroupItem? SelectedGroup
    {
        get => (GroupItem?)GetValue(SelectedGroupProperty);
        set => SetValue(SelectedGroupProperty, value);
    }

    public GroupItem? SelectedProfileGroup
    {
        get => (GroupItem?)GetValue(SelectedProfileGroupProperty);
        set => SetValue(SelectedProfileGroupProperty, value);
    }

    public string NewProfileName
    {
        get => (string)GetValue(NewProfileNameProperty);
        set => SetValue(NewProfileNameProperty, value);
    }

    public string SelectedOs
    {
        get => (string)GetValue(SelectedOsProperty);
        set => SetValue(SelectedOsProperty, value);
    }

    public string SelectedBrowserVersion
    {
        get => (string)GetValue(SelectedBrowserVersionProperty);
        set => SetValue(SelectedBrowserVersionProperty, value);
    }

    public string SummaryUserAgent
    {
        get => (string)GetValue(SummaryUserAgentProperty);
        set => SetValue(SummaryUserAgentProperty, value);
    }

    public string SummaryResolution
    {
        get => (string)GetValue(SummaryResolutionProperty);
        set => SetValue(SummaryResolutionProperty, value);
    }

    public string SummaryLanguages
    {
        get => (string)GetValue(SummaryLanguagesProperty);
        set => SetValue(SummaryLanguagesProperty, value);
    }

    public string SummaryTimezone
    {
        get => (string)GetValue(SummaryTimezoneProperty);
        set => SetValue(SummaryTimezoneProperty, value);
    }

    public string SummaryWebRtc
    {
        get => (string)GetValue(SummaryWebRtcProperty);
        set => SetValue(SummaryWebRtcProperty, value);
    }

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty, OnSearchChanged));
    public static readonly DependencyProperty ProxySearchTextProperty =
        DependencyProperty.Register(nameof(ProxySearchText), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty, OnProxySearchChanged));
    public static readonly DependencyProperty IsCreatingProxyProperty =
        DependencyProperty.Register(nameof(IsCreatingProxy), typeof(bool), typeof(MainWindow),
            new PropertyMetadata(false));
    public static readonly DependencyProperty ProxyConnectionTypeProperty =
        DependencyProperty.Register(nameof(ProxyConnectionType), typeof(string), typeof(MainWindow),
            new PropertyMetadata("HTTP"));
    public static readonly DependencyProperty ProxyAddressProperty =
        DependencyProperty.Register(nameof(ProxyAddress), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ProxyUsernameProperty =
        DependencyProperty.Register(nameof(ProxyUsername), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ProxyPasswordProperty =
        DependencyProperty.Register(nameof(ProxyPassword), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ProxyHostProperty =
        DependencyProperty.Register(nameof(ProxyHost), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty ProxyPortProperty =
        DependencyProperty.Register(nameof(ProxyPort), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty QuickProxyInputProperty =
        DependencyProperty.Register(nameof(QuickProxyInput), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty, OnQuickInputChanged));
    public static readonly DependencyProperty ProxyCheckInfoProperty =
        DependencyProperty.Register(nameof(ProxyCheckInfo), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty BulkProxyInputProperty =
        DependencyProperty.Register(nameof(BulkProxyInput), typeof(string), typeof(MainWindow),
            new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PageSizeProperty =
        DependencyProperty.Register(nameof(PageSize), typeof(int), typeof(MainWindow),
            new PropertyMetadata(10, OnPaginationChanged));
    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(MainWindow),
            new PropertyMetadata(1, OnPaginationChanged));
    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(MainWindow),
            new PropertyMetadata(1));

    public bool IsCreatingProfile
    {
        get => (bool)GetValue(IsCreatingProfileProperty);
        set => SetValue(IsCreatingProfileProperty, value);
    }

    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public string ProxySearchText
    {
        get => (string)GetValue(ProxySearchTextProperty);
        set => SetValue(ProxySearchTextProperty, value);
    }
    public bool IsCreatingProxy
    {
        get => (bool)GetValue(IsCreatingProxyProperty);
        set => SetValue(IsCreatingProxyProperty, value);
    }
    public string ProxyConnectionType
    {
        get => (string)GetValue(ProxyConnectionTypeProperty);
        set => SetValue(ProxyConnectionTypeProperty, value);
    }
    public string ProxyAddress
    {
        get => (string)GetValue(ProxyAddressProperty);
        set => SetValue(ProxyAddressProperty, value);
    }
    public string ProxyUsername
    {
        get => (string)GetValue(ProxyUsernameProperty);
        set => SetValue(ProxyUsernameProperty, value);
    }
    public string ProxyPassword
    {
        get => (string)GetValue(ProxyPasswordProperty);
        set => SetValue(ProxyPasswordProperty, value);
    }
    public string ProxyHost
    {
        get => (string)GetValue(ProxyHostProperty);
        set => SetValue(ProxyHostProperty, value);
    }
    public string ProxyPort
    {
        get => (string)GetValue(ProxyPortProperty);
        set => SetValue(ProxyPortProperty, value);
    }
    public string QuickProxyInput
    {
        get => (string)GetValue(QuickProxyInputProperty);
        set => SetValue(QuickProxyInputProperty, value);
    }
    public string ProxyCheckInfo
    {
        get => (string)GetValue(ProxyCheckInfoProperty);
        set => SetValue(ProxyCheckInfoProperty, value);
    }
    public string BulkProxyInput
    {
        get => (string)GetValue(BulkProxyInputProperty);
        set => SetValue(BulkProxyInputProperty, value);
    }
    public int PageSize
    {
        get => (int)GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }
    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }
    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }
    public int ProfilePageSize { get; set; } = 10;
    public int ProfileCurrentPage { get; set; } = 1;
    public int ProfileTotalPages { get; set; } = 1;

    public ObservableCollection<ProfileItem> Profiles { get; } = new();
    public ObservableCollection<ProfileItem> FilteredProfiles { get; } = new();
    public ObservableCollection<ProfileItem> PagedProfiles { get; } = new();
    public ObservableCollection<ProxyItem> Proxies { get; } = new();
    public ObservableCollection<ProxyItem> FilteredProxies { get; } = new();
    public ObservableCollection<ProxyItem> PagedProxies { get; } = new();

    public ObservableCollection<GroupItem> Groups { get; } = new();
    public ObservableCollection<string> BrowserVersions { get; } = new();
    public ObservableCollection<string> ProxyConnectionTypes { get; } = new() { "Without proxy", "HTTP", "SOCKS5" };

    private readonly string _dataFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ShadowFox",
        "data.json");
    private ProxyItem? _editingProxy;
    private bool _isEditingProxy;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        LoadData();
        if (!Profiles.Any())
        {
            Profiles.Add(new ProfileItem("Cloudmini", "103.183.x.x", "Unassigned", "Starter", "Launched", "139", "2025-09-09"));
            Profiles.Add(new ProfileItem("georgemercodm@g...", "45.134.x.x", "Unassigned", "Starter", "Ready", "139", "2025-10-09"));
            Profiles.Add(new ProfileItem("New Profile", "Proxy disabled", "Unassigned", "", "Ready", "139", "2025-10-21"));
        }

        if (!Groups.Any())
        {
            Groups.Add(new GroupItem("Unassigned", Profiles.Count));
        }

        BrowserVersions.Add("Version 141");
        BrowserVersions.Add("Version 140");
        BrowserVersions.Add("Version 139");

        if (!Proxies.Any())
        {
            Proxies.Add(new ProxyItem("127.0.0.1:40000", "Proxy", "dead", string.Empty, string.Empty, string.Empty, 0, "Unassigned", string.Empty, string.Empty, string.Empty));
        }

        SelectedProfileGroup = Groups.FirstOrDefault();
        IsCreatingProfile = false;
        IsCreatingProxy = false;
        QuickProxyInput = string.Empty;
        ProxyCheckInfo = string.Empty;
        BulkProxyInput = string.Empty;
        RefreshFilteredProfiles();
        RefreshFilteredProxies();
        UpdateViews("Profile");
    }

    private static void OnSearchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MainWindow window)
        {
            window.RefreshFilteredProfiles();
        }
    }

    private static void OnProxySearchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MainWindow window)
        {
            window.RefreshFilteredProxies();
            window.UpdatePagination();
        }
    }

    private void RefreshFilteredProfiles()
    {
        FilteredProfiles.Clear();
        var term = (SearchText ?? string.Empty).Trim().ToLowerInvariant();
        foreach (var p in Profiles.Where(p =>
                     string.IsNullOrWhiteSpace(term)
                     || p.Name.ToLowerInvariant().Contains(term)
                     || p.Proxy.ToLowerInvariant().Contains(term)
                     || p.Group.ToLowerInvariant().Contains(term)
                     || p.Tags.ToLowerInvariant().Contains(term)))
        {
            FilteredProfiles.Add(p);
        }
        UpdateProfilePagination();
    }

    private void RefreshFilteredProxies()
    {
        FilteredProxies.Clear();
        var term = (ProxySearchText ?? string.Empty).Trim().ToLowerInvariant();
        foreach (var p in Proxies.Where(p =>
                     string.IsNullOrWhiteSpace(term)
                     || p.Address.ToLowerInvariant().Contains(term)
                     || p.Name.ToLowerInvariant().Contains(term)
                     || p.Group.ToLowerInvariant().Contains(term)
                     || p.Tags.ToLowerInvariant().Contains(term)))
        {
            FilteredProxies.Add(p);
        }
        UpdatePagination();
    }

    private static void OnPaginationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MainWindow window)
        {
            window.UpdatePagination();
        }
    }

    private void UpdatePagination()
    {
        PagedProxies.Clear();
        if (FilteredProxies.Count == 0)
        {
            TotalPages = 1;
            CurrentPage = 1;
            return;
        }

        TotalPages = (int)Math.Ceiling(FilteredProxies.Count / (double)PageSize);
        if (CurrentPage < 1) CurrentPage = 1;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        var items = FilteredProxies.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        foreach (var item in items)
            PagedProxies.Add(item);
    }

    private void UpdateProfilePagination()
    {
        PagedProfiles.Clear();
        if (FilteredProfiles.Count == 0)
        {
            ProfileTotalPages = 1;
            ProfileCurrentPage = 1;
            return;
        }

        ProfileTotalPages = (int)Math.Ceiling(FilteredProfiles.Count / (double)ProfilePageSize);
        if (ProfileCurrentPage < 1) ProfileCurrentPage = 1;
        if (ProfileCurrentPage > ProfileTotalPages) ProfileCurrentPage = ProfileTotalPages;

        var items = FilteredProfiles.Skip((ProfileCurrentPage - 1) * ProfilePageSize).Take(ProfilePageSize);
        foreach (var item in items)
            PagedProfiles.Add(item);
    }

    private static void OnQuickInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MainWindow window && e.NewValue is string s)
        {
            window.FillProxyFieldsFromInput(s);
            window.AutoCheckParsedProxy();
        }
    }

    private void UpdateViews(string tag)
    {
        ProfileView.Visibility = tag.Equals("Profile", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
        GroupView.Visibility = tag.Equals("Group", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
        ProxyView.Visibility = tag.Equals("Proxy", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        var name = GroupNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show("Please enter a group name.", "Group", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (Groups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            System.Windows.MessageBox.Show("Group name already exists.", "Group", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var newGroup = new GroupItem(name, 0);
        Groups.Add(newGroup);
        SelectedGroup = newGroup;
        GroupNameBox.Clear();
        SaveData();
    }

    private void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedGroup is null) return;
        var toRemove = SelectedGroup;
        if (System.Windows.MessageBox.Show($"Delete group '{toRemove.Name}'?", "Confirm", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
        {
            Groups.Remove(toRemove);
            SelectedGroup = null;
            SaveData();
        }
    }

    private void BulkDeleteGroups_Click(object sender, RoutedEventArgs e)
    {
        var selected = Groups.Where(g => g.Selected).ToList();
        if (!selected.Any()) return;

        foreach (var g in selected)
            Groups.Remove(g);

        SelectedGroup = null;
        SaveData();
    }

    private void AddGroupFromProfile_Click(object sender, RoutedEventArgs e)
    {
        var name = ProfileNewGroupBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            System.Windows.MessageBox.Show("Please enter a group name.", "Group", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        if (Groups.Any(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedProfileGroup = Groups.First(g => g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            System.Windows.MessageBox.Show("Group name already exists, selected it.", "Group", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var newGroup = new GroupItem(name, 0);
        Groups.Add(newGroup);
        SelectedProfileGroup = newGroup;
        ProfileNewGroupBox.Clear();
        SaveData();
    }

    private void SelectAllGroups_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox cb)
        {
            var isChecked = cb.IsChecked == true;
            foreach (var g in Groups)
                g.Selected = isChecked;
        }
    }

    private void NavList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (e.AddedItems.OfType<System.Windows.Controls.ListBoxItem>().FirstOrDefault() is { Tag: string tag })
        {
            UpdateViews(tag);
            IsCreatingProfile = false;
            SearchText = string.Empty;
            RefreshFilteredProfiles();
            RefreshFilteredProxies();
        }
    }

    private void StartCreateProfile_Click(object sender, RoutedEventArgs e)
    {
        IsCreatingProfile = true;
    }

    private void NewProfile_Click(object sender, RoutedEventArgs e)
    {
        IsCreatingProfile = true;
    }

    private void CancelCreateProfile_Click(object sender, RoutedEventArgs e)
    {
        IsCreatingProfile = false;
        RefreshFilteredProfiles();
    }

    private void RandomizeFingerprint_Click(object sender, RoutedEventArgs e)
    {
        var rand = new Random();
        var resolutions = new[] { "1920x1080", "1366x768", "1440x900", "1600x900", "2560x1440" };
        var languages = new[] { "en-US", "en-GB", "de-DE", "fr-FR", "es-ES" };
        var timezones = new[] { "Automatic", "UTC", "America/New_York", "Europe/Berlin", "Asia/Ho_Chi_Minh" };
        var userAgents = new[]
        {
            "Chrome/141 Windows",
            "Chrome/140 Windows",
            "Chrome/141 MacOS",
            "Chrome/139 Linux"
        };

        SummaryResolution = resolutions[rand.Next(resolutions.Length)];
        SummaryLanguages = languages[rand.Next(languages.Length)];
        SummaryTimezone = timezones[rand.Next(timezones.Length)];
        SummaryUserAgent = userAgents[rand.Next(userAgents.Length)];
        SummaryWebRtc = rand.Next(2) == 0 ? "Altered" : "Blocked";
    }

    private void AddProxy_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Open proxy picker (placeholder).", "Proxy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async void CheckProxy_Click(object sender, RoutedEventArgs e)
    {
        var parsed = ParseProxyAddress(QuickProxyInput, ProxyUsername, ProxyPassword);
        if (string.IsNullOrWhiteSpace(parsed.host) && !string.IsNullOrWhiteSpace(ProxyHost))
        {
            parsed = (ProxyHost, ProxyPortToInt(ProxyPort), ProxyUsername, ProxyPassword);
        }

        ProxyCheckInfo = $"Parsed → Host: {parsed.host}, Port: {parsed.port}, User: {parsed.username}";
        var (status, ip, asn, country) = await QueryIpInfoAsync(parsed.host, parsed.port, parsed.username, parsed.password);
        ProxyCheckInfo = $"Parsed → Host: {parsed.host}, Port: {parsed.port}, User: {parsed.username} | Result: {status} | IP: {ip} | ISP: {asn} | Country: {country}";
    }

    private void AddProxyItem_Click(object sender, RoutedEventArgs e)
    {
        ResetProxyForm();
        IsCreatingProxy = true;
    }

    private void ImportProxyFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                BulkProxyInput = File.ReadAllText(dialog.FileName);
            }
            catch
            {
                System.Windows.MessageBox.Show("Failed to read file.", "Proxy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async void AcceptProxy_Click(object sender, RoutedEventArgs e)
    {
        var parsed = ParseProxyAddress(QuickProxyInput, ProxyUsername, ProxyPassword);
        if (string.IsNullOrWhiteSpace(parsed.host) && !string.IsNullOrWhiteSpace(ProxyHost))
        {
            parsed = (ProxyHost, ProxyPortToInt(ProxyPort), ProxyUsername, ProxyPassword);
        }

        if (string.IsNullOrWhiteSpace(parsed.host) || parsed.port == 0)
        {
            System.Windows.MessageBox.Show("Please enter proxy host and port.", "Proxy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var targetAddress = $"{parsed.host}:{parsed.port}".Trim();
        var duplicate = Proxies.Any(p =>
            p.Address.Equals(targetAddress, StringComparison.OrdinalIgnoreCase) &&
            (!_isEditingProxy || !_editingProxy?.Address.Equals(targetAddress, StringComparison.OrdinalIgnoreCase) == true));

        if (duplicate)
        {
            System.Windows.MessageBox.Show("Proxy already exists.", "Proxy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        var (status, ip, isp, country) = await QueryIpInfoAsync(parsed.host, parsed.port, parsed.username, parsed.password);
        var proxy = new ProxyItem(
            targetAddress,
            targetAddress,
            status,
            ip,
            isp,
            country,
            0,
            "Unassigned",
            string.Empty,
            parsed.username,
            parsed.password);

        if (_isEditingProxy && _editingProxy != null)
        {
            var idx = Proxies.IndexOf(_editingProxy);
            if (idx >= 0)
                Proxies[idx] = proxy;
        }
        else
        {
            Proxies.Add(proxy);
        }

        SaveData();
        RefreshFilteredProxies();
        ResetProxyForm();
        _editingProxy = null;
        _isEditingProxy = false;
        IsCreatingProxy = false;
    }

    private void CancelProxy_Click(object sender, RoutedEventArgs e)
    {
        ResetProxyForm();
        _editingProxy = null;
        _isEditingProxy = false;
        IsCreatingProxy = false;
    }

    private void AddBulkProxy_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(BulkProxyInput))
        {
            System.Windows.MessageBox.Show("Please paste proxies to add.", "Proxy", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var lines = BulkProxyInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var added = 0;
        var skipped = 0;

        foreach (var line in lines)
        {
            var parsed = ParseProxyAddress(line, string.Empty, string.Empty);
            if (string.IsNullOrWhiteSpace(parsed.host) || parsed.port == 0)
            {
                skipped++;
                continue;
            }

            var addr = $"{parsed.host}:{parsed.port}".Trim();
            if (Proxies.Any(p => p.Address.Equals(addr, StringComparison.OrdinalIgnoreCase)))
            {
                skipped++;
                continue;
            }

            var proxy = new ProxyItem(
                addr,
                addr,
                "dead",
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                "Unassigned",
                string.Empty,
                parsed.username,
                parsed.password);

            Proxies.Add(proxy);
            added++;
        }

        SaveData();
        RefreshFilteredProxies();
        ProxyCheckInfo = $"Bulk add completed. Added: {added}, Skipped: {skipped}.";
    }

    private async Task<(string status, string ip, string isp, string country)> QueryIpInfoAsync(string host, int port, string user, string pass)
    {
        if (string.IsNullOrWhiteSpace(host) || port == 0)
            return ("dead", string.Empty, string.Empty, string.Empty);

        var endpoints = new[] { "http://ipwho.is/", "https://ipwho.is/" };

        foreach (var url in endpoints)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(host, port)
                    {
                        Credentials = new NetworkCredential(user, pass)
                    },
                    UseProxy = true,
                    UseCookies = false
                };

                using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
                var resp = await client.GetStringAsync(url);
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement;
                var success = root.TryGetProperty("success", out var succEl) && succEl.GetBoolean();
                var ipVal = root.TryGetProperty("ip", out var ipEl) ? ipEl.GetString() ?? string.Empty : host;
                var ispVal = string.Empty;
                if (root.TryGetProperty("connection", out var conn))
                {
                    if (conn.TryGetProperty("isp", out var ispEl))
                        ispVal = ispEl.GetString() ?? string.Empty;
                    else if (conn.TryGetProperty("org", out var orgEl))
                        ispVal = orgEl.GetString() ?? string.Empty;
                }
                if (string.IsNullOrWhiteSpace(ispVal) && root.TryGetProperty("isp", out var ispRoot))
                    ispVal = ispRoot.GetString() ?? string.Empty;

                var countryVal = root.TryGetProperty("country_code", out var cEl) ? cEl.GetString() ?? string.Empty : string.Empty;
                return (success ? "live" : "dead", ipVal, ispVal, countryVal);
            }
            catch
            {
                // try next endpoint
            }
        }

        return ("dead", host, string.Empty, string.Empty);
    }

    private int ProxyPortToInt(string portText) =>
        int.TryParse(portText, out var p) ? p : 0;

    private static (string host, int port, string username, string password) ParseProxyAddress(string address, string userOverride, string passOverride)
    {
        if (string.IsNullOrWhiteSpace(address)) return (string.Empty, 0, string.Empty, string.Empty);

        var trimmed = address.Trim();
        trimmed = trimmed.Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                         .Replace("https://", "", StringComparison.OrdinalIgnoreCase);

        var hostPart = trimmed;
        var user = userOverride;
        var pass = passOverride;

        // user:pass@host:port
        if (trimmed.Contains('@'))
        {
            var splitAt = trimmed.Split('@', 2);
            var creds = splitAt[0].Split(':');
            if (creds.Length >= 2)
            {
                user = string.IsNullOrWhiteSpace(user) ? creds[0] : user;
                pass = string.IsNullOrWhiteSpace(pass) ? creds[1] : pass;
            }
            hostPart = splitAt[1];
        }
        else
        {
            // host:port:user:pass
            var segs = trimmed.Split(':');
            if (segs.Length >= 4)
            {
                // host could contain extra colons (IPv6 not handled here)
                hostPart = $"{segs[0]}:{segs[1]}";
                if (string.IsNullOrWhiteSpace(user)) user = segs[^2];
                if (string.IsNullOrWhiteSpace(pass)) pass = segs[^1];
            }
        }

        var hostSplit = hostPart.Split(':', StringSplitOptions.RemoveEmptyEntries);
        var host = hostSplit.Length > 0 ? hostSplit[0] : string.Empty;
        var port = hostSplit.Length > 1 && int.TryParse(hostSplit[1], out var p) ? p : 0;
        return (host, port, user, pass);
    }

    private void FillProxyFieldsFromInput(string input)
    {
        var parsed = ParseProxyAddress(input, ProxyUsername, ProxyPassword);
        ProxyHost = parsed.host;
        ProxyPort = parsed.port == 0 ? string.Empty : parsed.port.ToString();
        ProxyUsername = parsed.username;
        ProxyPassword = parsed.password;
        ProxyAddress = $"{parsed.host}:{(parsed.port == 0 ? "" : parsed.port)}".TrimEnd(':');
    }

    private async void AutoCheckParsedProxy()
    {
        var parsed = ParseProxyAddress(QuickProxyInput, ProxyUsername, ProxyPassword);
        if (string.IsNullOrWhiteSpace(parsed.host) || parsed.port == 0)
            return;

        ProxyCheckInfo = $"Parsed → Host: {parsed.host}, Port: {parsed.port}, User: {parsed.username}";
        var (status, ip, isp, country) = await QueryIpInfoAsync(parsed.host, parsed.port, parsed.username, parsed.password);
        ProxyCheckInfo = $"Parsed → Host: {parsed.host}, Port: {parsed.port}, User: {parsed.username} | Result: {status} | IP: {ip} | ISP: {isp} | Country: {country}";
    }

    private void ResetProxyForm()
    {
        ProxyConnectionType = "HTTP";
        QuickProxyInput = string.Empty;
        ProxyHost = string.Empty;
        ProxyPort = string.Empty;
        ProxyAddress = string.Empty;
        ProxyUsername = string.Empty;
        ProxyPassword = string.Empty;
        BulkProxyInput = string.Empty;
        ProxyCheckInfo = string.Empty;
    }

    private async void CheckProxyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string address)
        {
            var proxy = Proxies.FirstOrDefault(p => p.Address == address);
            var parsed = ParseProxyAddress(address, proxy?.Username ?? string.Empty, proxy?.Password ?? string.Empty);
                var (status, ip, isp, country) = await QueryIpInfoAsync(parsed.host, parsed.port, parsed.username, parsed.password);
                if (proxy != null)
                {
                    var updated = proxy with { Status = status, Ip = ip, Isp = isp, Country = country };
                    var idx = Proxies.IndexOf(proxy);
                    if (idx >= 0) Proxies[idx] = updated;
                    SaveData();
                    RefreshFilteredProxies();
                }
        }
    }

    private void DeleteProxyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string address)
        {
            var proxy = Proxies.FirstOrDefault(p => p.Address == address);
            if (proxy != null)
            {
                Proxies.Remove(proxy);
                SaveData();
                RefreshFilteredProxies();
            }
        }
    }

    private void PrevProfilePage_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCurrentPage > 1)
        {
            ProfileCurrentPage -= 1;
            UpdateProfilePagination();
        }
    }

    private void NextProfilePage_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileCurrentPage < ProfileTotalPages)
        {
            ProfileCurrentPage += 1;
            UpdateProfilePagination();
        }
    }

    private void EditProfileRow_Click(object sender, RoutedEventArgs e)
    {
        // placeholder for future edit implementation
        System.Windows.MessageBox.Show("Edit profile (not implemented yet).", "Profile", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void DeleteProfileRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string name)
        {
            var profile = Profiles.FirstOrDefault(p => p.Name == name);
            if (profile != null)
            {
                Profiles.Remove(profile);
                SaveData();
                RefreshFilteredProfiles();
            }
        }
    }

    private void BulkDelete_Click(object sender, RoutedEventArgs e)
    {
        var selected = Proxies.Where(p => p.Selected).ToList();
        if (!selected.Any()) return;

        foreach (var p in selected)
            Proxies.Remove(p);

        SaveData();
        RefreshFilteredProxies();
    }

    private void EditProxyRow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string address)
        {
            var proxy = Proxies.FirstOrDefault(p => p.Address == address);
            if (proxy != null)
            {
                _editingProxy = proxy;
                _isEditingProxy = true;
                ProxyAddress = proxy.Address;
                QuickProxyInput = string.IsNullOrWhiteSpace(proxy.Username)
                    ? proxy.Address
                    : $"{proxy.Address}:{proxy.Username}:{proxy.Password}";
                ProxyConnectionType = "HTTP";
                ProxyUsername = proxy.Username;
                ProxyPassword = proxy.Password;
                ProxyHost = proxy.Address.Split(':').FirstOrDefault() ?? string.Empty;
                ProxyPort = proxy.Address.Split(':').Skip(1).FirstOrDefault() ?? string.Empty;
                IsCreatingProxy = true;
            }
        }
    }

    private void SelectAllPage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.CheckBox cb)
        {
            var isChecked = cb.IsChecked == true;
            foreach (var proxy in PagedProxies)
            {
                proxy.Selected = isChecked;
            }
            // refresh bindings
            RefreshFilteredProxies();
        }
    }

    private void PrevPage_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPage > 1)
        {
            CurrentPage -= 1;
            UpdatePagination();
        }
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage += 1;
            UpdatePagination();
        }
    }

    private void ProfileAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string name)
        {
            ToggleProfileStatus(name);
        }
        e.Handled = true;
    }

    private void ProfileMoreAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string name)
        {
            System.Windows.MessageBox.Show($"More actions for {name}", "Action", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        e.Handled = true;
    }

    private void ToggleProfileStatus(string name)
    {
        var profile = Profiles.FirstOrDefault(p => p.Name == name);
        if (profile == null) return;

        var newStatus = profile.Status.Contains("Launched", StringComparison.OrdinalIgnoreCase) ? "Ready" : "Launched";
        var updated = profile with { Status = newStatus };

        var idx = Profiles.IndexOf(profile);
        if (idx >= 0)
        {
            Profiles[idx] = updated;
        }
        RefreshFilteredProfiles();
        SaveData();
    }

    private void CreateProfile_Click(object sender, RoutedEventArgs e)
    {
        AddProfileFromForm();
        IsCreatingProfile = false;
        RefreshFilteredProfiles();
        SaveData();
    }

    private void AddProfileFromForm()
    {
        var groupName = SelectedProfileGroup?.Name ?? "Unassigned";
        var newProfile = new ProfileItem(
            NewProfileName,
            "Proxy disabled",
            groupName,
            string.Empty,
            "Ready",
            SelectedBrowserVersion,
            DateTime.Now.ToString("yyyy-MM-dd"));

        Profiles.Add(newProfile);
        SaveData();
    }

    private void OsRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string os)
        {
            SelectedOs = os;
        }
    }

    private void TitleBar_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        SaveData();
        base.OnClosed(e);
    }

    private void LoadData()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataFilePath)!);
            if (!File.Exists(_dataFilePath)) return;

            var json = File.ReadAllText(_dataFilePath);
            var data = JsonSerializer.Deserialize<AppDataModel>(json);
            if (data != null)
            {
                Profiles.Clear();
                foreach (var p in data.Profiles ?? Enumerable.Empty<ProfileItem>())
                    Profiles.Add(p);

                Groups.Clear();
                foreach (var g in data.Groups ?? Enumerable.Empty<GroupItem>())
                    Groups.Add(g);

                Proxies.Clear();
                foreach (var p in data.Proxies ?? Enumerable.Empty<ProxyItem>())
                    Proxies.Add(p);
            }
        }
        catch
        {
            Debug.WriteLine("Failed to load data.json");
        }
    }

    private void SaveData()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataFilePath)!);
            var data = new AppDataModel
            {
                Profiles = Profiles.ToList(),
                Groups = Groups.ToList(),
                Proxies = Proxies.ToList()
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            using var fs = new FileStream(_dataFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(fs);
            writer.Write(json);
        }
        catch
        {
            Debug.WriteLine("Failed to save data.json");
        }
    }

    private class AppDataModel
    {
        public List<ProfileItem>? Profiles { get; set; }
        public List<GroupItem>? Groups { get; set; }
        public List<ProxyItem>? Proxies { get; set; }
    }
}

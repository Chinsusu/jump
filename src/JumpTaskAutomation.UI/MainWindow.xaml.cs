using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JumpTaskAutomation.UI;

public partial class MainWindow : Window
{
    private enum Tab
    {
        Profile,
        Proxy
    }

    public MainWindow()
    {
        InitializeComponent();
        SetTab(Tab.Profile);
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e) => SetTab(Tab.Profile);

    private void ProxyButton_Click(object sender, RoutedEventArgs e) => SetTab(Tab.Proxy);

    private void SetTab(Tab tab)
    {
        ProfilePanel.Visibility = tab == Tab.Profile ? Visibility.Visible : Visibility.Collapsed;
        ProxyPanel.Visibility = tab == Tab.Proxy ? Visibility.Visible : Visibility.Collapsed;

        TitleBlock.Text = tab == Tab.Profile ? "Profiles" : "Proxies";
        SubtitleBlock.Text = tab == Tab.Profile
            ? "Quản lý hồ sơ thiết bị và cấu hình tự động."
            : "Quản lý proxy, thông tin xác thực và ghi chú.";

        SetActive(ProfileButton, tab == Tab.Profile);
        SetActive(ProxyButton, tab == Tab.Proxy);
    }

    private void SetActive(Button button, bool isActive)
    {
        button.Opacity = isActive ? 1.0 : 0.75;
        var activeBrush = (Brush)FindResource("AccentBrush");
        var idleBrush = (Brush)FindResource("SidebarButtonBrush");
        button.Background = isActive ? activeBrush : idleBrush;
    }
}

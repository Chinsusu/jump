using System.Windows;
using System.Windows.Input;

namespace ShadowFox.UI;

public partial class MainWindow : Window
{
    public string SelectedNav { get; set; } = "Profile";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void DragArea_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is string tag)
        {
            SelectedNav = tag;
            // Refresh DataContext bindings manually since using simple property.
            DataContext = null;
            DataContext = this;
        }
    }
}

using System.Windows;
using System.Windows.Controls;

namespace ShadowFox.UI.Controls;

public partial class CreateProfileControl : UserControl
{
    public CreateProfileControl()
    {
        InitializeComponent();
    }

    private void OsRadio_Checked(object sender, RoutedEventArgs e) =>
        (Window.GetWindow(this) as MainWindow)?.OsRadio_Checked(sender, e);

    private void RandomizeFingerprint_Click(object sender, RoutedEventArgs e) =>
        (Window.GetWindow(this) as MainWindow)?.RandomizeFingerprint_Click(sender, e);

    private void CancelCreateProfile_Click(object sender, RoutedEventArgs e) =>
        (Window.GetWindow(this) as MainWindow)?.CancelCreateProfile_Click(sender, e);

    private void CreateProfile_Click(object sender, RoutedEventArgs e) =>
        (Window.GetWindow(this) as MainWindow)?.CreateProfile_Click(sender, e);
}

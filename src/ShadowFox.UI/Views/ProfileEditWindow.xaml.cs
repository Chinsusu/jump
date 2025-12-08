using System.Windows;
using ShadowFox.UI.ViewModels;

namespace ShadowFox.UI.Views;

public partial class ProfileEditWindow : Window
{
    public ProfileEditWindow(ProfileEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
